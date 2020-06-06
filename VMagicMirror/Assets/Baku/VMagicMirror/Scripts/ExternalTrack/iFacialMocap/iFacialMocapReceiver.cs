using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker.iFacialMocap
{
    /// <summary> iFacialMocapから顔トラッキングデータを受け取ってVMagicMirrorのデータ形状に整形するクラスです。 </summary>
    public class iFacialMocapReceiver : ExternalTrackSourceProvider
    {
        //NOTE: iFacialMocapはPCサイドの中間UIがとーっても難しいのでポート番号を固定します。下手にいじらせると死人が出そう…。
        private const int LOCAL_PORT = 50003;

        //NOTE: ちょっと倒錯してるんだけど、目のrotation情報からもとのブレンドシェイプを推定するためにこの値を使います。
        [SerializeField] private float eyeHorizontalRotationToBlendShapeFactor = 5.0f;
        [SerializeField] private float eyeVerticalRotationToBlendShapeFactor = 5.0f;
        
        private readonly RecordFaceTrackSource _faceTrackSource = new RecordFaceTrackSource();
        public override IFaceTrackSource FaceTrackSource => _faceTrackSource;
        public override bool SupportHandTracking => false;
        public override bool SupportFacePositionOffset => false;
        public override Quaternion HeadRotation => _offsetRotation * _faceTrackSource.FaceTransform.Rotation;

        private CancellationTokenSource _cts = null;
        private readonly object _rawMessageLock = new object();
        private string _rawMessage = "";
        private string RawMessage
        {
            get
            {
                lock (_rawMessageLock) return _rawMessage;
            }
            set
            {
                lock (_rawMessageLock) _rawMessage = value;
            }
        }
        //NOTE: 最終的に無くてよい。あくまでデバッグ用なので
        private ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
        
        
        private readonly Dictionary<string, float> _blendShapes = new Dictionary<string, float>()
        {
            //目
            [RawBlendShapeNames.eyeBlinkLeft] = 0.0f,
            [RawBlendShapeNames.eyeLookUpLeft] = 0.0f,
            [RawBlendShapeNames.eyeLookDownLeft] = 0.0f,
            [RawBlendShapeNames.eyeLookInLeft] = 0.0f,
            [RawBlendShapeNames.eyeLookOutLeft] = 0.0f,
            [RawBlendShapeNames.eyeWideLeft] = 0.0f,
            [RawBlendShapeNames.eyeSquintLeft] = 0.0f,

            [RawBlendShapeNames.eyeBlinkRight] = 0.0f,
            [RawBlendShapeNames.eyeLookUpRight] = 0.0f,
            [RawBlendShapeNames.eyeLookDownRight] = 0.0f,
            [RawBlendShapeNames.eyeLookInRight] = 0.0f,
            [RawBlendShapeNames.eyeLookOutRight] = 0.0f,
            [RawBlendShapeNames.eyeWideRight] = 0.0f,
            [RawBlendShapeNames.eyeSquintRight] = 0.0f,

            //あご
            [RawBlendShapeNames.jawOpen] = 0.0f,
            [RawBlendShapeNames.jawForward] = 0.0f,
            [RawBlendShapeNames.jawLeft] = 0.0f,
            [RawBlendShapeNames.jawRight] = 0.0f,

            //まゆげ
            [RawBlendShapeNames.browDownLeft] = 0.0f,
            [RawBlendShapeNames.browOuterUpLeft] = 0.0f,
            [RawBlendShapeNames.browDownRight] = 0.0f,
            [RawBlendShapeNames.browOuterUpRight] = 0.0f,
            [RawBlendShapeNames.browInnerUp] = 0.0f,

            //口(多い)
            [RawBlendShapeNames.mouthLeft] = 0.0f,
            [RawBlendShapeNames.mouthSmileLeft] = 0.0f,
            [RawBlendShapeNames.mouthFrownLeft] = 0.0f,
            [RawBlendShapeNames.mouthPressLeft] = 0.0f,
            [RawBlendShapeNames.mouthUpperUpLeft] = 0.0f,
            [RawBlendShapeNames.mouthLowerDownLeft] = 0.0f,
            [RawBlendShapeNames.mouthStretchLeft] = 0.0f,
            [RawBlendShapeNames.mouthDimpleLeft] = 0.0f,

            [RawBlendShapeNames.mouthRight] = 0.0f,
            [RawBlendShapeNames.mouthSmileRight] = 0.0f,
            [RawBlendShapeNames.mouthFrownRight] = 0.0f,
            [RawBlendShapeNames.mouthPressRight] = 0.0f,
            [RawBlendShapeNames.mouthUpperUpRight] = 0.0f,
            [RawBlendShapeNames.mouthLowerDownRight] = 0.0f,
            [RawBlendShapeNames.mouthStretchRight] = 0.0f,
            [RawBlendShapeNames.mouthDimpleRight] = 0.0f,
            
            [RawBlendShapeNames.mouthClose] = 0.0f,
            [RawBlendShapeNames.mouthFunnel] = 0.0f,
            [RawBlendShapeNames.mouthPucker] = 0.0f,
            [RawBlendShapeNames.mouthShrugUpper] = 0.0f,
            [RawBlendShapeNames.mouthShrugLower] = 0.0f,
            [RawBlendShapeNames.mouthRollUpper] = 0.0f,
            [RawBlendShapeNames.mouthRollLower] = 0.0f,

            //鼻
            [RawBlendShapeNames.noseSneerLeft] = 0.0f,
            [RawBlendShapeNames.noseSneerRight] = 0.0f,

            //ほお
            [RawBlendShapeNames.cheekPuff] = 0.0f,
            [RawBlendShapeNames.cheekSquintLeft] = 0.0f,
            [RawBlendShapeNames.cheekSquintRight] = 0.0f,
            
            //舌
            [RawBlendShapeNames.tongueOut] = 0.0f,         
        };

        //NOTE: headとneckは分けてるが最終的に合成値を公開する予定。
        //(ARKit的には単一の回転値だけ取る仕様だった気がするので、iFacialMocap側が勝手に値を分配している、と想定してそうする)
        private readonly Dictionary<string, Vector3> _rotationData = new Dictionary<string, Vector3>()
        {
            [iFacialMocapRotationNames.head] = Vector3.zero,
            //[iFacialMocapRotationNames.neck] = Vector3.zero,
            [iFacialMocapRotationNames.leftEye] = Vector3.zero,
            [iFacialMocapRotationNames.rightEye] = Vector3.zero,
        };
        
        private void Update()
        {
            while (_messages.TryDequeue(out string msg))
            {
                //Debug.Log("iFacialMocap: " + msg);
            }

            //明確にStartしてからStopするまでの途中でのみデシリアライズを行うようガード
            if (_cts == null)
            {
                return;
            }

            string message = RawMessage;
            if (!string.IsNullOrEmpty(message))
            {
                RawMessage = "";
                DeserializeFaceMessage(message);
                ApplyDeserializeResult();
                RaiseFaceTrackUpdated();
            }
        }

        private void OnDestroy()
        {
            StopReceive();
        }


        
        #region 受信ルーチンまわり
        
        public override void StartReceive()
        {
            Debug.Log("Start iFacialMocap receiver");
            StopReceive();
            _cts = new CancellationTokenSource();
            new Thread(() => ThreadMethod(_cts.Token)).Start();
        }
        
        public override void StopReceive()
        {
            _cts?.Cancel();
            _cts = null;
            RawMessage = "";
        }

        private void ThreadMethod(CancellationToken token)
        {
            var client = new UdpClient(LOCAL_PORT);
            client.Client.ReceiveTimeout = 500;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] data = client.Receive(ref remoteEndPoint);
                    string message = Encoding.ASCII.GetString(data);
                    _messages.Enqueue(message);
                    RawMessage = message;
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }

            try
            {
                client?.Close();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        #endregion
        
        #region デシリアライズまわり
        
        private void DeserializeFaceMessage(string msg)
        {
            //NOTE: この処理Allocちょっと多いので注意。多分ボトルネックにはならないだろ、と思って適当にやってるけど。
            var phrases = msg.Split('=');
            if (phrases.Length != 2)
            {
                return;
            }

            //0 ~ 100のブレンドシェイプが載っている部分
            var blendShapes = phrases[0].Split('|');
            for (int i = 0; i < blendShapes.Length; i++)
            {
                var item = blendShapes[i].Split('-');
                if (item.Length == 2 && 
                    _blendShapes.ContainsKey(item[0]) &&
                    int.TryParse(item[1], out int value)
                    )
                {
                    _blendShapes[item[0]] = Mathf.Clamp01(value * 0.01f);
                }
            }
            
            //回転値が書いてある部分(note: そのうち位置も増えるかもね)
            var rotations = phrases[1].Split('|');
            for (int i = 0; i < rotations.Length; i++)
            {
                var item = rotations[i].Split('#');
                if (item.Length != 2 || !_rotationData.ContainsKey(item[0]))
                {
                    continue;
                }

                var rotEuler = item[1].Split(',');
                if (rotEuler.Length == 3 && 
                    float.TryParse(rotEuler[0], out float x) && 
                    float.TryParse(rotEuler[1], out float y) && 
                    float.TryParse(rotEuler[2], out float z)
                    )
                {
                    _rotationData[item[0]] = new Vector3(x, y, z);
                }
            }

        }

        private void ApplyDeserializeResult()
        {
            //NOTE: iFacialMocap特有の処理として、目が回転情報で飛んできてしまうので、それをあえて逆算します。
            ApplyHeadTransform();
            ApplyEyes();
            ApplyMouth();
            ApplyOtherBlendShapes();

            void ApplyHeadTransform()
            {
                _faceTrackSource.FaceTransform.HasValidPosition = false;
                _faceTrackSource.FaceTransform.Rotation =
                    Quaternion.Euler(_rotationData[iFacialMocapRotationNames.head]);
                //* Quaternion.Euler(_rotationData[iFacialMocapRotationNames.neck]);
            }
            
            //目: みどころは回転 to ブレンドシェイプの逆変換(ちょっと面倒な話だけどね。)
            void ApplyEyes()
            {
                _faceTrackSource.Eye.LeftBlink = _blendShapes[RawBlendShapeNames.eyeBlinkLeft];
                _faceTrackSource.Eye.LeftSquint = _blendShapes[RawBlendShapeNames.eyeSquintLeft];
                _faceTrackSource.Eye.LeftWide = _blendShapes[RawBlendShapeNames.eyeWideLeft];
                var leftEyeEuler = _rotationData[iFacialMocapRotationNames.leftEye];
                float leftEyeHorizontal = Mathf.Clamp(leftEyeEuler.y * eyeHorizontalRotationToBlendShapeFactor, -1, 1);
                if (leftEyeHorizontal > 0)
                {
                    _faceTrackSource.Eye.LeftLookIn = 0;
                    _faceTrackSource.Eye.LeftLookOut = leftEyeHorizontal;
                }
                else
                {
                    _faceTrackSource.Eye.LeftLookIn = -leftEyeHorizontal;
                    _faceTrackSource.Eye.LeftLookOut = 0;
                }

                float leftEyeVertical = Mathf.Clamp(leftEyeEuler.x * eyeVerticalRotationToBlendShapeFactor, -1, 1);
                if (leftEyeVertical > 0)
                {
                    _faceTrackSource.Eye.LeftLookUp = 0;
                    _faceTrackSource.Eye.LeftLookDown = leftEyeVertical;
                }
                else
                {
                    _faceTrackSource.Eye.LeftLookUp = -leftEyeVertical;
                    _faceTrackSource.Eye.LeftLookDown = 0;
                }

                _faceTrackSource.Eye.RightBlink = _blendShapes[RawBlendShapeNames.eyeBlinkRight];
                _faceTrackSource.Eye.RightSquint = _blendShapes[RawBlendShapeNames.eyeSquintRight];
                _faceTrackSource.Eye.RightWide = _blendShapes[RawBlendShapeNames.eyeWideRight];
                var rightEyeEuler = _rotationData[iFacialMocapRotationNames.rightEye];
                float rightEyeHorizontal =
                    Mathf.Clamp(rightEyeEuler.y * eyeHorizontalRotationToBlendShapeFactor, -1, 1);
                if (rightEyeHorizontal > 0)
                {
                    _faceTrackSource.Eye.RightLookIn = 0;
                    _faceTrackSource.Eye.RightLookOut = rightEyeHorizontal;
                }
                else
                {
                    _faceTrackSource.Eye.RightLookIn = -rightEyeHorizontal;
                    _faceTrackSource.Eye.RightLookOut = 0;
                }

                float rightEyeVertical = Mathf.Clamp(rightEyeEuler.x * eyeVerticalRotationToBlendShapeFactor, -1, 1);
                if (rightEyeVertical > 0)
                {
                    _faceTrackSource.Eye.RightLookUp = 0;
                    _faceTrackSource.Eye.RightLookDown = rightEyeVertical;
                }
                else
                {
                    _faceTrackSource.Eye.RightLookUp = -rightEyeVertical;
                    _faceTrackSource.Eye.RightLookDown = 0;
                }            
            }

            //口: 単純に数が多い！
            void ApplyMouth()
            {
                _faceTrackSource.Mouth.Left = _blendShapes[RawBlendShapeNames.mouthLeft];
                _faceTrackSource.Mouth.LeftSmile = _blendShapes[RawBlendShapeNames.mouthSmileLeft];
                _faceTrackSource.Mouth.LeftFrown = _blendShapes[RawBlendShapeNames.mouthFrownLeft];
                _faceTrackSource.Mouth.LeftPress = _blendShapes[RawBlendShapeNames.mouthPressLeft];
                _faceTrackSource.Mouth.LeftUpperUp = _blendShapes[RawBlendShapeNames.mouthUpperUpLeft];
                _faceTrackSource.Mouth.LeftLowerDown = _blendShapes[RawBlendShapeNames.mouthLowerDownLeft];
                _faceTrackSource.Mouth.LeftStretch = _blendShapes[RawBlendShapeNames.mouthStretchLeft];
                _faceTrackSource.Mouth.LeftDimple = _blendShapes[RawBlendShapeNames.mouthDimpleLeft];

                _faceTrackSource.Mouth.Right = _blendShapes[RawBlendShapeNames.mouthRight];
                _faceTrackSource.Mouth.RightSmile = _blendShapes[RawBlendShapeNames.mouthSmileRight];
                _faceTrackSource.Mouth.RightFrown = _blendShapes[RawBlendShapeNames.mouthFrownRight];
                _faceTrackSource.Mouth.RightPress = _blendShapes[RawBlendShapeNames.mouthPressRight];
                _faceTrackSource.Mouth.RightUpperUp = _blendShapes[RawBlendShapeNames.mouthUpperUpRight];
                _faceTrackSource.Mouth.RightLowerDown = _blendShapes[RawBlendShapeNames.mouthLowerDownRight];
                _faceTrackSource.Mouth.RightStretch = _blendShapes[RawBlendShapeNames.mouthStretchRight];
                _faceTrackSource.Mouth.RightDimple = _blendShapes[RawBlendShapeNames.mouthDimpleRight];

                _faceTrackSource.Mouth.Close = _blendShapes[RawBlendShapeNames.mouthClose];
                _faceTrackSource.Mouth.Funnel = _blendShapes[RawBlendShapeNames.mouthFunnel];
                _faceTrackSource.Mouth.Pucker = _blendShapes[RawBlendShapeNames.mouthPucker];
                _faceTrackSource.Mouth.ShrugUpper = _blendShapes[RawBlendShapeNames.mouthShrugUpper];
                _faceTrackSource.Mouth.ShrugLower = _blendShapes[RawBlendShapeNames.mouthShrugLower];
                _faceTrackSource.Mouth.RollUpper = _blendShapes[RawBlendShapeNames.mouthRollUpper];
                _faceTrackSource.Mouth.RollLower = _blendShapes[RawBlendShapeNames.mouthRollLower];
            }

            //その他いろいろ
            void ApplyOtherBlendShapes()
            {
                _faceTrackSource.Brow.InnerUp = _blendShapes[RawBlendShapeNames.browInnerUp];
                _faceTrackSource.Brow.LeftDown = _blendShapes[RawBlendShapeNames.browDownLeft];
                _faceTrackSource.Brow.LeftOuterUp = _blendShapes[RawBlendShapeNames.browOuterUpLeft];
                _faceTrackSource.Brow.RightDown = _blendShapes[RawBlendShapeNames.browDownRight];
                _faceTrackSource.Brow.RightOuterUp = _blendShapes[RawBlendShapeNames.browOuterUpRight];
            
                _faceTrackSource.Jaw.Open = _blendShapes[RawBlendShapeNames.jawOpen];
                _faceTrackSource.Jaw.Forward = _blendShapes[RawBlendShapeNames.jawForward];
                _faceTrackSource.Jaw.Left = _blendShapes[RawBlendShapeNames.jawLeft];
                _faceTrackSource.Jaw.Right = _blendShapes[RawBlendShapeNames.jawRight];
                
                _faceTrackSource.Nose.LeftSneer = _blendShapes[RawBlendShapeNames.noseSneerLeft];
                _faceTrackSource.Nose.RightSneer = _blendShapes[RawBlendShapeNames.noseSneerRight];

                _faceTrackSource.Cheek.Puff = _blendShapes[RawBlendShapeNames.cheekPuff];
                _faceTrackSource.Cheek.LeftSquint = _blendShapes[RawBlendShapeNames.cheekSquintLeft];
                _faceTrackSource.Cheek.RightSquint = _blendShapes[RawBlendShapeNames.cheekSquintRight];
                
                _faceTrackSource.Tongue.TongueOut = _blendShapes[RawBlendShapeNames.tongueOut];
            }
        }

        #endregion
        
        #region キャリブレーション

        public override void Calibrate()
        {
            _trackerRotationEulerAngle = Quaternion.Inverse(_faceTrackSource.FaceTransform.Rotation).eulerAngles;
        }

        //NOTE: キャリブ情報の実態 = トラッカーデバイスがUnity空間にあったとみなした場合のワールド回転をオイラー角表現したもの。
        //こういう値だとUnity空間上にモノを置いて図示する余地があり、筋がいい
        private Vector3 _trackerRotationEulerAngle = Vector3.zero;
        private Quaternion _offsetRotation = Quaternion.identity;
        public string CalibrationData
        {
            get
            {
                var data = new IFacialMocapCalibrationData()
                {
                    rotX = _trackerRotationEulerAngle.x,
                    rotY = _trackerRotationEulerAngle.y,
                    rotZ = _trackerRotationEulerAngle.z,
                };
                return JsonUtility.ToJson(data);
            }
            set
            {
                try
                {
                    var data = JsonUtility.FromJson<IFacialMocapCalibrationData>(value);
                    _trackerRotationEulerAngle = new Vector3(data.rotX, data.rotY, data.rotZ);
                    _offsetRotation = Quaternion.Inverse(Quaternion.Euler(_trackerRotationEulerAngle));
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }

        #endregion
    }

    [Serializable]
    public class IFacialMocapCalibrationData
    {
        public float rotX;
        public float rotY;
        public float rotZ;
    }
}