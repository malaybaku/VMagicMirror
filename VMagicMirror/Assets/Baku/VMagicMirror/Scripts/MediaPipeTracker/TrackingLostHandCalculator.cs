using System;
using System.Threading;
using Baku.VMagicMirror.IK;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class TrackingLostHandCalculator
    {
        private static readonly Vector3 HandDownStartTangent = new(0, -1f, 0.1f);

        private readonly MediapipePoseSetterSettings _poseSetterSettings;
        // TODO: 普通のメソッドで受けるよりInjectしたほうがいいかも
        private AlwaysDownHandIkGenerator _downHandIk;

        private CancellationTokenSource _leftHandCts;
        private CancellationTokenSource _rightHandCts;

        private Pose _leftLastTrackedPose = Pose.identity;
        private Quaternion _leftLastTrackedLocalRotation = Quaternion.identity;

        private Pose _rightLastTrackedPose = Pose.identity;
        private Quaternion _rightLastTrackedLocalRotation = Quaternion.identity;

        private float TrackingLostWaitDuration => _poseSetterSettings.TrackingLostMotionWaitPhaseDuration;
        private float TrackingLostMotionDuration => _poseSetterSettings.TrackingLostMotionDuration;
        private float TrackingLostRotationDelay => _poseSetterSettings.TrackingLostRotationDelay; 
        private float TrackingLostPoseDuration => 
            _poseSetterSettings.TrackingLostMotionDuration + _poseSetterSettings.TrackingLostRotationDelay;

        // NOTE: Cancelを呼ばない限り、トラッキングロストの終端姿勢が適用され終わったあともずっとtrueになる
        public bool LeftHandTrackingLostRunning => _leftHandCts != null;
        public bool RightHandTrackingLostRunning => _rightHandCts != null;
        
        // NOTE: 「(Left|Right)HandTrackingLostRunningがtrueであり、かつ終端姿勢の適用までは終わってない」ときだけtrueになる
        public bool LeftHandTrackingLostMotionInProgress { get; private set; }
        public bool RightHandTrackingLostMotionInProgress { get; private set; }
        
        // NOTE:
        // - トラッキングロスト中だけ意味のある値が入る
        // - アバターのrootから見た姿勢を指定する (!= ワールド姿勢)

        // TODO: rotationが不要かも…
        public Pose LeftHandPose { get; private set; } = Pose.identity;
        public Pose RightHandPose { get; private set; } = Pose.identity;
        
        public Quaternion LeftHandLocalRotation { get; private set; } = Quaternion.identity;
        public Quaternion RightHandLocalRotation { get; private set; } = Quaternion.identity;

        [Inject]
        public TrackingLostHandCalculator(MediapipePoseSetterSettings poseSetterSettings)
        {
            _poseSetterSettings = poseSetterSettings;
        }

        public void SetupDownHandIk(AlwaysDownHandIkGenerator downHandIk) => _downHandIk = downHandIk;

        /// <summary>
        /// NOTE: lastTrackedPoseはワールド座標ではなく、アバターのrootから見た姿勢を指定する
        /// </summary>
        /// <param name="trackedPose"></param>
        /// <param name="handLocalRotation"></param>
        public void RunLeftHandTrackingLost(Pose trackedPose, Quaternion handLocalRotation)
        {
            CancelLeftHand();
            _leftHandCts = new CancellationTokenSource();

            _leftLastTrackedPose = trackedPose;
            _leftLastTrackedLocalRotation = handLocalRotation;
            LeftHandTrackingLostMotionInProgress = true;
            RunLeftHandLostInternal(_leftHandCts.Token).Forget();
        }

        /// <summary>
        /// NOTE: lastTrackedPoseはワールド座標ではなく、アバターのrootから見た姿勢を指定する
        /// </summary>
        /// <param name="trackedPose"></param>
        /// <param name="handLocalRotation"></param>
        public void RunRightHandTrackingLost(Pose trackedPose, Quaternion handLocalRotation)
        {
            CancelRightHand();
            _rightHandCts = new CancellationTokenSource();

            _rightLastTrackedPose = trackedPose;
            _rightLastTrackedLocalRotation = handLocalRotation;
            RightHandTrackingLostMotionInProgress = true;
            RunRightHandLostInternal(_rightHandCts.Token).Forget();
        }
        
        public void CancelLeftHand()
        {
            _leftHandCts?.Cancel();
            _leftHandCts?.Dispose();
            _leftHandCts = null;
            
            LeftHandTrackingLostMotionInProgress = false;
        }
        
        public void CancelRightHand()
        {
            _rightHandCts?.Cancel();
            _rightHandCts?.Dispose();
            _rightHandCts = null;
            
            RightHandTrackingLostMotionInProgress = false;
        }

        private Pose GetLeftHandDownPose() => _downHandIk.LeftHand.GetPose();
        private Pose GetRightHandTrackingLostEndPose() => _downHandIk.RightHand.GetPose();
        
        private async UniTaskVoid RunLeftHandLostInternal(CancellationToken cancellationToken)
        {
            LeftHandPose = _leftLastTrackedPose;
            LeftHandLocalRotation = _leftLastTrackedLocalRotation;
            await UniTask.Delay(TimeSpan.FromSeconds(TrackingLostWaitDuration), cancellationToken: cancellationToken);
            
            var time = 0f;
            while (time < TrackingLostPoseDuration)
            {
                var endPose = GetLeftHandDownPose();
                var positionRate = Mathf.SmoothStep(0, 1, time / TrackingLostMotionDuration);
                var position = MathUtil.GetCubicBezierWithStartTangent(
                    _leftLastTrackedPose.position, endPose.position, HandDownStartTangent, positionRate
                );

                var rotationRate =
                    Mathf.SmoothStep(0, 1, (time - TrackingLostRotationDelay) / TrackingLostMotionDuration);
                var rotation = 
                    Quaternion.Slerp(_leftLastTrackedPose.rotation, endPose.rotation, rotationRate);
                var handLocalRotation = 
                    Quaternion.Slerp(_leftLastTrackedLocalRotation, Quaternion.identity, rotationRate);

                LeftHandPose = new Pose(position, rotation);
                LeftHandLocalRotation = handLocalRotation;
                
                await UniTask.NextFrame(cancellationToken);
                time += Time.deltaTime;
            }

            LeftHandTrackingLostMotionInProgress = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                // NOTE: 設定によって手下げ姿勢が引き続き変わるかもしれないので、毎回適用している
                LeftHandPose = GetLeftHandDownPose();
                LeftHandLocalRotation = Quaternion.identity;
                await UniTask.NextFrame(cancellationToken);
            }
        }
        
        private async UniTaskVoid RunRightHandLostInternal(CancellationToken cancellationToken)
        {
            RightHandPose = _rightLastTrackedPose;
            RightHandLocalRotation = _rightLastTrackedLocalRotation;
            await UniTask.Delay(TimeSpan.FromSeconds(TrackingLostWaitDuration), cancellationToken: cancellationToken);
            
            var time = 0f;
            while (time < TrackingLostPoseDuration)
            {
                var endPose = GetRightHandTrackingLostEndPose();
                var positionRate = Mathf.SmoothStep(0, 1, time / TrackingLostMotionDuration);
                var position = MathUtil.GetCubicBezierWithStartTangent(
                    _rightLastTrackedPose.position, endPose.position, HandDownStartTangent, positionRate
                );

                var rotationRate =
                    Mathf.SmoothStep(0, 1, (time - TrackingLostRotationDelay) / TrackingLostMotionDuration);
                var rotation = 
                    Quaternion.Slerp(_rightLastTrackedPose.rotation, endPose.rotation, rotationRate);
                var handLocalRotation = 
                    Quaternion.Slerp(_rightLastTrackedLocalRotation, Quaternion.identity, rotationRate);

                RightHandPose = new Pose(position, rotation);
                RightHandLocalRotation = handLocalRotation;
                
                await UniTask.NextFrame(cancellationToken);
                time += Time.deltaTime;
            }
            
            RightHandTrackingLostMotionInProgress = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                RightHandPose = GetRightHandTrackingLostEndPose();
                RightHandLocalRotation = Quaternion.identity;
                await UniTask.NextFrame(cancellationToken);
            }
        }
    }
}
