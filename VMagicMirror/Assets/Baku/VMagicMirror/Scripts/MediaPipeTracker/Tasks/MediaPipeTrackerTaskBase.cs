using System;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // ref:
    // Holistic: https://ai.google.dev/edge/mediapipe/solutions/vision/holistic_landmarker?hl=ja
    // Hand: https://ai.google.dev/edge/mediapipe/solutions/vision/hand_landmarker?hl=ja
    // Pose: https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker?hl=ja
    // Face Detector: https://ai.google.dev/edge/mediapipe/solutions/vision/face_detector?hl=ja
    // Face Landmark: https://ai.google.dev/edge/mediapipe/solutions/vision/face_landmarker?hl=ja

    public abstract class MediaPipeTrackerTaskBase
    {
        public MediaPipeTrackerTaskBase(
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            WebCamTextureSource textureSource,
            MediaPipeKinematicSetter mediaPipeKinematicSetter,
            MediaPipeFacialValueRepository facialValueRepository,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer
        )
        {
            SettingsRepository = settingsRepository;
            _textureSource = textureSource;
            MediaPipeKinematicSetter = mediaPipeKinematicSetter;
            FacialValueRepository = facialValueRepository;
            Calibrator = calibrator;
            LandmarksVisualizer = landmarksVisualizer;
        }

        private readonly WebCamTextureSource _textureSource;

        private IDisposable _textureSourceSubscriber = null;

        protected MediaPipeKinematicSetter MediaPipeKinematicSetter { get; }
        
        protected MediaPipeFacialValueRepository FacialValueRepository { get; }

        // NOTE: visualizerはそのうち削除もアリ。Instantiateしないのが保証されてれば残ってもよいが
        protected LandmarksVisualizer LandmarksVisualizer { get; }

        protected MediaPipeTrackerRuntimeSettingsRepository SettingsRepository { get; }
        protected CameraCalibrator Calibrator { get; }

        protected int WebCamTextureWidth => _textureSource.Width;
        protected int WebCamTextureHeight => _textureSource.Height;

        /// <summary>
        /// NOTE: 横長になると1より大きくなる
        /// </summary>
        protected float WebCamTextureAspect => _textureSource.Width * 1f / _textureSource.Height;

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

        public void SetTaskActive(bool isActive)
        {
            if (isActive)
            {
                StartTask();
            }
            else
            {
                StopTask();
            }
        }
        
        public None StartTask()
        {
            // Stopしないでもシーケンス上は大丈夫だけど、まあ気になるので…
            StopTask();
            OnStartTask();

            // NOTE: OnResult的なやつが発火するまでIO<T>を無視するような実装もアリだが、
            // Mediapipeのdocによるとコールバックの発火側もよしなにdropすることがあるらしく、無視したらしたで面倒そうなので素通しする。
            // 負荷をケチる場合、そもそもtextureSource側でImageを生成するのをサボるとこまでやるのがよさそう
            _textureSourceSubscriber = _textureSource
                .ImageUpdated
                .Subscribe(OnWebCamImageUpdated);
            
            return None.Value;
        }

        public None StopTask()
        {
            OnStopTask();
            _textureSourceSubscriber?.Dispose();
            _textureSourceSubscriber = null;
            
            return None.Value;
        }
    }
}