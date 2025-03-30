using System.Collections.Generic;
using RootMotion.FinalIK;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // TODO: LateUpdateのタイミングの問題の対処だけMonoBehaviourに切り出して、このクラス自体はPresenterBaseか何かにしたい

    /// <summary>
    /// マルチスレッドを考慮したうえでIK/FKを適用するすごいやつだよ
    /// </summary>
    public class KinematicSetter : PresenterBase, ITickable
    {
        // TODO: 直接書き込むのではなく IKTarget的なやつにする
        [SerializeField] private Transform leftHandIkTarget;
        [SerializeField] private Transform rightHandIkTarget;

        private MediapipePoseSetterSettings poseSettings;

        //[SerializeField] private bool isMirrorMode;
        
        // NOTE: Face Landmarkerの検出結果はかなり安定してるのでスムージングは非常に弱め landmarkはdetectorに比べると検出結果が非常に安定しているため、スムージングしない
        // いろいろなkinematicの追従weight / frame (60fps基準)
        
        [Range(0f, 1f)] [SerializeField] private float rootPositionSmoothRate = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float headFkSmoothRateDetector = 0.2f;

        private IVRMLoadable _vrmLoadable;
        private KinematicSetterTimingInvoker _timingInvoker;
        private BodyScaleCalculator _bodyScaleCalculator;
        private TrackingLostHandCalculator _trackingLostHandCalculator;
        private MediaPipeTrackerSettingsRepository _settingsRepository;
        private readonly object _poseLock = new();

        // NOTE: ポーズに関しては「トラッキング結果を保存する関数群」の部分で反転を掛けるので、内部計算はあまり反転しない
        private bool IsFaceMirrored => _settingsRepository.IsFaceMirrored.Value;
        private bool IsHandMirrored => _settingsRepository.IsHandMirrored.Value;

        // メインスレッドのみから使う値
        private bool _hasModel;
        private Animator _targetAnimator;
        private FullBodyBipedIK _fbbik;
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new();
        private Quaternion _lastTrackedLeftHandLocalRotation = Quaternion.identity;
        private Quaternion _lastTrackedRightHandLocalRotation = Quaternion.identity;
        
        // マルチスレッドで読み書きする値
        // Forward Kinematic
        private readonly Dictionary<HumanBodyBones, Quaternion> _rotationsBeforeIK = new();

        // Inverse Kinematic

        // NOTE:
        // - Poseの原点は「キャリブしたときに頭が映ってた位置」が期待値
        // - headPose.position は root位置の移動として反映する
        private readonly CounterBoolState _hasHeadPose = new(3, 5);
        private Pose _headPose = Pose.identity;

        private readonly CounterBoolState _hasLeftHandPose = new(3, 5);
        private Vector2 _leftHandNormalizedPos;
        private Quaternion _leftHandRot = Quaternion.identity;

        private readonly CounterBoolState _hasRightHandPose = new(3, 5);
        private Vector2 _rightHandNormalizedPos;
        private Quaternion _rightHandRot = Quaternion.identity;

        
        [Inject]
        public KinematicSetter(
            IVRMLoadable vrmLoadable, 
            KinematicSetterTimingInvoker timingInvoker,
            MediapipePoseSetterSettings poseSettings,
            MediaPipeTrackerSettingsRepository settingsRepository,
            BodyScaleCalculator bodyScaleCalculator,
            TrackingLostHandCalculator trackingLostHandCalculator
        )
        {
            _vrmLoadable = vrmLoadable;
            _timingInvoker = timingInvoker;
            _settingsRepository = settingsRepository;
            _bodyScaleCalculator = bodyScaleCalculator;
            _trackingLostHandCalculator = trackingLostHandCalculator;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
            
            _timingInvoker.OnLateUpdate.Subscribe(_ => OnLateUpdate()).AddTo(this);
            
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _targetAnimator = info.animator;
            _fbbik = info.fbbIk;
            
            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                var boneTransform = _targetAnimator.GetBoneTransform(bone);
                if (boneTransform != null)
                {
                    _bones[bone] = boneTransform;
                }
            }
            
            // TODO: 不要？
            var leftHandLostPose = _trackingLostHandCalculator.GetLeftHandTrackingLostEndPose();
            leftHandIkTarget.localPosition = leftHandLostPose.position;
            leftHandIkTarget.localRotation = leftHandLostPose.rotation;
            
            var rightHandLostPose = _trackingLostHandCalculator.GetRightHandTrackingLostEndPose();
            rightHandIkTarget.localPosition = rightHandLostPose.position;
            rightHandIkTarget.localRotation = rightHandLostPose.rotation;
        }

        private void OnVrmUnloaded()
        {
            _fbbik = null;
            _targetAnimator = null;

            _bones.Clear();
        }
        
        #region トラッキング結果を保存する関数群
        
        public void SetHeadPose6Dof(Pose pose)
        {
            lock (_poseLock)
            {
                var pos = new Vector3(
                    pose.position.x * poseSettings.Face6DofHorizontalScale,
                    pose.position.y * poseSettings.Face6DofHorizontalScale,
                    pose.position.z * poseSettings.Face6DofDepthScale
                );
                if (IsFaceMirrored)
                {
                    pos = MathUtil.Mirror(pos);
                    pose = MathUtil.Mirror(pose);
                }
                _headPose = new Pose(pos, pose.rotation);
                _hasHeadPose.Set(true);
            }
        }
        
        public void ClearHeadPose()
        {
            lock (_poseLock)
            {
                _hasHeadPose.Set(false);
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
                }
                else
                {
                    _hasLeftHandPose.Set(false);
                }
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
                }
                else
                {
                    _hasRightHandPose.Set(false);
                }
            }
        }

        // NOTE: internal系のメソッドではミラーの検証は行わない(呼び出し元がそこをケアしているのが期待される)
        private void SetLeftHandPoseInternal(Vector2 pos, Quaternion rot)
        {
            _leftHandNormalizedPos = pos;
            _leftHandRot = rot * MediapipeMathUtil.GetVrmForwardHandRotation(true);
            _hasLeftHandPose.Set(true);
        }

        private void SetRightHandPoseInternal(Vector2 pos, Quaternion rot)
        {
            _rightHandNormalizedPos = pos;
            _rightHandRot = rot * MediapipeMathUtil.GetVrmForwardHandRotation(false);
            _hasRightHandPose.Set(true);
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
                var rootPosition = _hasHeadPose.Value
                    ? GetHeadPositionOffset()
                    : Vector3.zero;
                _targetAnimator.transform.localPosition = Vector3.Lerp(
                    _targetAnimator.transform.localPosition,
                    rootPosition,
                    rootPositionSmoothRate
                );
                
                foreach (var pair in _rotationsBeforeIK)
                {
                    if (_bones.TryGetValue(pair.Key, out var boneTransform))
                    {
                        if (pair.Key >= HumanBodyBones.LeftThumbProximal)
                        {
                            boneTransform.localRotation = Quaternion.Slerp(
                                boneTransform.localRotation,
                                pair.Value,
                                poseSettings.FingerBoneSmoothRate
                            );
                        }
                        else
                        {
                            boneTransform.localRotation = pair.Value;
                        }
                    }
                }

                UpdateLeftHandIk();
                UpdateRightHandIk();
            }
        }

        private void UpdateLeftHandIk()
        {
            // note: headが分かってないなりにIKを適用しちゃう手もある展が、マイナーケースっぽいので無しで。
            if (_hasHeadPose.Value && _hasLeftHandPose.Value)
            {
                _trackingLostHandCalculator.CancelLeftHand();

                var leftHandPose = GetLeftHandPose();
                var nextPos =
                    Vector3.Lerp(leftHandIkTarget.localPosition, leftHandPose.position, poseSettings.HandIkSmoothRate);
                leftHandIkTarget.localPosition = Vector3.MoveTowards(
                    leftHandIkTarget.localPosition, nextPos, poseSettings.HandMoveSpeedMax * Time.deltaTime
                );
                
                leftHandIkTarget.localRotation =
                    Quaternion.Slerp(leftHandIkTarget.localRotation, leftHandPose.rotation, poseSettings.HandIkSmoothRate);
                SetLeftHandEffectorWeight(1f, 1f);
                return;
            }

            var rootPosition = _targetAnimator.transform.localPosition;
            if (!_trackingLostHandCalculator.LeftHandTrackingLostRunning)
            {
                var localRotEuler = _lastTrackedLeftHandLocalRotation.eulerAngles;
                Debug.Log($"run left hand tracking lost, local rot = {localRotEuler.x:0.0}, {localRotEuler.y:0.0}, {localRotEuler.z:0.0}");
                var lastTrackedPose = new Pose(
                    leftHandIkTarget.localPosition - rootPosition,
                    leftHandIkTarget.localRotation
                );
                _trackingLostHandCalculator.RunLeftHandTrackingLost(lastTrackedPose, _lastTrackedLeftHandLocalRotation);
            }

            leftHandIkTarget.localPosition = rootPosition + _trackingLostHandCalculator.LeftHandPose.position;
            leftHandIkTarget.localRotation = _trackingLostHandCalculator.LeftHandPose.rotation;
            
            SetLeftHandEffectorWeight(1f, 0f);
        }

        private void UpdateRightHandIk()
        {
            if (_hasHeadPose.Value && _hasRightHandPose.Value)
            {
                _trackingLostHandCalculator.CancelRightHand();

                var rightHandPose = GetRightHandPose();
                var nextPos =
                    Vector3.Lerp(rightHandIkTarget.localPosition, rightHandPose.position, poseSettings.HandIkSmoothRate);
                rightHandIkTarget.localPosition = Vector3.MoveTowards(
                    rightHandIkTarget.localPosition, nextPos, poseSettings.HandMoveSpeedMax * Time.deltaTime
                );
                
                rightHandIkTarget.localRotation =
                    Quaternion.Slerp(rightHandIkTarget.localRotation, rightHandPose.rotation, poseSettings.HandIkSmoothRate);
                SetRightHandEffectorWeight(1f, 1f);
                return;
            }
            
            var rootPosition = _targetAnimator.transform.localPosition;
            if (!_trackingLostHandCalculator.RightHandTrackingLostRunning)
            {
                var trackedPose = new Pose(
                    rightHandIkTarget.localPosition - rootPosition,
                    rightHandIkTarget.localRotation
                );
                _trackingLostHandCalculator.RunRightHandTrackingLost(trackedPose, _lastTrackedRightHandLocalRotation);
            }

            rightHandIkTarget.localPosition = rootPosition + _trackingLostHandCalculator.RightHandPose.position;
            rightHandIkTarget.localRotation = _trackingLostHandCalculator.RightHandPose.rotation;
            SetRightHandEffectorWeight(1f, 0f);
        }
        
        private void OnLateUpdate()
        {
            lock (_poseLock)
            {
                // NOTE: VMMの場合これをworld rotにするのもアリ
                if (_hasHeadPose.Value)
                {
                    _bones[HumanBodyBones.Head].localRotation = _headPose.rotation;
                }

                if (_hasHeadPose.Value && _hasLeftHandPose.Value)
                {
                    _lastTrackedLeftHandLocalRotation = _bones[HumanBodyBones.LeftHand].localRotation;
                }
                else if (_trackingLostHandCalculator.LeftHandTrackingLostRunning)
                {
                    _bones[HumanBodyBones.LeftHand].localRotation = _trackingLostHandCalculator.LeftHandLocalRotation;
                }
                
                if (_hasHeadPose.Value && _hasRightHandPose.Value)
                {
                    _lastTrackedRightHandLocalRotation = _bones[HumanBodyBones.RightHand].localRotation;
                }
                else if (_trackingLostHandCalculator.RightHandTrackingLostRunning)
                {
                    _bones[HumanBodyBones.RightHand].localRotation = _trackingLostHandCalculator.RightHandLocalRotation;
                }
            }
        }

        private void SetLeftHandEffectorWeight(float positionWeight, float rotationWeight)
        {
            _fbbik.solver.leftHandEffector.positionWeight = positionWeight;
            _fbbik.solver.leftHandEffector.rotationWeight = rotationWeight;
        }
        
        private void SetRightHandEffectorWeight(float positionWeight, float rotationWeight)
        {
            _fbbik.solver.rightHandEffector.positionWeight = positionWeight;
            _fbbik.solver.rightHandEffector.rotationWeight = rotationWeight;
        }
        
        private Pose GetLeftHandPose() => GetHandPose(true);
        private Pose GetRightHandPose() => GetHandPose(false);

        // NOTE: 大まかには位置決めのほうがメインになっている
        private Pose GetHandPose(bool isLeftHand)
        {
            var rawNormalizedPos = isLeftHand ? _leftHandNormalizedPos : _rightHandNormalizedPos;

            // NOTE: scaled~ のほうは画像座標ベースの計算に使う。 world~ のほうは実際にユーザーが手を動かした量の推定値になっている
            var scaledNormalizedPos = rawNormalizedPos * poseSettings.Hand2DofNormalizedHorizontalScale;
            var worldPosDiffXy = rawNormalizedPos * poseSettings.Hand2DofWorldHorizontalScale;
            
            // ポイント
            // - 手が伸び切らない程度に前に出すのを基本とする
            // - 画面中心から離れるほど奥行きをキャンセルして真横方向にする
            var forwardRate = Mathf.Sqrt(1 - Mathf.Clamp01(scaledNormalizedPos.sqrMagnitude));
            var handForwardOffset =
                _bodyScaleCalculator.LeftArmLength * 0.7f * poseSettings.Hand2DofDepthScale * forwardRate;

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
                poseSettings.HandRotationModifyWeight
            );
            
            var baseRotation = isLeftHand ? _leftHandRot : _rightHandRot;
            return new Pose(handPosition, additionalRotation * baseRotation);
            // 補正計算の結果だけをチェックしたい場合はコッチを有効にする
            // return new Pose(handPosition, rotation * MediapipeMathUtil.GetVrmForwardHandRotation(isLeftHand));
        }
        
        // 手の回転をおおよそ肩に対して球面的にするための補正回転を生成する
        private Quaternion GetHandAdditionalRotation(Vector2 normalizedPos, bool isLeftHand)
        {
            // NOTE: 左(右)手を画面の真ん中つまり顔や胸元に持っていく場合、手のひらが少し右(左)向きになるはず…という補正値
            var rotWhenHandIsCenter = Quaternion.Euler(0, isLeftHand ? 10 : -10, 0);

            // 画面の中心から離れると球面的に外向きの回転がつく。この値はアバターの体型には依存せず、画面座標からの変換だけで決めてしまう
            var forwardRate = Mathf.Sqrt(1 - Mathf.Clamp01(normalizedPos.sqrMagnitude));

            var direction = new Vector3(
                normalizedPos.x, normalizedPos.y, forwardRate * poseSettings.Hand2DofDepthScale
                );
            return Quaternion.LookRotation(direction) * rotWhenHandIsCenter;
        }
        
        private Vector3 GetHeadPositionOffset() => _headPose.position * _bodyScaleCalculator.BodyHeightFactor;
    }
}
