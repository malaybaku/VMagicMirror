using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
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
        // NOTE: Standard Editionではゲーム入力中はモーション送信が停止する。
        // 中身が有償アセットになる想定の場所なので、あんま無制限に送れてもちょっと…ということでこうしている
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
        private readonly EyeBoneAngleSetter _eyeBoneAngleSetter;
        private readonly ExpressionAccumulator _expressionAccumulator;
        private readonly IFactory<uOscClient> _oscClientFactory;

        private readonly ReactiveProperty<bool> _sendEnabled = new(false);

        private uOscClient _oscClient;
        private CancellationTokenSource _cts;
        private VmcProtocolSendSettings _settings = VmcProtocolSendSettings.CreateDefaultSetting();

        private bool _isLoaded;
        private Animator _animator;
        
        // NOTE: 任意ボーンのチェックが発生しすぎないようにするために、GetBoneTransformの結果はキャッシュする
        private readonly bool[] _boneValidity = new bool[(int)HumanBodyBones.LastBone];
        private readonly Transform[] _boneTransforms = new Transform[(int)HumanBodyBones.LastBone];
        
        public VMCPSender(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable,
            BodyMotionModeController bodyMotionModeController,
            EyeBoneAngleSetter eyeBoneAngleSetter,
            ExpressionAccumulator expressionAccumulator,
            IFactory<uOscClient> oscClientFactory)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _bodyMotionModeController = bodyMotionModeController;
            _eyeBoneAngleSetter = eyeBoneAngleSetter;
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
                VmmCommands.SetVMCPSendSettings,
                c => SetVmcpSendSettings(c.GetStringValue()));

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
                _bodyMotionModeController.MotionMode.CurrentValue is BodyMotionMode.GameInputLocomotion &&
                BlockMotionSendDuringGameInputLocomotion
                );
        }

        private bool HasValidSettings()
        {
            if (string.IsNullOrEmpty(_settings.SendAddress))
            {
                return false;
            }

            if (_settings.SendPort < 0 || _settings.SendPort > 65535)
            {
                return false;
            }

            // NOTE: uOscClientも内部的にIPAddress.Parseするので、それに準ずる形
            if (!IPAddress.TryParse(_settings.SendAddress, out _))
            {
                return false;
            }

            return true;
        }
        
        // NOTE: 送信開始に失敗してもUIでの通知はしない (Serverならいざ知らず、Clientなら十分レアだと思うので)
        private void SetActive(bool active)
        {
            try
            {
                if (active && HasValidSettings())
                {
                    Activate();
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

        private void Activate()
        {
            _oscClient.address = _settings.SendAddress;
            _oscClient.port = _settings.SendPort;
            _oscClient.StartClient();
            CancelTask();
            _cts = new CancellationTokenSource();
            SendVmcpAsync(_cts.Token).Forget();            
        }
        
        private void SetVmcpSendSettings(string json)
        {
            try
            {
                var settings = JsonUtility.FromJson<VmcProtocolSendSettings>(json);
                _settings = settings;

                if (_oscClient.isRunning && HasValidSettings())
                {
                    // 起動中のOscClientの送信先だけ更新し、タスクは動いたままにする
                    _oscClient.address = _settings.SendAddress;
                    _oscClient.port = _settings.SendPort;
                }
                else if (_sendEnabled.Value && !_oscClient.isRunning)
                {
                    // 設定が無効→有効に切り替わるとここを通過する。この場合、タスク自体も走らせ直す
                    Activate();
                }
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
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
                // NOTE: 一応 >= で判定しているが、VMMのtargetFrameRateは30 or 60しか取らない想定
                if (Application.targetFrameRate >= 60 && _settings.Prefer30Fps)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken);
                }

                if (!_isLoaded)
                {
                    continue;
                }
                
                if (_settings.SendBonePose && CanSendPose())
                {
                    SendBonePoses(_settings.SendFingerBonePose);
                }

                if (_settings.SendFacial)
                {
                    SendFacials(_settings.SendNonStandardFacial, _settings.UseVrm0Facial);
                }
            }
        }

        private void SendBonePoses(bool sendFingerBone)
        {
            var bundle = new Bundle();
            // NOTE: VMMではRootは普段動かないことが多いが、ゲーム入力モードではRootが回転したりする
            var root= _animator.transform;
            var rootPos = root.position;
            var rootRot = root.rotation;
            bundle.Add(new uOSC.Message(
                "/VMC/Ext/Root/Pos", 
                "", rootPos.x, rootPos.y, rootPos.z, rootRot.x, rootRot.y, rootRot.z, rootRot.w
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
                if (i == (int)HumanBodyBones.Hips)
                {
                    // 「Hipsのローカル姿勢 == Rootから見た姿勢」に明示的に計算する。
                    // こうしないとゲーム入力モードのジャンプ/しゃがみとかでHipsが動いたのを反映できないため
                    var hipsLocalPosition = root.InverseTransformPoint(bone.position);
                    var hipsLocalRotation = Quaternion.Inverse(rootRot) * bone.rotation;
                    bundle.Add(BonePoseMessage(
                        BoneNames[i], hipsLocalPosition, hipsLocalRotation
                    ));
                }
                else if (i == (int)HumanBodyBones.LeftEye)
                {
                    //NOTE: 目ボーンだけはControlRigを無視して制御しているので、その制御結果を明示的に持ってくる
                    bundle.Add(BonePoseMessage(
                        BoneNames[i], bone.localPosition, _eyeBoneAngleSetter.LeftEyeLocalRotation
                    ));
                }
                else if (i == (int)HumanBodyBones.RightEye)
                {
                    bundle.Add(BonePoseMessage(
                        BoneNames[i], bone.localPosition, _eyeBoneAngleSetter.RightEyeLocalRotation
                    ));
                }
                else
                {
                    bundle.Add(BonePoseMessage(
                        BoneNames[i], bone.localPosition, bone.localRotation
                    ));
                }
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
    public class VmcProtocolSendSettings
    {
        public string SendAddress;
        public int SendPort;
        
        // 指以外のボーンの姿勢を送信する
        public bool SendBonePose;
        // 指ボーンの姿勢を送信する。このオプションはsendBone == falseでは無視される
        public bool SendFingerBonePose;
        // VRM1.0の標準表情を一通り送信する
        public bool SendFacial;
        // VRMの標準ではない表情も送信する。このオプションはsendFacial == falseでは無視される
        public bool SendNonStandardFacial;

        // trueの場合、ブレンドシェイプ名をVRM0相当に変換する。
        public bool UseVrm0Facial;
        
        // trueの場合、アプリケーションが60fpsで実行していても30FPSでデータを送信しようとする
        public bool Prefer30Fps;

        // アバターの手足の位置をトラッカー姿勢とみなして送信する: あってもいいかもと思ったが、VMM的にはニガテ分野なので無しにしとく
        // public bool SendTrackerPose;
        
        //NOTE: WPF側と初期値をあわせている
        public static VmcProtocolSendSettings CreateDefaultSetting() => new()
        {
            SendAddress = "127.0.0.1",
            SendPort = 9000,
            SendBonePose = true,
            SendFingerBonePose = true,
            SendFacial = true,
            SendNonStandardFacial = false,
            UseVrm0Facial = true,
            Prefer30Fps = false,
        };
    }
}

