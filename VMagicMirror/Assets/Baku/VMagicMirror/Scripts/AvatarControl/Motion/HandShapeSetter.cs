using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FingerLateController))]
    public class HandShapeSetter : MonoBehaviour
    {
        //手を離す処理にかける時間(秒)
        private const float ReleaseOperationCount = 0.8f;
        
        private FingerLateController _controller = null;

        //手の制御を離すときの処理で使うカウントダウン。
        private bool _leftReleaseRunning = false;
        private float _leftReleaseCount = 0;
        private HandShapeTypes _leftShape = HandShapeTypes.Default;

        private bool _rightReleaseRunning = false;
        private float _rightReleaseCount = 0;
        private HandShapeTypes _rightShape = HandShapeTypes.Default;
        
        private void Start()
        {
            _controller = GetComponent<FingerLateController>();
        }

        private void Update()
        {
            //左手のリリース処理中の場合に行うカウントアップ
            if (_leftReleaseRunning)
            {
                _leftReleaseCount += Time.deltaTime;
                if (_leftReleaseCount > ReleaseOperationCount)
                {
                    _leftReleaseRunning = false;
                    for (int i = 0; i < 5; i++)
                    {
                        _controller.Release(i);
                    }
                }
            }

            //右手のリリース処理中の場合に行うカウントアップ
            if (_rightReleaseRunning)
            {
                _rightReleaseCount += Time.deltaTime;
                if (_rightReleaseCount > ReleaseOperationCount)
                {
                    _rightReleaseRunning = false;
                    for (int i = 5; i < 10; i++)
                    {
                        _controller.Release(i);
                    }
                }
            }
        }

        public void SetHandShape(HandTypes hand, HandShapeTypes shape)
        {
            if ((hand == HandTypes.Left && _leftShape == shape) || 
                (hand == HandTypes.Right && _rightShape == shape))
            {
                return;
            }

            if (hand == HandTypes.Left)
            {
                _leftShape = shape;
                _leftReleaseCount = 0;
                _leftReleaseRunning = (shape == HandShapeTypes.Default);
            }
            else
            {
                _rightShape = shape;
                _rightReleaseCount = 0;
                _rightReleaseRunning = (shape == HandShapeTypes.Default);
            }
            
            int offset = hand == HandTypes.Right ? 5 : 0;
            float[] angles =
                (shape == HandShapeTypes.Default) ? _defaultBendAngles :
                (shape == HandShapeTypes.Rock) ? _rockBendAngles :
                (shape == HandShapeTypes.Scissors) ? _scissorsBendAngles :
                _paperBendAngles;

            for (int i = 0; i < 5; i++)
            {
                _controller.Hold(i + offset, angles[i]);
            }
        }

        public enum HandTypes
        {
            Left = 0,
            Right = 5,
        }

        public enum HandShapeTypes
        {
            Default,
            Rock,
            Scissors,
            Paper,
        }
        
        private static readonly float[] _rockBendAngles = new float[]
        {
            30f, 55f, 55f, 60f, 70f,
        };

        private static readonly float[] _scissorsBendAngles = new float[]
        {
            50f, 0f, 0f, 60f, 70f,
        };

        private static readonly float[] _paperBendAngles = new float[]
        {
            0f, 0f, 0f, 0f, 0f,
        };

        private static readonly float[] _defaultBendAngles = new float[]
        {
            0f, 0f, 0f, 0f, 0f
        };

    }
}
