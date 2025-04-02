using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // TODO: LateUpdateのタイミングの問題の対処だけMonoBehaviourに切り出して、このクラス自体はPresenterBaseか何かにしたい
    // NOTE
    // - このクラスではmirrorの反映、およびトラッキングロストしてるかどうかの判定までを行う
    //   - 特にトラッキングロストに関してステートフルである
    // - 姿勢の平滑化はこのクラスでは計算しない
    //   - この点ではステートレス

    /// <summary>
    /// マルチスレッドを考慮したうえでIK/FKの計算をするすごいやつだよ
    /// </summary>
    public class MediaPipeKinematicSetter : PresenterBase, ITickable
    {        
        private readonly IVRMLoadable _vrmLoadable;
        private readonly BodyScaleCalculator _bodyScaleCalculator;
        private readonly MediaPipeTrackerRuntimeSettingsRepository _settingsRepository;
        private readonly MediapipePoseSetterSettings _poseSetterSettings;
        private readonly object _poseLock = new();

        // NOTE: ポーズに関しては「トラッキング結果を保存する関数群」の部分で反転を掛けるので、内部計算はあまり反転しない
        private bool IsFaceMirrored => _settingsRepository.IsFaceMirrored.Value;
        private bool IsHandMirrored => _settingsRepository.IsHandMirrored.Value;

        // メインスレッドのみから使う値
        private bool _hasModel;
        private Animator _targetAnimator;
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new();
        
        // マルチスレッドで読み書きする値
        // Forward Kinematic
        private readonly Dictionary<HumanBodyBones, Quaternion> _rotationsBeforeIK = new();

        // Inverse Kinematic

        // NOTE:
        // - Poseの原点は「キャリブしたときに頭が映ってた位置」が期待値
        // - headPose.position は root位置の移動として反映する
        private readonly CounterBoolState _hasHeadPose = new(3, 5);
        private Pose _headPose = Pose.identity;
        // NOTE: このtimeは以下のように使う。 (left|right)Hand のほうも同じ
        // - _hasHeadPose がtrueになると積算されはじめる
        // - SetHeadPose か ClearHeadPose のいずれかでカウントをリセットする
        //   - つまり、トラッキングできてるかどうかによらず、結果が振ってさえいればリセットされ続ける
        // - 一定値を超えた場合、トラッキングロストと同様に扱って _hasHeadPose をリセットする
        private float _headResultLostTime = 0f;

        private readonly CounterBoolState _hasLeftHandPose = new(3, 5);
        private Vector2 _leftHandNormalizedPos;
        private Quaternion _leftHandRot = Quaternion.identity;
        private float _leftHandResultLostTime = 0f;

        private readonly CounterBoolState _hasRightHandPose = new(3, 5);
        private Vector2 _rightHandNormalizedPos;
        private Quaternion _rightHandRot = Quaternion.identity;
        private float _rightHandResultLostTime = 0f;
        
        [Inject]
        public MediaPipeKinematicSetter(
            IVRMLoadable vrmLoadable, 
            MediapipePoseSetterSettings poseSettings,
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            BodyScaleCalculator bodyScaleCalculator,
            MediapipePoseSetterSettings poseSetterSettings
        )
        {
            _vrmLoadable = vrmLoadable;
            _settingsRepository = settingsRepository;
            _bodyScaleCalculator = bodyScaleCalculator;
            _poseSetterSettings = poseSetterSettings;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _targetAnimator = info.animator;
            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                var boneTransform = _targetAnimator.GetBoneTransform(bone);
                if (boneTransform != null)
                {
                    _bones[bone] = boneTransform;
                }
            }
        }

        private void OnVrmUnloaded()
        {
            _targetAnimator = null;
            _bones.Clear();
        }
        
        #region トラッキング結果のI/O

        public bool HeadTracked
        {
            get
            {
                lock (_poseLock)
                {
                    return _hasHeadPose.Value;
                }
            }
        }
        
        public Pose GetHeadPose()
        {
            lock (_poseLock)
            {
                return _headPose;
            }
        }
        
        public void SetHeadPose6Dof(Pose pose)
        {
            lock (_poseLock)
            {
                var pos = new Vector3(
                    pose.position.x * _poseSetterSettings.Face6DofHorizontalScale,
                    pose.position.y * _poseSetterSettings.Face6DofHorizontalScale,
                    pose.position.z * _poseSetterSettings.Face6DofDepthScale
                );
                if (IsFaceMirrored)
                {
                    pos = MathUtil.Mirror(pos);
                    pose = MathUtil.Mirror(pose);
                }
                _headPose = new Pose(pos, pose.rotation);
                _hasHeadPose.Set(true);
                _headResultLostTime = 0f;
            }
        }
        
        public void ClearHeadPose()
        {
            lock (_poseLock)
            {
                _hasHeadPose.Set(false);
                _headResultLostTime = 0f;
            }
        }

        public bool TryGetLeftHandPose(out Pose result)
        {
            lock (_poseLock)
            {
                if (_hasHeadPose.Value && _hasLeftHandPose.Value)
                {
                    result = GetHandPose(true);
                    return true;
                }
                
                result = Pose.identity;
                return false;
            }
        }
        
        /// <summary>
        /// NOTE: posは「画像座標で、x軸方向を基準として画像のアス比の影響だけ除去したもの」を指定する。
        /// つまり、画像座標 (x, y) に対して (x, y / aspect) 的な加工をした値を指定するのが期待値。
        ///
        /// また、rotは「手のひらが+z, 中指が+yに向いてる状態」を基準とした回転を指定する。
        /// VRMのワールド回転の値ではないことに注意する。
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        public void SetLeftHandPose(Vector2 pos, Quaternion rot)
        {
            lock (_poseLock)
            {
                if (IsHandMirrored)
                {
                    SetRightHandPoseInternal(MathUtil.Mirror(pos), MathUtil.Mirror(rot));
                }
                else
                {
                    SetLeftHandPoseInternal(pos, rot);
                }
            }
        }
        
        public void ClearLeftHandPose()
        {
            lock (_poseLock)
            {
                if (IsHandMirrored)
                {
                    _hasRightHandPose.Set(false);
                    _rightHandResultLostTime = 0f;
                }
                else
                {
                    _hasLeftHandPose.Set(false);
                    _leftHandResultLostTime = 0f;
                }
            }
        }

        public bool TryGetRightHandPose(out Pose result)
        {
            lock (_poseLock)
            {
                if (_hasHeadPose.Value && _hasRightHandPose.Value)
                {
                    result = GetHandPose(false);
                    return true;
                }
                
                result = Pose.identity;
                return false;
            }
        }

        public void SetRightHandPose(Vector2 pos, Quaternion rot)
        {
            lock (_poseLock)
            {
                if (IsHandMirrored)
                {
                    SetLeftHandPoseInternal(MathUtil.Mirror(pos), MathUtil.Mirror(rot));
                }
                else
                {
                    SetRightHandPoseInternal(pos, rot);
                }
            }
        }

        public void ClearRightHandPose()
        {
            lock (_poseLock)
            {
                if (IsHandMirrored)
                {
                    _hasLeftHandPose.Set(false);
                    _leftHandResultLostTime = 0f;
                }
                else
                {
                    _hasRightHandPose.Set(false);
                    _rightHandResultLostTime = 0f;
                }
            }
        }

        // NOTE: internal系のメソッドではミラーの検証は行わない(呼び出し元がそこをケアしているのが期待される)
        private void SetLeftHandPoseInternal(Vector2 pos, Quaternion rot)
        {
            _leftHandNormalizedPos = pos;
            _leftHandRot = rot * MediapipeMathUtil.GetVrmForwardHandRotation(true);
            _hasLeftHandPose.Set(true);
            _leftHandResultLostTime = 0f;
        }

        private void SetRightHandPoseInternal(Vector2 pos, Quaternion rot)
        {
            _rightHandNormalizedPos = pos;
            _rightHandRot = rot * MediapipeMathUtil.GetVrmForwardHandRotation(false);
            _hasRightHandPose.Set(true);
            _rightHandResultLostTime = 0f;
        }

        // NOTE: Headについてはこの関数では制御せず、代わりに SetHeadPose の結果を用いる
        public void SetLocalRotationBeforeIK(HumanBodyBones bone, Quaternion localRotation)
        {
            lock (_poseLock)
            {
                // 手と頭でmirrorの状態を参照する先が異なるので注意
                var isMirrored = (int)bone >= (int)HumanBodyBones.LeftThumbProximal
                    ? IsHandMirrored
                    : IsFaceMirrored;
                
                // note: Clearを呼んでないのを良いことにテキトーにやる。VMMに組み込む場合、mirrorの切り替えで一旦FK情報を捨てる…とかをやってもよい
                if (isMirrored)
                {
                    _rotationsBeforeIK[MathUtil.Mirror(bone)] = MathUtil.Mirror(localRotation);
                }
                else
                {
                    _rotationsBeforeIK[bone] = localRotation;
                }
            }
        }

        public void ClearLocalRotationBeforeIK(HumanBodyBones bone)
        {
            lock (_poseLock)
            {
                _rotationsBeforeIK.Remove(bone);
            }
        }

        #endregion

        void ITickable.Tick()
        {
            lock (_poseLock)
            {
                if (_hasHeadPose.Value)
                {
                    _headResultLostTime += Time.deltaTime;
                    if (_headResultLostTime > _poseSetterSettings.TrackingLostTimeThreshold)
                    {
                        _hasHeadPose.Reset(false);
                        _headResultLostTime = 0f;
                    }
                }
                
                if (_hasLeftHandPose.Value)
                {
                    _leftHandResultLostTime += Time.deltaTime;
                    if (_leftHandResultLostTime > _poseSetterSettings.TrackingLostTimeThreshold)
                    {
                        _hasLeftHandPose.Reset(false);
                        _leftHandResultLostTime = 0f;
                    }
                }
                
                if (_hasRightHandPose.Value)
                {
                    _rightHandResultLostTime += Time.deltaTime;
                    if (_rightHandResultLostTime > _poseSetterSettings.TrackingLostTimeThreshold)
                    {
                        _hasRightHandPose.Reset(false);
                        _rightHandResultLostTime = 0f;
                    }
                }
            }
            

            // NOTE: ハンドトラッキングの都合で必要なものがあれば復活させてもいいが、多分クラスを分ける感じになるはず
            return;
            
            // TODO: 指の姿勢適用についても別クラスに移行する。
            // そもそもDictでキャッシュ持つのは相性悪いかもしれないので、それを直してもよい
            lock (_poseLock)
            {
                foreach (var pair in _rotationsBeforeIK)
                {
                    if (_bones.TryGetValue(pair.Key, out var boneTransform))
                    {
                        if (pair.Key >= HumanBodyBones.LeftThumbProximal)
                        {
                            boneTransform.localRotation = Quaternion.Slerp(
                                boneTransform.localRotation,
                                pair.Value,
                                _poseSetterSettings.FingerBoneSmoothRate
                            );
                        }
                        else
                        {
                            boneTransform.localRotation = pair.Value;
                        }
                    }
                }
            }
        }

        // NOTE: 大まかには位置決めのほうがメインになっている
        private Pose GetHandPose(bool isLeftHand)
        {
            var rawNormalizedPos = isLeftHand ? _leftHandNormalizedPos : _rightHandNormalizedPos;

            // NOTE: scaled~ のほうは画像座標ベースの計算に使う。 world~ のほうは実際にユーザーが手を動かした量の推定値になっている
            var scaledNormalizedPos = rawNormalizedPos * _poseSetterSettings.Hand2DofNormalizedHorizontalScale;
            var worldPosDiffXy = rawNormalizedPos * _poseSetterSettings.Hand2DofWorldHorizontalScale;
            
            // ポイント
            // - 手が伸び切らない程度に前に出すのを基本とする
            // - 画面中心から離れるほど奥行きをキャンセルして真横方向にする
            var forwardRate = Mathf.Sqrt(1 - Mathf.Clamp01(scaledNormalizedPos.sqrMagnitude));
            var handForwardOffset =
                _bodyScaleCalculator.LeftArmLength * 0.7f * _poseSetterSettings.Hand2DofDepthScale * forwardRate;

            var handPositionOffset = new Vector3(
                worldPosDiffXy.x * _bodyScaleCalculator.BodyHeightFactor,
                worldPosDiffXy.y * _bodyScaleCalculator.BodyHeightFactor,
                handForwardOffset
            );
            var handPosition = _bodyScaleCalculator.RootToHead + handPositionOffset;
         
            // weightに基づいて、補正回転のうち一部だけ適用
            var additionalRotation = Quaternion.Slerp(
                Quaternion.identity,
                GetHandAdditionalRotation(scaledNormalizedPos, isLeftHand),
                _poseSetterSettings.HandRotationModifyWeight
            );
            
            var baseRotation = isLeftHand ? _leftHandRot : _rightHandRot;
            return new Pose(handPosition, additionalRotation * baseRotation);
            // 補正計算の結果だけをチェックしたい場合はコッチを有効にする
            // return new Pose(handPosition, additionalRotation * MediapipeMathUtil.GetVrmForwardHandRotation(isLeftHand));
        }
        
        // 手の回転をおおよそ肩に対して球面的にするための補正回転を生成する
        private Quaternion GetHandAdditionalRotation(Vector2 normalizedPos, bool isLeftHand)
        {
            // NOTE: 左(右)手を画面の真ん中つまり顔や胸元に持っていく場合、手のひらが少し右(左)向きになるはず…という補正値
            var rotWhenHandIsCenter = Quaternion.Euler(0, isLeftHand ? 10 : -10, 0);

            // 画面の中心から離れると球面的に外向きの回転がつく。この値はアバターの体型には依存せず、画面座標からの変換だけで決めてしまう
            var forwardRate = Mathf.Sqrt(1 - Mathf.Clamp01(normalizedPos.sqrMagnitude));

            var direction = new Vector3(
                normalizedPos.x, normalizedPos.y, forwardRate * _poseSetterSettings.Hand2DofDepthScale
                );
            return Quaternion.LookRotation(direction) * rotWhenHandIsCenter;
        }
    }
}
