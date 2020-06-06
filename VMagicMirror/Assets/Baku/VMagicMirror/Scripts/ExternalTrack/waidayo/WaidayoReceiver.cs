using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker.Waidayo
{
    /// <summary> Waidayoのデータ受信機構です。 </summary>
    /// <remarks>
    /// 整形方法が多少違うこと以外は特に考える事もないのでノーコメで…
    /// </remarks>
    public class WaidayoReceiver : ExternalTrackSourceProvider
    {
        //多重起動する設計になってないから決め打ちに罪悪感がないんですね～。
        private const int UDP_PORT = 50004;
        
        private readonly RecordFaceTrackSource _faceTrackSource = new RecordFaceTrackSource();
        public override IFaceTrackSource FaceTrackSource => _faceTrackSource;

        public override Quaternion HeadRotation => _offsetRotation * FaceTrackSource.FaceTransform.Rotation;

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

        public override void StartReceive()
        {
            StopReceive();
            new Thread(() => ThreadMethod(_cts.Token)).Start();
        }
        
        public override void StopReceive()
        {
            _cts?.Cancel();
            _cts = null;
            RawMessage = "";
        }

        private void Update()
        {
            //ちょっと間接的な確認方法だけど、CancellationTokenSourceがないのは読み取りが走ってない証拠
            if (_cts == null)
            {
                return;
            }
                
            string message = RawMessage;
            if (message != null)
            {
                RawMessage = "";
                DeserializeFaceMessage(message);
                RaiseFaceTrackUpdated();
            }
        }
        
        private void OnDestroy()
        {
            StopReceive();
        }

        private void DeserializeFaceMessage(string message)
        {
            //NOTE: 多分JSONをバラしててきとーにバラす…のか？あれOSCだっけ？
            //ああ面倒臭い…というか用途合致という意味で見てiFacialMocapのほうが枯れてるし筋がいい…
        }
        
        private void ThreadMethod(CancellationToken token)
        {
            var client = new UdpClient(UDP_PORT);
            client.Client.ReceiveTimeout = 500;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] data = client.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);
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
                var data = new WaidayoCalibrationData()
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
                    var data = JsonUtility.FromJson<WaidayoCalibrationData>(value);
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
    public class WaidayoCalibrationData
    {
        public float rotX;
        public float rotY;
        public float rotZ;
    }
    
}
