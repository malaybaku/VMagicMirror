using System;
using UnityEngine;
using Zenject;

//TODO: このクラスはほぼ全プロパティがマルチスレッドアクセスになりそうな気がする。
//ので、

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 手の位置を推定してくれるすごいやつだよ
    /// </summary>
    /// <remarks>
    /// NOTE: このクラスではローパスとか「手は左右どっちかな？」とかの推定までは行わず、
    /// 正規化っぽい処理だけをがんばります。
    /// そういう高級な処理は受け取った側ががんばります
    /// </remarks>
    public class HandTracker : MonoBehaviour
    {
        private FaceTracker _faceTracker = null;
        
        private readonly IHandAreaDetector _handAreaDetector = 
#if VMAGICMIRROR_USE_OPENCV
            new HandAreaDetector();
#else
            new EmptyHandAreaDetector();
#endif

        [Inject]
        public void Initialize(FaceTracker faceTracker)
        {
            _faceTracker = faceTracker;
            _faceTracker.FaceDetectionUpdated += OnFaceDetectionUpdated;
        }

        private void Start()
        {
            //DEBUG: UI側がまだないので強制的に画像処理をオンにします
            ImageProcessEnabled = true;
        }

        private readonly object _imageProcessEnabledLock = new object();
        private bool _imageProcessEnabled = false;
        /// <summary> 画像による手検出を行うかどうかを取得、設定します。デフォルトではfalseです。 </summary>
        public bool ImageProcessEnabled
        {
            get
            {
                lock (_imageProcessEnabledLock) return _imageProcessEnabled;
            }
            set
            {
                lock (_imageProcessEnabledLock)
                {
                    //フラグが下がった時点で手自体が検出できない状態に落としておく: このほうが色々と安全かな～と。
                    if (_imageProcessEnabled && !value)
                    {
                        HasValidHandDetectResult = false;
                    }
                    _imageProcessEnabled = value;                    
                }
            }
        }

        private readonly Atomic<bool> _hasValidHandDetectResult = new Atomic<bool>();
        public bool HasValidHandDetectResult
        {
            get => _hasValidHandDetectResult.Value;
            set => _hasValidHandDetectResult.Value = value;
        }
        
        private readonly Atomic<Vector2> _referenceFacePosition = new Atomic<Vector2>();
        public Vector2 ReferenceFacePosition
        {
            get => _referenceFacePosition.Value;
            private set => _referenceFacePosition.Value = value;
        }
        
        private readonly Atomic<Vector2> _handPosition = new Atomic<Vector2>();
        public Vector2 HandPosition
        {
            get => _handPosition.Value;
            private set => _handPosition.Value = value;
        }

        private readonly Atomic<Vector2> _handSize = new Atomic<Vector2>();
        public Vector2 HandSize
        {
            get => _handSize.Value;
            private set => _handSize.Value = value;
        }

        private readonly Atomic<int> _convexDefectCount = new Atomic<int>();
        public int ConvexDefectCount
        {
            get => _convexDefectCount.Value;
            private set => _convexDefectCount.Value = value;
        }

        private readonly Atomic<Vector2> _handTopOrientation = new Atomic<Vector2>();
        public Vector2 HandTopOrientation
        {
            get => _handTopOrientation.Value;
            private set => _handTopOrientation.Value = value;
        }

        private void Update()
        {
            //Debug.Log("Has Valid Hand Position? " + HasValidHandDetectResult);
        }

        private void OnFaceDetectionUpdated(FaceDetectionUpdateStatus status)
        {
            LogOutput.Instance.Write("OnFaceDetectionUpdated");
            try
            {
                if (!ImageProcessEnabled)
                {
                    LogOutput.Instance.Write("Not Enabled!!");
                    return;
                }

                if (status.HasValidFaceArea)
                {
                    _handAreaDetector.UpdateHandDetection(status.Image, status.Width, status.Height, status.FaceArea);
                }
                else
                {
                    _handAreaDetector.UpdateHandDetectionWithoutFace();
                }

                HasValidHandDetectResult = _handAreaDetector.HasValidHandArea;
                if (!HasValidHandDetectResult)
                {
                    LogOutput.Instance.Write("Hand was not detected.");
                    return;
                }

                //NOTE: こっから先では座標系を直しながらアレしていきます
                //とりあえずテキトーに書いてから考えましょうか。
                ReferenceFacePosition = new Vector2(
                    status.FaceArea.center.x / status.Width - 0.5f,
                    status.FaceArea.center.y / status.Height - 0.5f
                );
                
                HandPosition = new Vector2(
                    _handAreaDetector.HandAreaCenter.x / status.Width - 0.5f,
                    -_handAreaDetector.HandAreaCenter.y / status.Height + 0.5f
                );

                HandSize = new Vector2(
                    _handAreaDetector.HandAreaSize.x / status.Width,
                    _handAreaDetector.HandAreaSize.y / status.Height
                );

                //NOTE: いったん信用できない値として扱っちゃいます
                HandTopOrientation = new Vector2(0, 1);

                ConvexDefectCount = _handAreaDetector.ConvexDefectVectors.Count;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
    }
}
