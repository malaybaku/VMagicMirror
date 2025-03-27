using System.Collections.Generic;
using Baku.VMagicMirror.MediaPipeTracker;
using RootMotion.FinalIK;
using UnityEngine;

namespace Baxter
{
    /// <summary>
    /// マルチスレッドを考慮したうえでIK/FKを適用するすごいやつだよ
    /// </summary>
    public class KinematicSetter : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private Transform leftHandIkTarget;
        [SerializeField] private Transform rightHandIkTarget;

        [SerializeField] private MediapipePoseSetterSettings poseSetterSettings;

        // NOTE: mirrorのアプローチとしては「トラッキング結果を保存する関数群」の部分で反転しているので、内部計算はあんまり反転しない
        [SerializeField] private bool isMirrorMode;
        
        // いろいろなkinematicの追従weight / frame (60fps基準)
        [Range(0f, 1f)] [SerializeField] private float rootPositionSmoothRate = 0.3f;
        // NOTE: landmarkはdetectorに比べると検出結果が非常に安定しているため、スムージングしない
        [Range(0f, 1f)] [SerializeField] private float headFkSmoothRateDetector = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float handIkSmoothRate = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float fingerBoneSmoothRate = 0.3f;

        // NOTE: アバターが極端に大きい場合は上限を緩和したほうがいいが、あんまり深く考えたくない…
        // 手のIKの1frameあたり最大の移動距離。とくにトラッキング中に適用される。
        [SerializeField] private float handMoveSpeedMax = 1.4f;
        
        private FullBodyBipedIK _fbbik;
        private FullBodyBipedIK Fbbik => _fbbik ??= targetAnimator.GetComponent<FullBodyBipedIK>();
        
        private readonly BodyScaleCalculator _bodyScaleCalculator = new();
        private readonly TrackingLostHandCalculator _trackingLostHandCalculator = new();
        
        private readonly object _poseLock = new();

        // メインスレッドのみから使う値
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
        private bool _isLandmarkBasedPose;
        private Quaternion _prevAppliedHeadRotation = Quaternion.identity;

        private readonly CounterBoolState _hasLeftHandPose = new(3, 5);
        private Vector2 _leftHandNormalizedPos;
        private Quaternion _leftHandRot = Quaternion.identity;

        private readonly CounterBoolState _hasRightHandPose = new(3, 5);
        private Vector2 _rightHandNormalizedPos;
        private Quaternion _rightHandRot = Quaternion.identity;

        public BodyScaleCalculator BodyScaleCalculator => _bodyScaleCalculator;

        #region トラッキング結果を保存する関数群
        
        public void SetHeadPose6Dof(Pose pose)
        {
            lock (_poseLock)
            {
                var pos = new Vector3(
                    pose.position.x * poseSetterSettings.Face6DofHorizontalScale,
                    pose.position.y * poseSetterSettings.Face6DofHorizontalScale,
                    pose.position.z * poseSetterSettings.Face6DofDepthScale
                );
                if (isMirrorMode)
                {
                    pos = MathUtil.Mirror(pos);
                    pose = MathUtil.Mirror(pose);
                }
                _headPose = new Pose(pos, pose.rotation);
                _isLandmarkBasedPose = true;
                _hasHeadPose.Set(true);
            }
        }

        public void SetHeadPose2Dof(Vector2 normalizedPos, Quaternion rotation)
        {
            lock (_poseLock)
            {
                // normalizedPosからはxy成分だけが分かるので、奥行き成分は無しにして位置を決める
                var scaledPosition = 
                    poseSetterSettings.Body2DoFScale * new Vector3(normalizedPos.x, normalizedPos.y, 0f);
                if (isMirrorMode)
                {
                    scaledPosition = MathUtil.Mirror(scaledPosition);
                    rotation = MathUtil.Mirror(rotation);
                }
                _headPose = new Pose(scaledPosition, rotation);
                _isLandmarkBasedPose = false;
                _hasHeadPose.Set(true);
            }
        }
        
        public void ClearHeadPose()
        {
            lock (_poseLock)
            {
                _hasHeadPose.Set(false);
                _prevAppliedHeadRotation = Quaternion.identity;
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
                if (isMirrorMode)
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
                if (isMirrorMode)
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
                if (isMirrorMode)
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
                if (isMirrorMode)
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
                // note: Clearを呼んでないのを良いことにテキトーにやる。VMMに組み込む場合、mirrorの切り替えで一旦FK情報を捨てる…とかをやってもよい
                if (isMirrorMode)
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

        private void Start()
        {
            if (targetAnimator == null || leftHandIkTarget == null || rightHandIkTarget == null || Fbbik == null)
            {
                Debug.LogError("there are something missed in kinematic setter setup");
                return;
            }

            _bodyScaleCalculator.Calculate(targetAnimator);
            _trackingLostHandCalculator.SetupAnimator(targetAnimator);

            var leftHandLostPose = _trackingLostHandCalculator.GetLeftHandTrackingLostEndPose();
            leftHandIkTarget.localPosition = leftHandLostPose.position;
            leftHandIkTarget.localRotation = leftHandLostPose.rotation;
            
            var rightHandLostPose = _trackingLostHandCalculator.GetRightHandTrackingLostEndPose();
            rightHandIkTarget.localPosition = rightHandLostPose.position;
            rightHandIkTarget.localRotation = rightHandLostPose.rotation;

            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                var boneTransform = targetAnimator.GetBoneTransform(bone);
                if (boneTransform != null)
                {
                    _bones[bone] = boneTransform;
                }
            }
        }

        private void Update()
        {
            lock (_poseLock)
            {
                var rootPosition = _hasHeadPose.Value
                    ? GetHeadPositionOffset()
                    : Vector3.zero;
                targetAnimator.transform.localPosition = Vector3.Lerp(
                    targetAnimator.transform.localPosition,
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
                                fingerBoneSmoothRate
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
                    Vector3.Lerp(leftHandIkTarget.localPosition, leftHandPose.position, handIkSmoothRate);
                leftHandIkTarget.localPosition = Vector3.MoveTowards(
                    leftHandIkTarget.localPosition, nextPos, handMoveSpeedMax * Time.deltaTime
                );
                
                leftHandIkTarget.localRotation =
                    Quaternion.Slerp(leftHandIkTarget.localRotation, leftHandPose.rotation, handIkSmoothRate);
                SetLeftHandEffectorWeight(1f, 1f);
                return;
            }

            var rootPosition = targetAnimator.transform.localPosition;
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
                    Vector3.Lerp(rightHandIkTarget.localPosition, rightHandPose.position, handIkSmoothRate);
                rightHandIkTarget.localPosition = Vector3.MoveTowards(
                    rightHandIkTarget.localPosition, nextPos, handMoveSpeedMax * Time.deltaTime
                );
                
                rightHandIkTarget.localRotation =
                    Quaternion.Slerp(rightHandIkTarget.localRotation, rightHandPose.rotation, handIkSmoothRate);
                SetRightHandEffectorWeight(1f, 1f);
                return;
            }
            
            var rootPosition = targetAnimator.transform.localPosition;
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
        
        private void LateUpdate()
        {
            lock (_poseLock)
            {
                // NOTE: VMMの場合これをworld rotにするのもアリ
                if (_hasHeadPose.Value)
                {
                    var nextHeadLocalRotation = _isLandmarkBasedPose
                        ? _headPose.rotation
                        : Quaternion.Slerp(
                            _prevAppliedHeadRotation,
                            _headPose.rotation,
                            headFkSmoothRateDetector
                        );
                    _bones[HumanBodyBones.Head].localRotation = nextHeadLocalRotation;
                    _prevAppliedHeadRotation = nextHeadLocalRotation;
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
            Fbbik.solver.leftHandEffector.positionWeight = positionWeight;
            Fbbik.solver.leftHandEffector.rotationWeight = rotationWeight;
        }
        
        private void SetRightHandEffectorWeight(float positionWeight, float rotationWeight)
        {
            Fbbik.solver.rightHandEffector.positionWeight = positionWeight;
            Fbbik.solver.rightHandEffector.rotationWeight = rotationWeight;
        }
        
        private Pose GetLeftHandPose() => GetHandPose(true);
        private Pose GetRightHandPose() => GetHandPose(false);

        // NOTE: 大まかには位置決めのほうがメインになっている
        private Pose GetHandPose(bool isLeftHand)
        {
            var rawNormalizedPos = isLeftHand ? _leftHandNormalizedPos : _rightHandNormalizedPos;

            // NOTE: scaled~ のほうは画像座標ベースの計算に使う。 world~ のほうは実際にユーザーが手を動かした量の推定値になっている
            var scaledNormalizedPos = rawNormalizedPos * poseSetterSettings.Hand2DofNormalizedHorizontalScale;
            var worldPosDiffXy = rawNormalizedPos * poseSetterSettings.Hand2DofWorldHorizontalScale;
            
            // ポイント
            // - 手が伸び切らない程度に前に出すのを基本とする
            // - 画面中心から離れるほど奥行きをキャンセルして真横方向にする
            var forwardRate = Mathf.Sqrt(1 - Mathf.Clamp01(scaledNormalizedPos.sqrMagnitude));
            var handForwardOffset =
                BodyScaleCalculator.ArmLength * 0.7f * poseSetterSettings.Hand2DofDepthScale * forwardRate;

            var handPositionOffset = new Vector3(
                worldPosDiffXy.x * BodyScaleCalculator.BodyHeightFactor,
                worldPosDiffXy.y * BodyScaleCalculator.BodyHeightFactor,
                handForwardOffset
            );
            var handPosition = BodyScaleCalculator.RootToHead + handPositionOffset;
         
            // weightに基づいて、補正回転のうち一部だけ適用
            var additionalRotation = Quaternion.Slerp(
                Quaternion.identity,
                GetHandAdditionalRotation(scaledNormalizedPos, isLeftHand),
                poseSetterSettings.HandRotationModifyWeight
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
                normalizedPos.x, normalizedPos.y, forwardRate * poseSetterSettings.Hand2DofDepthScale
                );
            return Quaternion.LookRotation(direction) * rotWhenHandIsCenter;
        }
        
        private Vector3 GetHeadPositionOffset() => _headPose.position * BodyScaleCalculator.BodyHeightFactor;
    }
}
