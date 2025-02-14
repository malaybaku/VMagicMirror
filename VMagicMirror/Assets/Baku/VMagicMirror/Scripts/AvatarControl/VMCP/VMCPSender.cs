using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UniVRM10;
using uOSC;
using VRM;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    // ポイント
    // - 「開きっぱなし」感のある挙動は避けておく (※Sendだから開くとかあんまないんだけど)
    // - 60FPSで実行中もデータ送信は30FPSにできるようにする
    
    // - 指のボーン姿勢や非標準ブレンドシェイプなどのデータは省けるようになっている 
    
    /// <summary>
    /// VMCPの送信をするすごいやつだよ
    /// </summary>
    public class VMCPSender : PresenterBase
    {
        // - Standard Editionではゲーム入力中はモーション送信が停止する (中身が有償アセットになる想定のとこなので、送れると無償再頒布みたくなってしまう)
        private const bool BlockMotionSendDuringGameInputLocomotion = FeatureLocker.IsFeatureLocked;

        private static readonly string[] BoneNames;
        private static readonly ExpressionKey[] StandardExpressionKeys = {
            ExpressionKey.Happy,
            ExpressionKey.Angry,
            ExpressionKey.Sad,
            ExpressionKey.Relaxed,
            ExpressionKey.Surprised,
            ExpressionKey.Aa,
            ExpressionKey.Ih,
            ExpressionKey.Ou,
            ExpressionKey.Ee,
            ExpressionKey.Oh,
            ExpressionKey.Blink,
            ExpressionKey.BlinkLeft,
            ExpressionKey.BlinkRight,
            ExpressionKey.LookUp,
            ExpressionKey.LookDown,
            ExpressionKey.LookLeft,
            ExpressionKey.LookRight,
            ExpressionKey.Neutral,
        };

        // ref: https://protocol.vmc.info/performer-spec
        // caseも折角なので変換している。また、Surprisedも(VRoidをリファレンスとして)やっといた方がわずかに親切そうなのでcaseを変えている
        private static readonly Dictionary<string, string> Vrm0FacialNameMap = new()
        {
            [nameof(ExpressionPreset.happy)] = nameof(BlendShapePreset.Joy),
            [nameof(ExpressionPreset.angry)] = nameof(BlendShapePreset.Angry),
            [nameof(ExpressionPreset.sad)] = nameof(BlendShapePreset.Sorrow),
            [nameof(ExpressionPreset.relaxed)] = nameof(BlendShapePreset.Fun),
            [nameof(ExpressionPreset.surprised)] = "Surprised",
            [nameof(ExpressionPreset.aa)] = nameof(BlendShapePreset.A),
            [nameof(ExpressionPreset.ih)] = nameof(BlendShapePreset.I),
            [nameof(ExpressionPreset.ou)] = nameof(BlendShapePreset.U),
            [nameof(ExpressionPreset.ee)] = nameof(BlendShapePreset.E),
            [nameof(ExpressionPreset.oh)] = nameof(BlendShapePreset.O),
            [nameof(ExpressionPreset.blink)] = nameof(BlendShapePreset.Blink),
            [nameof(ExpressionPreset.blinkLeft)] = nameof(BlendShapePreset.Blink_L),
            [nameof(ExpressionPreset.blinkRight)] = nameof(BlendShapePreset.Blink_R),
        };
        
        static VMCPSender()
        {
            BoneNames = new string[(int)HumanBodyBones.LastBone];
            for (var i = 0; i < BoneNames.Length; i++)
            {
                BoneNames[i] = ((HumanBodyBones)i).ToString();
            }
        }
        
        
        private readonly IMessageReceiver _receiver;
        private readonly IVRMLoadable _vrmLoadable;
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly ExpressionAccumulator _expressionAccumulator;
        private readonly IFactory<uOscClient> _oscClientFactory;

        private readonly ReactiveProperty<bool> _sendEnabled = new(false);

        private uOscClient _oscClient;
        private CancellationTokenSource _cts;
        private VmcProtocolDestSettings _settings;

        private bool _isLoaded;
        private Animator _animator;
        
        // 任意ボーンのチェックを頻発させたくないので、GetBoneTransformの結果はキャッシュする
        private readonly bool[] _boneValidity = new bool[(int)HumanBodyBones.LastBone];
        private readonly Transform[] _boneTransforms = new Transform[(int)HumanBodyBones.LastBone];
        
        public VMCPSender(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable,
            BodyMotionModeController bodyMotionModeController,
            ExpressionAccumulator expressionAccumulator,
            IFactory<uOscClient> oscClientFactory)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _bodyMotionModeController = bodyMotionModeController;
            _expressionAccumulator = expressionAccumulator;
            _oscClientFactory = oscClientFactory;
        }

        public override void Initialize()
        {
            _oscClient = _oscClientFactory.Create();
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
            _receiver.BindBoolProperty(
                VmmCommands.EnableVMCPSend,
                _sendEnabled
                );

            _receiver.AssignCommandHandler(
                VmmCommands.SetVMCPDestSettings,
                c => SetVmcpDestSettings(c.Content)
                );

            _sendEnabled.Subscribe(SetActive).AddTo(this);
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _animator = info.animator;

            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var t = _animator.GetBoneTransform((HumanBodyBones)i);
                _boneValidity[i] = t != null;
                _boneTransforms[i] = t;
            }
            
            _isLoaded = true;
        }

        private void OnVrmUnloaded()
        {
            _isLoaded = false;
            _animator = null;
        }

        public override void Dispose()
        {
            base.Dispose();
            CancelTask();
        }

        private void CancelTask()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        // BlockMotion~ のとこに書いたコメントを参照
        private bool CanSendPose()
        {
            return !(
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.GameInputLocomotion &&
                BlockMotionSendDuringGameInputLocomotion
                );
        }

        private bool HasValidSettings()
        {
            if (string.IsNullOrEmpty(_settings.destAddress))
            {
                return false;
            }

            if (_settings.destPort < 0 || _settings.destPort > 65535)
            {
                return false;
            }

            // NOTE: IPAddressとしての正当性とかまでは見ないで通してしまう (そこはuOSCに押し付けておく)
            return true;
        }
        
        // NOTE: 送信開始に失敗してもUIの通知とかはしない (Serverならいざ知らずClientなら十分レアだと思うので)
        private void SetActive(bool active)
        {
            try
            {
                if (active && HasValidSettings())
                {
                    _oscClient.address = _settings.destAddress;
                    _oscClient.port = _settings.destPort;
                    _oscClient.StartClient();
                    CancelTask();
                    _cts = new CancellationTokenSource();
                    SendVmcpAsync(_cts.Token).Forget();
                }
                else
                {
                    _oscClient.StopClient();
                    CancelTask();
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        private void SetVmcpDestSettings(string json)
        {
            try
            {
                var settings = JsonUtility.FromJson<VmcProtocolDestSettings>(json);
                _settings = settings;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        // NOTE: address/portが変わってもタスクは継続する (uOscClient側が対応してる挙動でもあるので)
        private async UniTaskVoid SendVmcpAsync(CancellationToken cancellationToken)
        {
            // TODO: sendで例外スローされる可能性があるかどうか調べる
            // (可能性がある場合、例外時に続行する方に寄せるべきか考えたいので)
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                // NOTE: 一応 >= で判定しているが、VMMのtargetFrameRateは30 or 60しか取らない想定
                if (Application.targetFrameRate >= 60 && _settings.prefer30fps)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                }

                if (!_isLoaded)
                {
                    continue;
                }
                
                //TODOかも: BundleにPose + Facial両方押し込む説あるかも？
                if (_settings.sendBone && CanSendPose())
                {
                    SendBonePoses(_settings.sendFingerBone);
                }

                if (_settings.sendFacial)
                {
                    SendFacials(_settings.sendNonStandardFacial, _settings.useVrm0Facial);
                }
            }
        }

        //TODO: ボーン名の入れ方とかが想定通りか自信ないのでデバッグちゃんとやってね
        
        private void SendBonePoses(bool sendFingerBone)
        {
            var bundle = new Bundle();
            // VMMではRoot自体は動かない
            // TODO: ※Root自体は動かないはず…だが、ゲーム入力中にRootごと回してるかもしれないので、ゲーム入力モードだけきちんと見たほうがいい
            var root= _animator.transform;
            var rootPos = root.position;
            var rootRot = root.rotation;
            bundle.Add(new uOSC.Message(
                "/VMC/Ext/Root/Pos", 
                rootPos.x, rootPos.y, rootPos.z, rootRot.x, rootRot.y, rootRot.z, rootRot.w
                ));

            var lastBoneInclusive = sendFingerBone
                ? (int)HumanBodyBones.LastBone - 1
                : (int)HumanBodyBones.Jaw;
            for (var i = 0; i <= lastBoneInclusive; i++)
            {
                if (!_boneValidity[i])
                {
                    continue;
                }

                var bone = _boneTransforms[i];
                bundle.Add(BonePoseMessage(
                    BoneNames[i], bone.localPosition, bone.localRotation
                    ));
            }

            _oscClient.Send(bundle);
        }

        private void SendFacials(bool sendNonStandardFacial, bool useVrm0Facial)
        {
            var bundle = new Bundle();

            var values = _expressionAccumulator.GetValues();
            if (sendNonStandardFacial)
            {
                foreach (var (key, value) in values)
                {
                    var keyName = useVrm0Facial
                        ? ConvertToVrm0FacialName(key.Name)
                        : key.Name;
                    bundle.Add(BlendShapeValueMessage(keyName, value));
                }
            }
            else
            {
                // 標準ブレンドシェイプしか送らない場合は名指しで拾って入れていく
                foreach (var key in StandardExpressionKeys)
                {
                    var keyName = useVrm0Facial
                        ? ConvertToVrm0FacialName(key.Name)
                        : key.Name;
                    // NOTE: 標準ブレンドシェイプは必ずあるはずなので、TryGetとかでガードしない
                    bundle.Add(BlendShapeValueMessage(keyName, values.GetValueOrDefault(key)));
                }
            }

            bundle.Add(new uOSC.Message("/VMC/Ext/Blend/Apply"));
            _oscClient.Send(bundle);
        }

        private static uOSC.Message BonePoseMessage(string boneName, Vector3 pos, Quaternion rot)
            => new("/VMC/Ext/Bone/Pos", boneName, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w);

        private static uOSC.Message BlendShapeValueMessage(string keyName, float value)
            => new("/VMC/Ext/Blend/Val", keyName, value);

        private static string ConvertToVrm0FacialName(string name)
            => Vrm0FacialNameMap.GetValueOrDefault(name, name);
    }

    [Serializable]
    public class VmcProtocolDestSettings
    {
        public string destAddress;
        public int destPort;
        
        // 指以外のボーンの姿勢を送信する
        public bool sendBone;
        // 指ボーンの姿勢を送信する。このオプションはsendBone == falseでは無視される
        public bool sendFingerBone;
        // VRM1.0の標準表情を一通り送信する
        public bool sendFacial;
        // VRMの標準ではない表情も送信する。このオプションはsendFacial == falseでは無視される
        public bool sendNonStandardFacial;

        // trueの場合、ブレンドシェイプ名をVRM0相当に変換する。
        public bool useVrm0Facial;
        
        // trueの場合、アプリケーションが60fpsで実行していても30FPSでデータを送信しようとする
        public bool prefer30fps;
        // アバターの手足の位置をトラッカー姿勢とみなして送信する: あってもいいかもと思ったが、VMM的にはニガテ分野なので無しにしとく
        // public bool sendTracker;
    }
}

