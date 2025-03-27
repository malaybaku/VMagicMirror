using System;
using Baxter;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // Holistic: https://ai.google.dev/edge/mediapipe/solutions/vision/holistic_landmarker?hl=ja
    // Hand: https://ai.google.dev/edge/mediapipe/solutions/vision/hand_landmarker?hl=ja
    // Pose: https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker?hl=ja
    // Face Detector: https://ai.google.dev/edge/mediapipe/solutions/vision/face_detector?hl=ja
    // Face Landmark: https://ai.google.dev/edge/mediapipe/solutions/vision/face_landmarker?hl=ja

    // TODO: 最終的にはMonoBehaviourをやめてVMMのPresenterBaseに移行予定
    public abstract class MediapipeTaskRunnerBase : MonoBehaviour
    {
        [SerializeField] private WebCamTextureSource textureSource;
        [SerializeField] private KinematicSetter kinematicSetter;
        [SerializeField] private FacialSetter facialSetter;
        [SerializeField] private CameraCalibrator calibrator;
        [SerializeField] private LandmarksVisualizer landmarksVisualizer;

        private IDisposable _textureSourceSubscriber = null;

        protected KinematicSetter KinematicSetter => kinematicSetter;
        protected FacialSetter FacialSetter => facialSetter;
        protected LandmarksVisualizer LandmarksVisualizer => landmarksVisualizer;
        protected CameraCalibrator Calibrator => calibrator;
        
        protected int WebCamTextureWidth => textureSource.Width;
        protected int WebCamTextureHeight => textureSource.Height;
        
        /// <summary>
        /// NOTE: 横長になると1より大きくなる
        /// </summary>
        protected float WebCamTextureAspect => textureSource.Width * 1f / textureSource.Height;
        
        protected abstract void OnStartTask();
        protected abstract void OnStopTask();
        protected abstract void OnWebCamImageUpdated(WebCamImageSource source);

        private int _onResultCalledCount;
        private long _onResultCalledPrevTimestamp;

        protected void LogOnResultCalled(long timestampMillisecond)
        {
            _onResultCalledCount++;
            // 割とテキトーに。
            if (_onResultCalledCount % 30 == 0)
            {
                var elapsedSeconds = (timestampMillisecond - _onResultCalledPrevTimestamp) * 0.001f;
                var calledRate = 30 / elapsedSeconds;
                Debug.Log($"OnResult: 30 times called, elapsed={elapsedSeconds:0.000}, fps={calledRate:0.0}");
                _onResultCalledPrevTimestamp = timestampMillisecond;
            }
        }
        
        private void OnEnable()
        {
            // Stopしないでもシーケンス上は大丈夫だけど、まあ気になるので…
            StopTask();
            StartTask();
        }
        
        private void OnDisable() => StopTask();
        
        private void StartTask()
        {
            OnStartTask();

            // NOTE: OnResult的なやつが発火するまでIO<T>を無視するような実装もアリだが、
            // Mediapipeのdocによるとコールバックの発火側もよしなにdropすることがあるらしく、無視したらしたで面倒そうなので素通しする。
            // 負荷をケチる場合、そもそもtextureSource側でImageを生成するのをサボるとこまでやるのがよさそう
            _textureSourceSubscriber = textureSource
                .ImageUpdated
                .Subscribe(OnWebCamImageUpdated);
        }

        private void StopTask()
        {
            OnStopTask();
            _textureSourceSubscriber?.Dispose();
            _textureSourceSubscriber = null;
        }
    }
}
