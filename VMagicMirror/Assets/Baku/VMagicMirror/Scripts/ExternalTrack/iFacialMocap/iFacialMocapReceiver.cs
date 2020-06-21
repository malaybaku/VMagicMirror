using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker.iFacialMocap
{
    /// <summary> iFacialMocapから顔トラッキングデータを受け取ってVMagicMirrorのデータ形状に整形するクラスです。 </summary>
    public class iFacialMocapReceiver : ExternalTrackSourceProvider
    {
        private const float CalibrateReflectDuration = 0.6f;
        private const int PortNumber = 49983;

        //テキストのGCAllocを避けるやつ
        private readonly StringBuilder _sb = new StringBuilder(2048);
        
        private readonly RecordFaceTrackSource _faceTrackSource = new RecordFaceTrackSource();
        public override IFaceTrackSource FaceTrackSource => _faceTrackSource;
        public override bool SupportHandTracking => false;
        public override bool SupportFacePositionOffset => false;
        public override Quaternion HeadRotation => SmoothOffsetRotation * _faceTrackSource.FaceTransform.Rotation;

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
            DeserializeMessageWithLessGcAlloc(message);
            //Lerpファクタが1ぴったりならLerp要らん(しかもその状況は発生頻度が高い)、みたいな話です。
            ApplyDeserializeResult(UpdateApplyRate < 0.999f);
            RaiseFaceTrackUpdated();
        }

        private void OnDestroy()
        {
            StopReceive();
        }

        public override void BreakToBasePosition(float breakRate)
        {
            _faceTrackSource.FaceTransform.Rotation = Quaternion.Slerp(
                _faceTrackSource.FaceTransform.Rotation,
                Quaternion.Inverse(SmoothOffsetRotation), 
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
                    float.TryParse(rotEuler[0], out float x) && 
                    float.TryParse(rotEuler[1], out float y) && 
                    float.TryParse(rotEuler[2], out float z)
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
            ParseRotations(_sb, equalIndex + 1, _sb.Length);
         
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

            void ParseRotations(StringBuilder src, int startIndex, int endIndex)
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
                    //"head # 1.00,-2.345,6.789"
                    string section = src.ToString(sectionStartIndex, i - sectionStartIndex);
                    sectionStartIndex = i + 1;

                    //区切り文字の位置をピックアップしていく
                    int hashIndex = -1;
                    int xCommaIndex = -1;
                    int yCommaIndex = -1;
                    string key = "";

                    for (int j = 0; j < section.Length; j++)
                    {
                        var c = section[j];
                        if (c == '#')
                        {
                            hashIndex = j;
                            key = section.Substring(0, hashIndex);
                            if (key != "head")
                            {
                                key = "";
                                //頭のデータではない: このforの後のif文で弾いて無視
                                break;
                            }
                        }

                        if (c == ',')
                        {
                            if (xCommaIndex == -1)
                            {
                                xCommaIndex = j;
                            }
                            else
                            {
                                yCommaIndex = j;
                            }
                        }
                    }

                    //キーが欲しいのと違う
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }
                    
                    //データが不正だったケース
                    if (hashIndex < 0 || xCommaIndex < 0 || yCommaIndex < 0)
                    {
                        continue;
                    }

                    //後半の数値のとこを取得
                    if (float.TryParse(
                            section.Substring(hashIndex + 1, xCommaIndex - hashIndex - 1),
                            out var x) &&
                        float.TryParse(
                            section.Substring(xCommaIndex + 1, yCommaIndex - xCommaIndex - 1),
                            out var y) &&
                        float.TryParse(
                            section.Substring(yCommaIndex + 1),
                            out var z)
                    )
                    {
                        _rotationData[key] = new Vector3(x, y, z);
                    }
                }
            }
        }

        private void ApplyDeserializeResult()
        {
            _faceTrackSource.FaceTransform.Rotation =
                Quaternion.Euler(_rotationData[iFacialMocapRotationNames.head]);
            
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
        
        #endregion
        
        #region キャリブレーション

        public override void Calibrate()
        {
            //NOTE: iFacialMocapはロールとピッチが補正済みの姿勢を送ってくる。
            //デバイス座標的な解釈としては、iOS機器がつねに垂直に立っているものと捉えたうえで
            //デバイスの方向をキャリブしてるんですよ、と考えるとよい
            YawOffset = -_rotationData[iFacialMocapRotationNames.head].y;
        }


        //NOTE: キャリブ情報の実態 = トラッカーデバイスがUnity空間にあったとみなした場合のワールド回転をオイラー角表現したもの。
        //こういう値だとUnity空間上にモノを置いて図示する余地があり、筋がいい
        private float _yawOffset = 0;
        private float YawOffset
        {
            get => _yawOffset;
            set
            {
                if (Mathf.Abs(_yawOffset - value) < Mathf.Epsilon)
                {
                    return;
                }
                
                _yawOffset = value;
                _yawOffsetTween?.Kill();
                _yawOffsetTween = DOTween.To(
                    () => _smoothYawOffset,
                    v => _smoothYawOffset = v,
                    _yawOffset,
                    CalibrateReflectDuration
                )
                    .SetEase(Ease.OutCubic);
            }
        }

        //NOTE: この値はふだんYawOffsetに一致するが、
        //キャリブレーション時などに値が急に飛ばないようTweenつきでアップデートされる
        private float _smoothYawOffset = 0;
        private Quaternion SmoothOffsetRotation => Quaternion.AngleAxis(_smoothYawOffset, Vector3.up);
        //NOTE: 連打とかでTweenが多重に走るのを防止するやつです
        private TweenerCore<float, float, FloatOptions> _yawOffsetTween = null;
        
        public override string CalibrationData
        {
            get
            {
                var data = new IFacialMocapCalibrationData()
                {
                    yawOffset = YawOffset,
                };
                return JsonUtility.ToJson(data);
            }
            set
            {
                try
                {
                    var data = JsonUtility.FromJson<IFacialMocapCalibrationData>(value);
                    YawOffset = data.yawOffset;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    LogOutput.Instance.Write(ex);
                }
            }
        }

        #endregion
    }

    [Serializable]
    public class IFacialMocapCalibrationData
    {
        public float yawOffset;
    }
}