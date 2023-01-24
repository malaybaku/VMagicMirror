using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Baku.VMagicMirror.ExternalTracker.iFacialMocap
{
    /// <summary> iFacialMocapから顔トラッキングデータを受け取ってVMagicMirrorのデータ形状に整形するクラスです。 </summary>
    public class iFacialMocapReceiver : ExternalTrackSourceProvider
    {
        private const float CalibrateReflectDuration = 0.6f;
        private const int PortNumber = 49983;

        private const int SleepMsDefault = 12;
        private const int SleepMsLowerLimit = 4;
        //この回数だけUDP受信すると、そのたびに受信FPSを計算する
        private const int ReceiveFpsCountInterval = 500;
        
        //テキストのGCAllocを避けるやつ
        private readonly StringBuilder _sb = new StringBuilder(2048);
        
        private readonly RecordFaceTrackSource _faceTrackSource = new RecordFaceTrackSource();
        public override IFaceTrackSource FaceTrackSource => _faceTrackSource;
        public override bool SupportHandTracking => false;
        public override bool SupportFacePositionOffset => true;
        public override Quaternion HeadRotation 
            => _smoothOffsetRotation * _faceTrackSource.FaceTransform.Rotation;

        public override Vector3 HeadPositionOffset
        {
            get
            {
                if (_hasReceiveRawPosition)
                {
                    return 
                        _smoothOffsetRotation * 
                        (_smoothOffsetPosition + _faceTrackSource.FaceTransform.Position);
                }
                else
                {
                    return Vector3.zero;
                }
            }
        }

        private CancellationTokenSource _cts = null;
        //NOTE: 前回とまったく同じ文字列が来たときに「こいつさてはトラッキングロスしたな？」と推測するために使う
        private string _prevMessage = "";
        
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

        /// <summary>
        /// データの受信スレッドが動いているかどうかを取得します。
        /// 実データが飛んで来ないような受信待ちの状態でもtrueになることに注意してください。
        /// </summary>
        public bool IsRunning { get; private set; } = false;
        
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

        private bool _hasReceiveRawPosition = false;
        private Vector3 _rawPosition = Vector3.zero;
        
        private void Update()
        {
            //明確にStartしてからStopするまでの途中でのみデシリアライズを行うようガード
            if (_cts == null)
            {
                return;
            }

            string message = RawMessage;
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            
            RawMessage = "";

            if (_prevMessage == message)
            {
                //新規データかと思ったら前と同じだったケース。
                //この場合は恐らくトラッキングロスしてるため、本来の意味で接続したとは見なさない。
                return;
            }
            
            DeserializeMessageWithLessGcAlloc(message);
            //Lerpファクタが1ぴったりならLerp要らん(しかもその状況は発生頻度が高い)、みたいな話です。
            ApplyDeserializeResult(UpdateApplyRate < 0.999f);
            RaiseFaceTrackUpdated();
            _prevMessage = message;
        }

        private void OnDestroy()
        {
            StopReceive();
        }

        public override void BreakToBasePosition(float breakRate)
        {
            _faceTrackSource.FaceTransform.Rotation = Quaternion.Slerp(
                _faceTrackSource.FaceTransform.Rotation,
                Quaternion.Inverse(_smoothOffsetRotation), 
                1 - breakRate
            );
            
            //目
            {
                _faceTrackSource.Eye.LeftBlink *= breakRate;
                _faceTrackSource.Eye.LeftSquint *= breakRate;
                _faceTrackSource.Eye.LeftWide *= breakRate;
                _faceTrackSource.Eye.LeftLookIn *= breakRate;
                _faceTrackSource.Eye.LeftLookOut *= breakRate;
                _faceTrackSource.Eye.LeftLookUp *= breakRate;
                _faceTrackSource.Eye.LeftLookDown *= breakRate;

                _faceTrackSource.Eye.RightBlink *= breakRate;
                _faceTrackSource.Eye.RightSquint *= breakRate;
                _faceTrackSource.Eye.RightWide *= breakRate;
                _faceTrackSource.Eye.RightLookIn *= breakRate;
                _faceTrackSource.Eye.RightLookOut *= breakRate;
                _faceTrackSource.Eye.RightLookUp *= breakRate;
                _faceTrackSource.Eye.RightLookDown *= breakRate;
            }

            //口
            {
                _faceTrackSource.Mouth.Left *= breakRate;
                _faceTrackSource.Mouth.LeftSmile *= breakRate;
                _faceTrackSource.Mouth.LeftFrown *= breakRate;
                _faceTrackSource.Mouth.LeftPress *= breakRate;
                _faceTrackSource.Mouth.LeftUpperUp *= breakRate;
                _faceTrackSource.Mouth.LeftLowerDown *= breakRate;
                _faceTrackSource.Mouth.LeftStretch *= breakRate;
                _faceTrackSource.Mouth.LeftDimple *= breakRate;

                _faceTrackSource.Mouth.Right *= breakRate;
                _faceTrackSource.Mouth.RightSmile *= breakRate;
                _faceTrackSource.Mouth.RightFrown *= breakRate;
                _faceTrackSource.Mouth.RightPress *= breakRate;
                _faceTrackSource.Mouth.RightUpperUp *= breakRate;
                _faceTrackSource.Mouth.RightLowerDown *= breakRate;
                _faceTrackSource.Mouth.RightStretch *= breakRate;
                _faceTrackSource.Mouth.RightDimple *= breakRate;

                _faceTrackSource.Mouth.Close *= breakRate;
                _faceTrackSource.Mouth.Funnel *= breakRate;
                _faceTrackSource.Mouth.Pucker *= breakRate;
                _faceTrackSource.Mouth.ShrugUpper *= breakRate;
                _faceTrackSource.Mouth.ShrugLower *= breakRate;
                _faceTrackSource.Mouth.RollUpper *= breakRate;
                _faceTrackSource.Mouth.RollLower *= breakRate;
            }

            //その他いろいろ
            {
                _faceTrackSource.Brow.InnerUp *= breakRate;
                _faceTrackSource.Brow.LeftDown *= breakRate;
                _faceTrackSource.Brow.LeftOuterUp *= breakRate;
                _faceTrackSource.Brow.RightDown *= breakRate;
                _faceTrackSource.Brow.RightOuterUp *= breakRate;
            
                _faceTrackSource.Jaw.Open *= breakRate;
                _faceTrackSource.Jaw.Forward *= breakRate;
                _faceTrackSource.Jaw.Left *= breakRate;
                _faceTrackSource.Jaw.Right *= breakRate;
                
                _faceTrackSource.Nose.LeftSneer *= breakRate;
                _faceTrackSource.Nose.RightSneer *= breakRate;

                _faceTrackSource.Cheek.Puff *= breakRate;
                _faceTrackSource.Cheek.LeftSquint *= breakRate;
                _faceTrackSource.Cheek.RightSquint *= breakRate;
                
                _faceTrackSource.Tongue.TongueOut *= breakRate;
            }
        }

        #region 受信ルーチンまわり
        
        public override void StartReceive()
        {
            LogOutput.Instance.Write("Start iFacialMocap receiver");
            StopReceive();
            _cts = new CancellationTokenSource();
            new Thread(() => ThreadMethod(_cts.Token)).Start();
            IsRunning = true;
        }
        
        public override void StopReceive()
        {
            IsRunning = false;
            _cts?.Cancel();
            _cts = null;
            RawMessage = "";
            _prevMessage = "";
        }

        private void ThreadMethod(CancellationToken token)
        {
            var clientV4 = new UdpClient(PortNumber, AddressFamily.InterNetwork);
            clientV4.Client.ReceiveTimeout = 500;
            var clientV6 = new UdpClient(PortNumber, AddressFamily.InterNetworkV6);
            clientV6.Client.ReceiveTimeout = 500;

            UdpClient client = null;
            
            //最初のメッセージがIPv4またはIPv6から飛んでくるのを待機
            //並列実行に出来る部分だが、そんなに速度要求される部分でもないので直列でやってしまう
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] data = clientV4.Receive(ref remoteEndPoint);
                    //NOTE: GetStringをメインスレッドでやるようにしたほうが負荷が下がるかもしれない(UDPの受信が超高速で回ってたら検討すべき)
                    string message = Encoding.ASCII.GetString(data);
                    RawMessage = message;
                    client = clientV4;
                    break;
                }
                catch (Exception)
                {
                    //ここは通信待ち状態とかで頻繁に来る(SocketExceptionが出る)ので、ログを出さない。以降も同様
                    //LogOutput.Instance.Write(ex);
                }

                try
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] data = clientV6.Receive(ref remoteEndPoint);
                    string message = Encoding.ASCII.GetString(data);
                    RawMessage = message;
                    client = clientV6;
                    break;
                }
                catch (Exception)
                {
                    //Do nothing
                }
            }

            if (client != null)
            {
                var log = $"Connected with family {(client == clientV4 ? "IPv4" : "IPv6")}";
                Debug.Log(log);
                LogOutput.Instance.Write(log);
            }

            // Receiveのサボり方
            // 前提:
            // - iFacialMocapは60FPSでデータを送信してくる
            // - VMagicMirrorの受信処理は極めて短時間で済むよう設計されてる
            // やっていること:
            // - 投機的に、受信のたびにThread.Sleepで休む
            // - データの受信FPSが低い場合、Sleepが長すぎる可能性があるので徐々にSleepの長さを減らしていく

            var sleepMsReachedLowerLimit = false;
            var sleepMs = SleepMsDefault;
            var receiveCount = 0;

            var sw = new Stopwatch();
            sw.Start();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] data = client.Receive(ref remoteEndPoint);
                    string message = Encoding.ASCII.GetString(data);
                    RawMessage = message;
                    if (!sleepMsReachedLowerLimit)
                    {
                        receiveCount++;
                    }
                }
                catch (Exception)
                {
                    //iFacialMocap側が完全に止まった可能性が高いので、FPSの測定をリセット
                    receiveCount = 0;
                    sw.Restart();
                    //Do nothing
                }

                if (receiveCount >= ReceiveFpsCountInterval)
                {
                    receiveCount = 0;
                    var elapsed = sw.ElapsedMilliseconds;

                    var fps = ReceiveFpsCountInterval * 1000 / elapsed ;
                    //FPSが低い、という判定はそこそこ厳しく取る。互換性の観点で言うと基本SleepMsが短い方がよい、というのを踏まえてる
                    if (fps < 50)
                    {
                        sleepMs--;
                        sleepMsReachedLowerLimit = sleepMs <= SleepMsLowerLimit;
                        LogOutput.Instance.Write($"iFacialMocap receive fps({fps}) is low, set sleep time to {sleepMs}ms");
                    }
                    sw.Restart();
                }
                
                Thread.Sleep(sleepMs);
            }

            sw.Stop();
            try
            {
                clientV4?.Close();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }

            try
            {
                clientV6?.Close();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        #endregion
        
        #region デシリアライズまわり
        
        //NOTE: この処理が何気にGCAlloc多いので書き直した関数を使ってます(直した後の処理もちょっと重いけど)
        private void DeserializeFaceMessage(string msg)
        {
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
                    ParseUtil.FloatParse(rotEuler[0], out float x) && 
                    ParseUtil.FloatParse(rotEuler[1], out float y) && 
                    ParseUtil.FloatParse(rotEuler[2], out float z)
                    )
                {
                    _rotationData[item[0].TrimEnd()] = new Vector3(x, y, z);
                }
            }

        }

        //string.Split禁止バージョン
        private void DeserializeMessageWithLessGcAlloc(string msg)
        {
            _sb.Clear();
            //サニタイズとしてスペース文字を全部消しながら詰める
            for (int i = 0; i < msg.Length; i++)
            {
                if (msg[i] != ' ')
                {
                    _sb.Append(msg[i]);
                }
            }

            //"="の位置を調べつつvalidate
            int equalIndex = FindEqualCharIndex(_sb);
            if (equalIndex == -1 || equalIndex == _sb.Length - 1)
            {
                return;
            }
            
            //validateされたらブレンドシェイプと頭部回転をチェック
            ParseBlendShapes(_sb, 0, equalIndex);
            ParseHeadData(_sb, equalIndex + 1, _sb.Length);
         
            //NOTE: これだけpureな処理
            int FindEqualCharIndex(StringBuilder src)
            {
                int findCount = 0;
                int result = -1;
                for (int i = 0; i < src.Length; i++)
                {
                    if (src[i] == '=')
                    {
                        if (findCount == 0)
                        {
                            findCount = 1;
                            result = i;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }
                return result;
            }
            
            void ParseBlendShapes(StringBuilder src,int startIndex, int endIndex)
            {
                int sectionStartIndex = startIndex;
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (src[i] != '|')
                    {
                        continue;
                    }

                    //0 ~ 100のブレンドシェイプが載っている部分を引っ張り出す。
                    //Substringの時点でこんな感じのハズ(スペースは入ってたり入ってなかったり):
                    //"L_eyeSquint-42"
                    string section = src.ToString(sectionStartIndex, i - sectionStartIndex);
                    sectionStartIndex = i + 1;

                    for (int j = 0; j < section.Length; j++)
                    {
                        if (section[j] != '-')
                        {
                            continue;
                        }

                        string key = section.Substring(0, j);
                        if (int.TryParse(section.Substring(j + 1), out int blendShapeValue))
                        {
                            _blendShapes[key] = blendShapeValue * 0.01f;
                        }
                    }
                }
            }

            void ParseHeadData(StringBuilder src, int startIndex, int endIndex)
            {  
                int sectionStartIndex = startIndex;
                for (int i = startIndex; i < endIndex; i++)
                {
                    //NOTE: 末尾要素の後にもちゃんと"|"が入ってます
                    if (src[i] != '|')
                    {
                        continue;
                    }

                    //個別のデータはこんな感じ
                    //"head#1.00,-2.345,6.789"
                    string section = src.ToString(sectionStartIndex, i - sectionStartIndex);
                    sectionStartIndex = i + 1;
                    //IMPORTANT: iFacialMocap 1.0.6からrot + positionが飛んでくるようになった。
                    //この差分を真剣に捌くのはすげー辛いので、適当にやります。
                    if (!section.StartsWith("head#"))
                    {
                        continue;
                    }

                    var items = section.Substring(5).Split(',');
                    if (items.Length > 5 && 
                        ParseUtil.FloatParse(items[0], out var rx) &&
                        ParseUtil.FloatParse(items[1], out var ry) &&
                        ParseUtil.FloatParse(items[2], out var rz) &&
                        ParseUtil.FloatParse(items[3], out var px) &&
                        ParseUtil.FloatParse(items[4], out var py) &&
                        ParseUtil.FloatParse(items[5], out var pz))
                    {
                        _rotationData[iFacialMocapRotationNames.head] = new Vector3(rx, ry, rz);
                        _hasReceiveRawPosition = true;
                        _rawPosition = new Vector3(px, py, pz);
                    }
                    else if (
                        items.Length > 2 &&
                        ParseUtil.FloatParse(items[0], out var x) &&
                        ParseUtil.FloatParse(items[1], out var y) &&
                        ParseUtil.FloatParse(items[2], out var z))
                    {
                        _rotationData[iFacialMocapRotationNames.head] = new Vector3(x, y, z);
                        _hasReceiveRawPosition = false;
                        _rawPosition = Vector3.zero;
                    }
                    else
                    {
                        _hasReceiveRawPosition = false;
                        _rawPosition = Vector3.zero;
                    }
                }
            }
        }

        private void ApplyDeserializeResult()
        {
            _faceTrackSource.FaceTransform.Rotation =
                Quaternion.Euler(_rotationData[iFacialMocapRotationNames.head]);
            _faceTrackSource.FaceTransform.HasValidPosition = _hasReceiveRawPosition;
            _faceTrackSource.FaceTransform.Position = _rawPosition;
            
            //目
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
                LimitSquint();
            }

            //口: 単純に数が多い！
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

        //NOTE: Lerp版の処理がわざわざ別関数になっているのは、Lerpするのが特殊なシチュエーションだけだからです
        private void ApplyDeserializeResult(bool considerApplyRate)
        {
            if (!considerApplyRate)
            {
                ApplyDeserializeResult();
                return;
            }
            
            //NOTE: 引数なし版と違い、地獄のようなLerpが続きます
            
            _faceTrackSource.FaceTransform.Rotation = Quaternion.Slerp(
                _faceTrackSource.FaceTransform.Rotation,
                Quaternion.Euler(_rotationData[iFacialMocapRotationNames.head]),
                UpdateApplyRate
                );

            _faceTrackSource.FaceTransform.HasValidPosition = _hasReceiveRawPosition;
            //NOTE: 有効か無効かによらずLerpしとく
            _faceTrackSource.FaceTransform.Position = Vector3.Lerp(
                _faceTrackSource.FaceTransform.Position,
                _rawPosition,
                UpdateApplyRate
                );

            //目
            {
                _faceTrackSource.Eye.LeftBlink = Mathf.Lerp(_faceTrackSource.Eye.LeftBlink,_blendShapes[iFacialMocapBlendShapeNames.eyeBlinkLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftSquint = Mathf.Lerp(_faceTrackSource.Eye.LeftSquint,_blendShapes[iFacialMocapBlendShapeNames.eyeSquintLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftWide = Mathf.Lerp(_faceTrackSource.Eye.LeftWide,_blendShapes[iFacialMocapBlendShapeNames.eyeWideLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftLookIn = Mathf.Lerp(_faceTrackSource.Eye.LeftLookIn,_blendShapes[iFacialMocapBlendShapeNames.eyeLookInLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftLookOut = Mathf.Lerp(_faceTrackSource.Eye.LeftLookOut, _blendShapes[iFacialMocapBlendShapeNames.eyeLookOutLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftLookUp = Mathf.Lerp(_faceTrackSource.Eye.LeftLookUp, _blendShapes[iFacialMocapBlendShapeNames.eyeLookUpLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftLookDown = Mathf.Lerp(_faceTrackSource.Eye.LeftLookDown, _blendShapes[iFacialMocapBlendShapeNames.eyeLookDownLeft], UpdateApplyRate);

                _faceTrackSource.Eye.RightBlink = Mathf.Lerp(_faceTrackSource.Eye.RightBlink,_blendShapes[iFacialMocapBlendShapeNames.eyeBlinkRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightSquint = Mathf.Lerp(_faceTrackSource.Eye.RightSquint,_blendShapes[iFacialMocapBlendShapeNames.eyeSquintRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightWide = Mathf.Lerp(_faceTrackSource.Eye.RightWide,_blendShapes[iFacialMocapBlendShapeNames.eyeWideRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightLookIn = Mathf.Lerp(_faceTrackSource.Eye.RightLookIn,_blendShapes[iFacialMocapBlendShapeNames.eyeLookInRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightLookOut = Mathf.Lerp(_faceTrackSource.Eye.RightLookOut, _blendShapes[iFacialMocapBlendShapeNames.eyeLookOutRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightLookUp = Mathf.Lerp(_faceTrackSource.Eye.RightLookUp, _blendShapes[iFacialMocapBlendShapeNames.eyeLookUpRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightLookDown = Mathf.Lerp(_faceTrackSource.Eye.RightLookDown, _blendShapes[iFacialMocapBlendShapeNames.eyeLookDownRight], UpdateApplyRate);
                LimitSquint();
            }

            //口: 単純に数が多い！
            {
                _faceTrackSource.Mouth.Left = Mathf.Lerp(_faceTrackSource.Mouth.Left,_blendShapes[iFacialMocapBlendShapeNames.mouthLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftSmile = Mathf.Lerp(_faceTrackSource.Mouth.LeftSmile,_blendShapes[iFacialMocapBlendShapeNames.mouthSmileLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftFrown = Mathf.Lerp(_faceTrackSource.Mouth.LeftFrown,_blendShapes[iFacialMocapBlendShapeNames.mouthFrownLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftPress = Mathf.Lerp(_faceTrackSource.Mouth.LeftPress,_blendShapes[iFacialMocapBlendShapeNames.mouthPressLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftUpperUp = Mathf.Lerp(_faceTrackSource.Mouth.LeftUpperUp,_blendShapes[iFacialMocapBlendShapeNames.mouthUpperUpLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftLowerDown = Mathf.Lerp(_faceTrackSource.Mouth.LeftLowerDown,_blendShapes[iFacialMocapBlendShapeNames.mouthLowerDownLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftStretch = Mathf.Lerp(_faceTrackSource.Mouth.LeftStretch,_blendShapes[iFacialMocapBlendShapeNames.mouthStretchLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftDimple = Mathf.Lerp(_faceTrackSource.Mouth.LeftDimple,_blendShapes[iFacialMocapBlendShapeNames.mouthDimpleLeft], UpdateApplyRate);

                _faceTrackSource.Mouth.Right = Mathf.Lerp(_faceTrackSource.Mouth.Right,_blendShapes[iFacialMocapBlendShapeNames.mouthRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightSmile = Mathf.Lerp(_faceTrackSource.Mouth.RightSmile,_blendShapes[iFacialMocapBlendShapeNames.mouthSmileRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightFrown = Mathf.Lerp(_faceTrackSource.Mouth.RightFrown,_blendShapes[iFacialMocapBlendShapeNames.mouthFrownRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightPress = Mathf.Lerp(_faceTrackSource.Mouth.RightPress,_blendShapes[iFacialMocapBlendShapeNames.mouthPressRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightUpperUp = Mathf.Lerp(_faceTrackSource.Mouth.RightUpperUp,_blendShapes[iFacialMocapBlendShapeNames.mouthUpperUpRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightLowerDown = Mathf.Lerp(_faceTrackSource.Mouth.RightLowerDown,_blendShapes[iFacialMocapBlendShapeNames.mouthLowerDownRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightStretch = Mathf.Lerp(_faceTrackSource.Mouth.RightStretch,_blendShapes[iFacialMocapBlendShapeNames.mouthStretchRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightDimple = Mathf.Lerp(_faceTrackSource.Mouth.RightDimple,_blendShapes[iFacialMocapBlendShapeNames.mouthDimpleRight], UpdateApplyRate);

                _faceTrackSource.Mouth.Close = Mathf.Lerp(_faceTrackSource.Mouth.Close,_blendShapes[iFacialMocapBlendShapeNames.mouthClose], UpdateApplyRate);
                _faceTrackSource.Mouth.Funnel = Mathf.Lerp(_faceTrackSource.Mouth.Funnel,_blendShapes[iFacialMocapBlendShapeNames.mouthFunnel], UpdateApplyRate);
                _faceTrackSource.Mouth.Pucker = Mathf.Lerp(_faceTrackSource.Mouth.Pucker,_blendShapes[iFacialMocapBlendShapeNames.mouthPucker], UpdateApplyRate);
                _faceTrackSource.Mouth.ShrugUpper = Mathf.Lerp(_faceTrackSource.Mouth.ShrugUpper,_blendShapes[iFacialMocapBlendShapeNames.mouthShrugUpper], UpdateApplyRate);
                _faceTrackSource.Mouth.ShrugLower = Mathf.Lerp(_faceTrackSource.Mouth.ShrugLower,_blendShapes[iFacialMocapBlendShapeNames.mouthShrugLower], UpdateApplyRate);
                _faceTrackSource.Mouth.RollUpper = Mathf.Lerp(_faceTrackSource.Mouth.RollUpper,_blendShapes[iFacialMocapBlendShapeNames.mouthRollUpper], UpdateApplyRate);
                _faceTrackSource.Mouth.RollLower = Mathf.Lerp(_faceTrackSource.Mouth.RollLower,_blendShapes[iFacialMocapBlendShapeNames.mouthRollLower], UpdateApplyRate);
            }

            //その他いろいろ
            {
                _faceTrackSource.Brow.InnerUp = Mathf.Lerp(_faceTrackSource.Brow.InnerUp,_blendShapes[iFacialMocapBlendShapeNames.browInnerUp], UpdateApplyRate);
                _faceTrackSource.Brow.LeftDown = Mathf.Lerp(_faceTrackSource.Brow.LeftDown,_blendShapes[iFacialMocapBlendShapeNames.browDownLeft], UpdateApplyRate);
                _faceTrackSource.Brow.LeftOuterUp = Mathf.Lerp(_faceTrackSource.Brow.LeftOuterUp,_blendShapes[iFacialMocapBlendShapeNames.browOuterUpLeft], UpdateApplyRate);
                _faceTrackSource.Brow.RightDown = Mathf.Lerp(_faceTrackSource.Brow.RightDown,_blendShapes[iFacialMocapBlendShapeNames.browDownRight], UpdateApplyRate);
                _faceTrackSource.Brow.RightOuterUp = Mathf.Lerp(_faceTrackSource.Brow.RightOuterUp,_blendShapes[iFacialMocapBlendShapeNames.browOuterUpRight], UpdateApplyRate);
            
                _faceTrackSource.Jaw.Open = Mathf.Lerp(_faceTrackSource.Jaw.Open,_blendShapes[iFacialMocapBlendShapeNames.jawOpen], UpdateApplyRate);
                _faceTrackSource.Jaw.Forward = Mathf.Lerp(_faceTrackSource.Jaw.Forward,_blendShapes[iFacialMocapBlendShapeNames.jawForward], UpdateApplyRate);
                _faceTrackSource.Jaw.Left = Mathf.Lerp(_faceTrackSource.Jaw.Left,_blendShapes[iFacialMocapBlendShapeNames.jawLeft], UpdateApplyRate);
                _faceTrackSource.Jaw.Right = Mathf.Lerp(_faceTrackSource.Jaw.Right,_blendShapes[iFacialMocapBlendShapeNames.jawRight], UpdateApplyRate);
                
                _faceTrackSource.Nose.LeftSneer = Mathf.Lerp(_faceTrackSource.Nose.LeftSneer,_blendShapes[iFacialMocapBlendShapeNames.noseSneerLeft], UpdateApplyRate);
                _faceTrackSource.Nose.RightSneer = Mathf.Lerp(_faceTrackSource.Nose.RightSneer,_blendShapes[iFacialMocapBlendShapeNames.noseSneerRight], UpdateApplyRate);

                _faceTrackSource.Cheek.Puff = Mathf.Lerp(_faceTrackSource.Cheek.Puff,_blendShapes[iFacialMocapBlendShapeNames.cheekPuff], UpdateApplyRate);
                _faceTrackSource.Cheek.LeftSquint = Mathf.Lerp(_faceTrackSource.Cheek.LeftSquint,_blendShapes[iFacialMocapBlendShapeNames.cheekSquintLeft], UpdateApplyRate);
                _faceTrackSource.Cheek.RightSquint = Mathf.Lerp(_faceTrackSource.Cheek.RightSquint,_blendShapes[iFacialMocapBlendShapeNames.cheekSquintRight], UpdateApplyRate);
                
                _faceTrackSource.Tongue.TongueOut = Mathf.Lerp(_faceTrackSource.Tongue.TongueOut,_blendShapes[iFacialMocapBlendShapeNames.tongueOut], UpdateApplyRate);
            }
        }

        //左右の目について、BlinkとSquintの合計値が1を超えるのを禁止します。
        private void LimitSquint()
        {
            _faceTrackSource.Eye.LeftSquint =
                Mathf.Min(_faceTrackSource.Eye.LeftSquint, 1f - _faceTrackSource.Eye.LeftBlink);
            _faceTrackSource.Eye.RightSquint =
                Mathf.Min(_faceTrackSource.Eye.RightSquint, 1f - _faceTrackSource.Eye.RightBlink);
        }
        
        #endregion
        
        #region キャリブレーション

        public override void Calibrate()
        {
            //NOTE: iFacialMocapは端末座標系でpos/rotを送ってくる。ので、記録すべきデータはそのpos/rotそのもの。
            RotOffset = Quaternion.Inverse(Quaternion.Euler(_rotationData[iFacialMocapRotationNames.head]));
            PosOffset = -_rawPosition;
        }
      
        public override string CalibrationData
        {
            get
            {
                var rEuler = _rotOffset.eulerAngles;
                var data = new IFacialMocapCalibrationData()
                {
                    rx = rEuler.x,
                    ry = rEuler.y,
                    rz = rEuler.z,
                    px = _posOffset.x,
                    py = _posOffset.y,
                    pz = _posOffset.z,
                };
                return JsonUtility.ToJson(data);
            }
            set
            {
                try
                {
                    var data = JsonUtility.FromJson<IFacialMocapCalibrationData>(value);
                    RotOffset = Quaternion.Euler(data.rx, data.ry, data.rz);
                    PosOffset = new Vector3(data.px, data.py, data.pz);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    LogOutput.Instance.Write(ex);
                }
            }
        }
        
        //Updateとかで使っていいキャリブオフセット: キャリブ前後で値が吹っ飛ばないよう対策されている。
        private Vector3 _smoothOffsetPosition;
        private Quaternion _smoothOffsetRotation = Quaternion.identity;
        
        
        //生のキャリブ値
        private Vector3 _posOffset = new Vector3(0, 0, 0.5f);
        private Quaternion _rotOffset = Quaternion.identity;
        
        //キャリブデータの読み書きをここに入れるとスムージングが走る
        private Vector3 PosOffset
        {
            get => _posOffset;
            set
            {
                _posOffset = value;
                _posOffsetTween?.Kill();
                _posOffsetTween = DOTween.To(
                        () => _smoothOffsetPosition,
                        v => _smoothOffsetPosition = v,
                        _posOffset,
                        CalibrateReflectDuration
                    )
                    .SetEase(Ease.OutCubic);
            }
        }
        private Quaternion RotOffset
        {
            get => _rotOffset;
            set
            {
                _rotOffset = value;
                _rotOffsetTween?.Kill();
                _rotOffsetTween = DOTween.To(
                        () => _smoothOffsetRotation,
                        v => _smoothOffsetRotation = v,
                        _rotOffset.eulerAngles,
                        CalibrateReflectDuration
                    )
                    .SetEase(Ease.OutCubic);
            }
        }        
        
        //NOTE: 多重Tweenを防止するためにTweenerを保持する
        private TweenerCore<Vector3, Vector3, VectorOptions> _posOffsetTween = null;
        private TweenerCore<Quaternion, Vector3, QuaternionOptions> _rotOffsetTween = null;

        #endregion
    }

    [Serializable]
    public class IFacialMocapCalibrationData
    {
        public float rx;
        public float ry;
        public float rz;
        public float px;
        public float py;
        public float pz;
    }
}