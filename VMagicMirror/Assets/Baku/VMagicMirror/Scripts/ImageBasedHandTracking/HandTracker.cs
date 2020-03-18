using System;
using UnityEngine;
using Zenject;

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
        
        /// <summary>
        /// 画像座標ベースで顔の左側で検出された手の情報を取得します。これは実世界における右手を意味すると考えられます。
        /// </summary>
        public TrackedHand LeftSideHand { get; } = new TrackedHand();

        /// <summary>
        /// 画像座標ベースで顔の右側で検出された手の情報を取得します。これは実世界における左手を意味すると考えられます。
        /// </summary>
        public TrackedHand RightSideHand { get; } = new TrackedHand();
        
        [Inject]
        public void Initialize(FaceTracker faceTracker)
        {
            _faceTracker = faceTracker;
            _faceTracker.FaceDetectionUpdated += OnFaceDetectionUpdated;
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
                        LeftSideHand.HasValidHandDetectResult = false;
                        RightSideHand.HasValidHandDetectResult = false;
                    }
                    _imageProcessEnabled = value;                    
                }
            }
        }

        private void OnFaceDetectionUpdated(FaceDetectionUpdateStatus status)
        {
            try
            {
                if (!ImageProcessEnabled)
                {
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

                SetDetectResult(_handAreaDetector.LeftSideResult, status, LeftSideHand);
                SetDetectResult(_handAreaDetector.RightSideResult, status, RightSideHand);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void SetDetectResult(IHandDetectResult source, FaceDetectionUpdateStatus status, TrackedHand dest)
        {
            dest.HasValidHandDetectResult = source.HasValidHandArea;
            if (!dest.HasValidHandDetectResult)
            {
                return;
            }

            //NOTE: こっから先では座標系の正規化をやってます
            dest.ReferenceFacePosition = new Vector2(
                
                status.FaceArea.center.x / status.Width - 0.5f,
                status.FaceArea.center.y / status.Height - 0.5f
            );
                
            dest.HandPosition = new Vector2(
                source.HandAreaCenter.x / status.Width - 0.5f,
                -source.HandAreaCenter.y / status.Height + 0.5f
            );
            
            dest.HandSize = new Vector2(
                source.HandAreaSize.x / status.Width,
                source.HandAreaSize.y / status.Height
            );

            //NOTE: いったん信用できない値として扱っちゃいます
            dest.HandTopOrientation = new Vector2(0, 1);

            dest.ConvexDefectCount = source.ConvexDefectVectors.Count;            
        }
    }

    
    /// <summary> 画像座標ベースで顔の左、または右にある手の検出結果をスレッドセーフに保持するためのクラス </summary>
    /// <remarks>
    /// HasValidDetectResultはいろいろなクラスから触ってよいです。
    /// それ以外は、setはHandTrackerで呼び出し、他のクラスはgetだけする、という使い方を想定してます。
    /// </remarks>
    public class TrackedHand
    {
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
            set => _referenceFacePosition.Value = value;
        }
        
        private readonly Atomic<Vector2> _handPosition = new Atomic<Vector2>();
        public Vector2 HandPosition
        {
            get => _handPosition.Value;
            set => _handPosition.Value = value;
        }

        private readonly Atomic<Vector2> _handSize = new Atomic<Vector2>();
        public Vector2 HandSize
        {
            get => _handSize.Value;
            set => _handSize.Value = value;
        }

        private readonly Atomic<int> _convexDefectCount = new Atomic<int>();
        public int ConvexDefectCount
        {
            get => _convexDefectCount.Value;
            set => _convexDefectCount.Value = value;
        }

        private readonly Atomic<Vector2> _handTopOrientation = new Atomic<Vector2>();
        public Vector2 HandTopOrientation
        {
            get => _handTopOrientation.Value;
            set => _handTopOrientation.Value = value;
        }
    }
}
