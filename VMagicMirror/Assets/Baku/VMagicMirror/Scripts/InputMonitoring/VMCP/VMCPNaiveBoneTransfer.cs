using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    //NOTE: このクラスの`Set`はIKの適用より後、かつVRM1.0のRuntimeのProcess()よりは先に呼ばれる必要がある。のでMonoBehaviour。
    public class VMCPNaiveBoneTransfer : MonoBehaviour
    {
        //NOTE: 60FPSのVMagicMirrorが30FPSアプリからポーズを受信したときに滑らかになる…みたいな志向で調整した値
        private const float LerpFactor = 24f;

        // 平滑化が有効な場合、 _applyWeight を気にしたうえで補間も効かせる。
        private const string HipsBoneKey = nameof(HumanBodyBones.Hips);
        
        //NOTE:
        // - このクラスの守備範囲はFinger以外のボーン全般
        //   - Headを送ってるSourceは上半身のうち、Handとその子要素を除くボーン回転を指定できる
        //   - Handを送ってるSourceはHandのボーン回転を指定できる
        // - - LowerBodyを送ってるSourceはHips~Toesのボーン回転を指定できる
        // - 指ボーンは取得できている場合、VMCPHandに基づいてVMCPBasedFingerSetterが処理するので、ここでの処理は不要
        private static readonly HumanBodyBones[] UpperBodyBones = new[]
        {
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,

            HumanBodyBones.LeftShoulder,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightShoulder,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
        };

        private static readonly HumanBodyBones[] LowerBodyBones = new[]
        {
            // NOTE: HipsはPositionと一緒にWorld座標ベースで動かす特殊な処理をするので、一括の回転処理の対象からは外している
            //HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.RightToes,
        };
        
        private static readonly HumanBodyBones[] HandBones = new[]
        {
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightHand,
        };

        private static readonly HumanBodyBones[] SpineToHeadBones = new[]
        {
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
        };

        // _upperBodyBones ~ _handBones までのボーンが全部入ってるやつ。EndOfFrameでボーンを書き戻す時に使う
        private readonly Dictionary<HumanBodyBones, Transform> _allBones = new();
        
        // 上半身のボーンのうち手と指以外
        private readonly Dictionary<HumanBodyBones, Transform> _upperBodyBones = new();

        // Spine ~ Headの体幹のボーン。加算処理をやる場合に使う
        private readonly Dictionary<HumanBodyBones, Transform> _spineToHeadBones = new();
        
        // Hips ~ Toesまでの両脚ぶん。ただしHipsは特別扱いなので分けて持っておく
        private readonly Dictionary<HumanBodyBones, Transform> _lowerBodyBones = new();
        private Transform _hipsBone;

        // LeftHand / RightHandのみ
        private readonly Dictionary<HumanBodyBones, Transform> _handBones = new();

        private readonly ReactiveProperty<bool> _enableBoneLerp = new();

        // NOTE: 下記は姿勢適用をなめらかにする場合に使う。モデルのリロードや、なめらか適用オプションの無効化が発生するとキャッシュが消える￥
        private readonly Dictionary<HumanBodyBones, Quaternion> _localRotationLerpCache = new();
        private Pose? _hipsPoseLerpCache = null;

        // Spine~Headに対して加算ベースでVMCPの回転を適用するとき、LateUpdate直前の回転を一時的に入れるキャッシュ
        private readonly Dictionary<HumanBodyBones, Quaternion> _spineToHeadBoneBaseRotCache = new();
        
        // LateUpate中に変更したボーン回転の変更前の値を保持するキャッシュ。
        // 加算モーションの使用中だけ、毎フレームごとにClear + 再書き込みを行う。また、hipsについてはworld姿勢で値を覚えておく
        private readonly Dictionary<HumanBodyBones, Quaternion> _overwrittenRotationCache = new();
        private Pose? _overwrittenHipsPoseCache = null;
        
        private bool _hasModel;
        private IMessageReceiver _receiver;
        private IVRMLoadable _vrmLoadable;
        private FaceControlConfiguration _faceControlConfiguration;
        private VMCPHandPose _vmcpHand;
        private VMCPHeadPose _vmcpHead;
        private VMCPLowerBodyPose _vmcpLowerBodyPose;

        //NOTE: WordToMotionで上半身を動かすときweightを下げていく
        private CancellationTokenSource _weightCts;
        private float _applyWeight = 1f;

        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable,
            FaceControlConfiguration faceControlConfiguration,
            VMCPHandPose vmcpHand,
            VMCPHeadPose vmcpHead,
            VMCPLowerBodyPose vmcpLowerBodyPose)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _faceControlConfiguration = faceControlConfiguration;
            _vmcpHand = vmcpHand;
            _vmcpHead = vmcpHead;
            _vmcpLowerBodyPose = vmcpLowerBodyPose;

            _receiver.BindBoolProperty(VmmCommands.EnableVMCPPoseLerp, _enableBoneLerp);
            
            _vrmLoadable.VrmLoaded += info =>
            {
                var a = info.animator;
                
                foreach (var bone in UpperBodyBones)
                {
                    var t = a.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _upperBodyBones[bone] = t;
                        _allBones[bone] = t;
                    }
                }
                
                foreach (var bone in SpineToHeadBones)
                {
                    var t = a.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _spineToHeadBones[bone] = t;
                        _allBones[bone] = t;
                    }
                }

                foreach (var bone in LowerBodyBones)
                {
                    var t = a.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _lowerBodyBones[bone] = t;
                        _allBones[bone] = t;
                    }
                }
                

                // Hipsは特別扱いすることに注意
                _hipsBone = a.GetBoneTransform(HumanBodyBones.Hips);
                
                foreach (var bone in HandBones)
                {
                    var t = a.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _handBones[bone] = t;
                        _allBones[bone] = t;
                    }
                }

                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _allBones.Clear();
                _upperBodyBones.Clear();
                _spineToHeadBones.Clear();
                _lowerBodyBones.Clear();
                _hipsBone = null;
                _handBones.Clear();
                ClearLerpCache();
                
                _overwrittenHipsPoseCache = null;
                _overwrittenRotationCache.Clear();
            };

            // 補間オン/オフを繰り返したときヘンにならないようにしてる
            _enableBoneLerp
                .Where(v => !v)
                .Subscribe(_ => ClearLerpCache())
                .AddTo(this);
        }

        public void FadeInWeight(float duration) => FadeWeight(1f, duration);
        public void FadeOutWeight(float duration) => FadeWeight(0f, duration);

        /// <summary>
        /// VMCPによる姿勢移動が発生していた場合、その前の姿勢戻す。
        /// この処理は各フレームの Render ~ EndOfFrame の間に呼ばれるのを想定している。
        /// 1フレーム中に複数回呼んだ場合、最初の呼び出しだけが有効。
        /// VMCPの姿勢が適用中ではない場合、呼んでも何も起こらない。
        /// </summary>
        /// <remarks>
        /// この関数がpublic APIになっている理由:
        /// とくに「アバターの現在の姿勢から計算したIK位置を、ワールド姿勢としてキャッシュする」みたいなケースにおいて、
        /// VMCPによってアバターが大幅に移動してるとIKが直立時の位置から大幅にずれため、次フレームのFBBIKの計算がおかしくなる。
        /// こういうケースの対策として、「だいたい直立なはずの姿勢に戻す」というAPIを提供している
        /// </remarks>
        public void RestoreBoneIfNeededAsync()
        {
            if (!_hasModel)
            {
                _overwrittenHipsPoseCache = null;
                _overwrittenRotationCache.Clear();
                return;
            }
            
            // 加算モーションをやっているとき、通常の直立姿勢の計算とVMCPの計算が両方走るので以下のような面倒ごとがある。
            // - 直立してる想定でEndOfFrameの姿勢を取るMonoBehaviourがあるが、それが壊れやすい
            // - 次のFBBIKの計算に対して初期値がだいぶしんどい
            // これの対策として、LateUpdateで書き換えたボーンをぜんぶ元に戻す
            foreach (var pair in _overwrittenRotationCache)
            {
                _allBones[pair.Key].localRotation = pair.Value;
            }
        
            if (_overwrittenHipsPoseCache.HasValue)
            {
                var hipsPose = _overwrittenHipsPoseCache.Value;
                _hipsBone.SetPositionAndRotation(hipsPose.position, hipsPose.rotation);
            }
        }

        private void LateUpdate()
        {
            // この時点で残ってるキャッシュは常に古すぎて邪魔なので、逐一消してよい
            _overwrittenHipsPoseCache = null;
            _overwrittenRotationCache.Clear();

            if (!_hasModel)
            {
                return;
            }
            
            var lerpFactor = Mathf.Clamp01(LerpFactor * Time.deltaTime);

            //NOTE: HeadとHandが同一ソースの場合、結果的に単一ソースからボーン回転が読み出される
            if (_vmcpHead.IsConnected.CurrentValue)
            {
                if (_faceControlConfiguration.UseAdditionalVmcpHeadMotion.CurrentValue)
                {
                    CacheCurrentSpineToHeadRotations();
                }
                SetBoneRotations(_vmcpHead.Humanoid, _upperBodyBones, lerpFactor);
                if (_faceControlConfiguration.UseAdditionalVmcpHeadMotion.CurrentValue)
                {
                    ApplyCachedSpineToHeadRotations();
                }
            }

            // TODO: 実行タイミング的に問題ないかは要チェック
            // NOTE: 接続してなくても受信の意思があれば指は制御してしまう
            if (_vmcpHand.IsActive.CurrentValue)
            {
                _vmcpHand.ApplyFingerLocalPose();
            }

            if (_vmcpHand.IsConnected.CurrentValue)
            {
                SetBoneRotations(_vmcpHand.Humanoid, _handBones, lerpFactor);
            }

            if (_vmcpLowerBodyPose.IsConnected.CurrentValue)
            {
                var lowerBodyHumanoid = _vmcpLowerBodyPose.Humanoid;
                SetBoneRotations(lowerBodyHumanoid, _lowerBodyBones, lerpFactor);
                // 本アバターのRootBone自体は動かさず、Hipsを目標位置に持っていく
                if (lowerBodyHumanoid.HipsLocalPosition is { } hipsPosition)
                {
                    // root - hips のヒエラルキー相当の計算でHipsの姿勢を計算 + 適用する。
                    // この処理はHipsをワールド座標で固定するため、BodyOffsetManagerとかFBBIKの結果を完全に無視して適用される
                    var rootPose = lowerBodyHumanoid.RootPose;
                    var hipsWorldPosition = rootPose.position + rootPose.rotation * hipsPosition;
                    var hipsWorldRotation = 
                        rootPose.rotation * _vmcpLowerBodyPose.Humanoid.GetLocalRotation(HipsBoneKey);

                    SetHipsPose(hipsWorldPosition, hipsWorldRotation, lerpFactor);
                }
            }
        }

        private void OnDestroy()
        {
            _weightCts?.Cancel();
            _weightCts?.Dispose();
        }

        //NOTE: 同一ボーンに対して毎フレーム最大1回しか呼ばない…という前提で実装されてる (補間の部分が)
        private void SetBoneRotations(VMCPBasedHumanoid source, Dictionary<HumanBodyBones, Transform> dest, float lerpFactor)
        {
            if (_applyWeight <= 0f)
            {
                return;
            }

            foreach (var pair in dest)
            {
                Quaternion targetRotation;
                if (_applyWeight >= 1f)
                {
                    targetRotation = source.GetLocalRotation(pair.Key.ToString());
                }
                else
                {
                    targetRotation = Quaternion.Slerp(
                        pair.Value.localRotation,
                        source.GetLocalRotation(pair.Key.ToString()),
                        _applyWeight
                    );
                }

                SetBoneLocalRotation(pair.Key, pair.Value, targetRotation, lerpFactor);
            }
        }

        private void SetBoneLocalRotation(HumanBodyBones key, Transform target, Quaternion rotation, float lerpFactor)
        {
            _overwrittenRotationCache[key] = target.localRotation;
            
            if (!_enableBoneLerp.CurrentValue)
            {
                target.localRotation = rotation;
                return;
            }

            // 補間したいがキャッシュが無い場合: 最初の値なのでキャッシュしつつ適用し、この時点では補間はしない
            if (!_localRotationLerpCache.TryGetValue(key, out var currentRotation))
            {
                _localRotationLerpCache[key] = rotation;
                target.localRotation = rotation;
                return;
            }
            
            // 補間のキャッシュがある場合: 補間して適用
            var nextRotation = Quaternion.Slerp(currentRotation, rotation, lerpFactor);
            target.localRotation = nextRotation;
            _localRotationLerpCache[key] = nextRotation;
        }

        private void SetHipsPose(Vector3 hipsWorldPosition, Quaternion hipsWorldRotation, float lerpFactor)
        {
            if (_applyWeight <= 0f)
            {
                return;
            }

            var target = _hipsBone;
            _overwrittenHipsPoseCache = new Pose(target.position, target.rotation);
            
            if (_applyWeight < 1)
            {
                var pos = target.position;
                var rot = target.rotation;
                hipsWorldPosition = Vector3.Lerp(pos, hipsWorldPosition, _applyWeight);
                hipsWorldRotation = Quaternion.Slerp(rot, hipsWorldRotation, _applyWeight);
            }

            // 平滑化しない場合、そのまま適用して終わり
            if (!_enableBoneLerp.CurrentValue)
            {
                target.SetPositionAndRotation(hipsWorldPosition, hipsWorldRotation);
                return;
            }

            // 平滑化はしたいがキャッシュがまだない: 初期値をキャッシュして終わり
            if (_hipsPoseLerpCache == null)
            {
                target.SetPositionAndRotation(hipsWorldPosition, hipsWorldRotation);
                _hipsPoseLerpCache = new Pose(hipsWorldPosition, hipsWorldRotation);
                return;
            }
            
            // 補間のキャッシュがある場合: 補間して適用
            var currentPose = _hipsPoseLerpCache.Value;
            var nextPosition = Vector3.Lerp(
                currentPose.position, hipsWorldPosition, lerpFactor
            );
            var nextRotation = Quaternion.Slerp(
                currentPose.rotation, hipsWorldRotation, lerpFactor
            );

            target.SetPositionAndRotation(nextPosition, nextRotation);
            _hipsPoseLerpCache = new Pose(nextPosition, nextRotation);
        }

        private void ClearLerpCache()
        {
            _localRotationLerpCache.Clear();
            _hipsPoseLerpCache = null;
        }

        private void CacheCurrentSpineToHeadRotations()
        {
            foreach (var bone in _spineToHeadBones)
            {
                _spineToHeadBoneBaseRotCache[bone.Key] = bone.Value.localRotation;
            }
        }
        
        private void ApplyCachedSpineToHeadRotations()
        {
            foreach (var bone in _spineToHeadBones)
            {
                // 左から掛けることに注意: つまり、VMCPの回転のほうをベースの回転と見なす
                if (_spineToHeadBoneBaseRotCache.TryGetValue(bone.Key, out var baseRot))
                {
                    bone.Value.localRotation = baseRot * bone.Value.localRotation;
                }
            }
        }
        
        private void FadeWeight(float target, float duration)
        {
            _weightCts?.Cancel();
            _weightCts?.Dispose();
            _weightCts = new();
            FadeWeightAsync(target, duration, _weightCts.Token).Forget();
        }

        private async UniTaskVoid FadeWeightAsync(float target, float duration, CancellationToken ct)
        {
            if (duration <= 0f)
            {
                _applyWeight = target;
                return;
            }

            if (target > .5f)
            {
                while (_applyWeight < 1f)
                {
                    _applyWeight = Mathf.Clamp01(_applyWeight + Time.deltaTime / duration);
                    await UniTask.NextFrame(ct);
                }
            }
            else
            {
                while (_applyWeight > 0f)
                {
                    _applyWeight = Mathf.Clamp01(_applyWeight - Time.deltaTime / duration);
                    await UniTask.NextFrame(ct);
                }
            }
        }
    }
}