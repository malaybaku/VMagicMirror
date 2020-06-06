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
        private const int PortNumber = 49983;
        
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
            [iFacialMocapBlendShapeNames.eyeBlinkLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeLookUpLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeLookDownLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeLookInLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeLookOutLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeWideLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeSquintLeft] = 0.0f,

            [iFacialMocapBlendShapeNames.eyeBlinkRight] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeLookUpRight] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeLookDownRight] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeLookInRight] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeLookOutRight] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeWideRight] = 0.0f,
            [iFacialMocapBlendShapeNames.eyeSquintRight] = 0.0f,

            //あご
            [iFacialMocapBlendShapeNames.jawOpen] = 0.0f,
            [iFacialMocapBlendShapeNames.jawForward] = 0.0f,
            [iFacialMocapBlendShapeNames.jawLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.jawRight] = 0.0f,

            //まゆげ
            [iFacialMocapBlendShapeNames.browDownLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.browOuterUpLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.browDownRight] = 0.0f,
            [iFacialMocapBlendShapeNames.browOuterUpRight] = 0.0f,
            [iFacialMocapBlendShapeNames.browInnerUp] = 0.0f,

            //口(多い)
            [iFacialMocapBlendShapeNames.mouthLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthSmileLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthFrownLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthPressLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthUpperUpLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthLowerDownLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthStretchLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthDimpleLeft] = 0.0f,

            [iFacialMocapBlendShapeNames.mouthRight] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthSmileRight] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthFrownRight] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthPressRight] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthUpperUpRight] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthLowerDownRight] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthStretchRight] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthDimpleRight] = 0.0f,
            
            [iFacialMocapBlendShapeNames.mouthClose] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthFunnel] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthPucker] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthShrugUpper] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthShrugLower] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthRollUpper] = 0.0f,
            [iFacialMocapBlendShapeNames.mouthRollLower] = 0.0f,

            //鼻
            [iFacialMocapBlendShapeNames.noseSneerLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.noseSneerRight] = 0.0f,

            //ほお
            [iFacialMocapBlendShapeNames.cheekPuff] = 0.0f,
            [iFacialMocapBlendShapeNames.cheekSquintLeft] = 0.0f,
            [iFacialMocapBlendShapeNames.cheekSquintRight] = 0.0f,
            
            //舌
            [iFacialMocapBlendShapeNames.tongueOut] = 0.0f,         
        };

        //NOTE: 生データでは目のオイラー角表現が入ってるけど無視(ブレンドシェイプ側の情報を使う為)
        private readonly Dictionary<string, Vector3> _rotationData = new Dictionary<string, Vector3>()
        {
            [iFacialMocapRotationNames.head] = Vector3.zero,
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
            var client = new UdpClient(PortNumber);
            client.Client.ReceiveTimeout = 500;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] data = client.Receive(ref remoteEndPoint);
                    //NOTE: GetStringをメインスレッドでやるようにしたほうが負荷が下がるかもしれない(UDPの受信が超高速で回ってたら検討すべき)
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
                    _blendShapes.ContainsKey(item[0].TrimEnd()) &&
                    int.TryParse(item[1].TrimStart(), out int value)
                    )
                {
                    _blendShapes[item[0].TrimEnd()] = Mathf.Clamp01(value * 0.01f);
                }
            }
            
            //回転値が書いてある部分(note: そのうち位置も増えるかもね)
            var rotations = phrases[1].Split('|');
            for (int i = 0; i < rotations.Length; i++)
            {
                var item = rotations[i].Split('#');
                if (item.Length != 2 || !_rotationData.ContainsKey(item[0].TrimEnd()))
                {
                    continue;
                }

                var rotEuler = item[1].TrimStart().Split(',');
                if (rotEuler.Length == 3 && 
                    float.TryParse(rotEuler[0], out float x) && 
                    float.TryParse(rotEuler[1], out float y) && 
                    float.TryParse(rotEuler[2], out float z)
                    )
                {
                    _rotationData[item[0].TrimEnd()] = new Vector3(x, y, z);
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
            }
            
            //目
            void ApplyEyes()
            {
                _faceTrackSource.Eye.LeftBlink = _blendShapes[iFacialMocapBlendShapeNames.eyeBlinkLeft];
                _faceTrackSource.Eye.LeftSquint = _blendShapes[iFacialMocapBlendShapeNames.eyeSquintLeft];
                _faceTrackSource.Eye.LeftWide = _blendShapes[iFacialMocapBlendShapeNames.eyeWideLeft];
                _faceTrackSource.Eye.LeftLookIn = _blendShapes[iFacialMocapBlendShapeNames.eyeLookInLeft];
                _faceTrackSource.Eye.LeftLookOut =  _blendShapes[iFacialMocapBlendShapeNames.eyeLookOutLeft];
                _faceTrackSource.Eye.LeftLookUp =  _blendShapes[iFacialMocapBlendShapeNames.eyeLookUpLeft];
                _faceTrackSource.Eye.LeftLookDown =  _blendShapes[iFacialMocapBlendShapeNames.eyeLookDownLeft];

                _faceTrackSource.Eye.RightBlink = _blendShapes[iFacialMocapBlendShapeNames.eyeBlinkRight];
                _faceTrackSource.Eye.RightSquint = _blendShapes[iFacialMocapBlendShapeNames.eyeSquintRight];
                _faceTrackSource.Eye.RightWide = _blendShapes[iFacialMocapBlendShapeNames.eyeWideRight];
                _faceTrackSource.Eye.RightLookIn = _blendShapes[iFacialMocapBlendShapeNames.eyeLookInRight];
                _faceTrackSource.Eye.RightLookOut =  _blendShapes[iFacialMocapBlendShapeNames.eyeLookOutRight];
                _faceTrackSource.Eye.RightLookUp =  _blendShapes[iFacialMocapBlendShapeNames.eyeLookUpRight];
                _faceTrackSource.Eye.RightLookDown =  _blendShapes[iFacialMocapBlendShapeNames.eyeLookDownRight];
            }

            //口: 単純に数が多い！
            void ApplyMouth()
            {
                _faceTrackSource.Mouth.Left = _blendShapes[iFacialMocapBlendShapeNames.mouthLeft];
                _faceTrackSource.Mouth.LeftSmile = _blendShapes[iFacialMocapBlendShapeNames.mouthSmileLeft];
                _faceTrackSource.Mouth.LeftFrown = _blendShapes[iFacialMocapBlendShapeNames.mouthFrownLeft];
                _faceTrackSource.Mouth.LeftPress = _blendShapes[iFacialMocapBlendShapeNames.mouthPressLeft];
                _faceTrackSource.Mouth.LeftUpperUp = _blendShapes[iFacialMocapBlendShapeNames.mouthUpperUpLeft];
                _faceTrackSource.Mouth.LeftLowerDown = _blendShapes[iFacialMocapBlendShapeNames.mouthLowerDownLeft];
                _faceTrackSource.Mouth.LeftStretch = _blendShapes[iFacialMocapBlendShapeNames.mouthStretchLeft];
                _faceTrackSource.Mouth.LeftDimple = _blendShapes[iFacialMocapBlendShapeNames.mouthDimpleLeft];

                _faceTrackSource.Mouth.Right = _blendShapes[iFacialMocapBlendShapeNames.mouthRight];
                _faceTrackSource.Mouth.RightSmile = _blendShapes[iFacialMocapBlendShapeNames.mouthSmileRight];
                _faceTrackSource.Mouth.RightFrown = _blendShapes[iFacialMocapBlendShapeNames.mouthFrownRight];
                _faceTrackSource.Mouth.RightPress = _blendShapes[iFacialMocapBlendShapeNames.mouthPressRight];
                _faceTrackSource.Mouth.RightUpperUp = _blendShapes[iFacialMocapBlendShapeNames.mouthUpperUpRight];
                _faceTrackSource.Mouth.RightLowerDown = _blendShapes[iFacialMocapBlendShapeNames.mouthLowerDownRight];
                _faceTrackSource.Mouth.RightStretch = _blendShapes[iFacialMocapBlendShapeNames.mouthStretchRight];
                _faceTrackSource.Mouth.RightDimple = _blendShapes[iFacialMocapBlendShapeNames.mouthDimpleRight];

                _faceTrackSource.Mouth.Close = _blendShapes[iFacialMocapBlendShapeNames.mouthClose];
                _faceTrackSource.Mouth.Funnel = _blendShapes[iFacialMocapBlendShapeNames.mouthFunnel];
                _faceTrackSource.Mouth.Pucker = _blendShapes[iFacialMocapBlendShapeNames.mouthPucker];
                _faceTrackSource.Mouth.ShrugUpper = _blendShapes[iFacialMocapBlendShapeNames.mouthShrugUpper];
                _faceTrackSource.Mouth.ShrugLower = _blendShapes[iFacialMocapBlendShapeNames.mouthShrugLower];
                _faceTrackSource.Mouth.RollUpper = _blendShapes[iFacialMocapBlendShapeNames.mouthRollUpper];
                _faceTrackSource.Mouth.RollLower = _blendShapes[iFacialMocapBlendShapeNames.mouthRollLower];
            }

            //その他いろいろ
            void ApplyOtherBlendShapes()
            {
                _faceTrackSource.Brow.InnerUp = _blendShapes[iFacialMocapBlendShapeNames.browInnerUp];
                _faceTrackSource.Brow.LeftDown = _blendShapes[iFacialMocapBlendShapeNames.browDownLeft];
                _faceTrackSource.Brow.LeftOuterUp = _blendShapes[iFacialMocapBlendShapeNames.browOuterUpLeft];
                _faceTrackSource.Brow.RightDown = _blendShapes[iFacialMocapBlendShapeNames.browDownRight];
                _faceTrackSource.Brow.RightOuterUp = _blendShapes[iFacialMocapBlendShapeNames.browOuterUpRight];
            
                _faceTrackSource.Jaw.Open = _blendShapes[iFacialMocapBlendShapeNames.jawOpen];
                _faceTrackSource.Jaw.Forward = _blendShapes[iFacialMocapBlendShapeNames.jawForward];
                _faceTrackSource.Jaw.Left = _blendShapes[iFacialMocapBlendShapeNames.jawLeft];
                _faceTrackSource.Jaw.Right = _blendShapes[iFacialMocapBlendShapeNames.jawRight];
                
                _faceTrackSource.Nose.LeftSneer = _blendShapes[iFacialMocapBlendShapeNames.noseSneerLeft];
                _faceTrackSource.Nose.RightSneer = _blendShapes[iFacialMocapBlendShapeNames.noseSneerRight];

                _faceTrackSource.Cheek.Puff = _blendShapes[iFacialMocapBlendShapeNames.cheekPuff];
                _faceTrackSource.Cheek.LeftSquint = _blendShapes[iFacialMocapBlendShapeNames.cheekSquintLeft];
                _faceTrackSource.Cheek.RightSquint = _blendShapes[iFacialMocapBlendShapeNames.cheekSquintRight];
                
                _faceTrackSource.Tongue.TongueOut = _blendShapes[iFacialMocapBlendShapeNames.tongueOut];
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