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

using WebSocketSharp;
using WebSocketSharp.Server;
using Baku.VMagicMirror.ExternalTracker.iFacialMocap;

namespace Baku.VMagicMirror.ExternalTracker.Shiori
{
    /// <summary> Shioriから顔トラッキングデータを受け取ってVMagicMirrorのデータ形状に整形するクラスです。 </summary>
    public class ShioriReceiver : ExternalTrackSourceProvider
    {
        private const float CalibrateReflectDuration = 0.6f;
        private const int PortNumber = 23456;

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

        //NOTE: 前回とまったく同じ文字列が来たときに「こいつさてはトラッキングロスしたな？」と推測するために使う
        private string _prevMessage = "";

        private readonly object _rawMessageLock = new object();
        private string _rawMessage = "";
        public string RawMessage
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
            [ShioriBlendShapeNames.eyeBlinkLeft] = 0.0f,
            [ShioriBlendShapeNames.eyeLookUpLeft] = 0.0f,
            [ShioriBlendShapeNames.eyeLookDownLeft] = 0.0f,
            [ShioriBlendShapeNames.eyeLookInLeft] = 0.0f,
            [ShioriBlendShapeNames.eyeLookOutLeft] = 0.0f,
            [ShioriBlendShapeNames.eyeWideLeft] = 0.0f,
            [ShioriBlendShapeNames.eyeSquintLeft] = 0.0f,

            [ShioriBlendShapeNames.eyeBlinkRight] = 0.0f,
            [ShioriBlendShapeNames.eyeLookUpRight] = 0.0f,
            [ShioriBlendShapeNames.eyeLookDownRight] = 0.0f,
            [ShioriBlendShapeNames.eyeLookInRight] = 0.0f,
            [ShioriBlendShapeNames.eyeLookOutRight] = 0.0f,
            [ShioriBlendShapeNames.eyeWideRight] = 0.0f,
            [ShioriBlendShapeNames.eyeSquintRight] = 0.0f,

            //あご
            [ShioriBlendShapeNames.jawOpen] = 0.0f,
            [ShioriBlendShapeNames.jawForward] = 0.0f,
            [ShioriBlendShapeNames.jawLeft] = 0.0f,
            [ShioriBlendShapeNames.jawRight] = 0.0f,

            //まゆげ
            [ShioriBlendShapeNames.browDownLeft] = 0.0f,
            [ShioriBlendShapeNames.browOuterUpLeft] = 0.0f,
            [ShioriBlendShapeNames.browDownRight] = 0.0f,
            [ShioriBlendShapeNames.browOuterUpRight] = 0.0f,
            [ShioriBlendShapeNames.browInnerUp] = 0.0f,

            //口(多い)
            [ShioriBlendShapeNames.mouthLeft] = 0.0f,
            [ShioriBlendShapeNames.mouthSmileLeft] = 0.0f,
            [ShioriBlendShapeNames.mouthFrownLeft] = 0.0f,
            [ShioriBlendShapeNames.mouthPressLeft] = 0.0f,
            [ShioriBlendShapeNames.mouthUpperUpLeft] = 0.0f,
            [ShioriBlendShapeNames.mouthLowerDownLeft] = 0.0f,
            [ShioriBlendShapeNames.mouthStretchLeft] = 0.0f,
            [ShioriBlendShapeNames.mouthDimpleLeft] = 0.0f,

            [ShioriBlendShapeNames.mouthRight] = 0.0f,
            [ShioriBlendShapeNames.mouthSmileRight] = 0.0f,
            [ShioriBlendShapeNames.mouthFrownRight] = 0.0f,
            [ShioriBlendShapeNames.mouthPressRight] = 0.0f,
            [ShioriBlendShapeNames.mouthUpperUpRight] = 0.0f,
            [ShioriBlendShapeNames.mouthLowerDownRight] = 0.0f,
            [ShioriBlendShapeNames.mouthStretchRight] = 0.0f,
            [ShioriBlendShapeNames.mouthDimpleRight] = 0.0f,

            [ShioriBlendShapeNames.mouthClose] = 0.0f,
            [ShioriBlendShapeNames.mouthFunnel] = 0.0f,
            [ShioriBlendShapeNames.mouthPucker] = 0.0f,
            [ShioriBlendShapeNames.mouthShrugUpper] = 0.0f,
            [ShioriBlendShapeNames.mouthShrugLower] = 0.0f,
            [ShioriBlendShapeNames.mouthRollUpper] = 0.0f,
            [ShioriBlendShapeNames.mouthRollLower] = 0.0f,

            //鼻
            [ShioriBlendShapeNames.noseSneerLeft] = 0.0f,
            [ShioriBlendShapeNames.noseSneerRight] = 0.0f,

            //ほお
            [ShioriBlendShapeNames.cheekPuff] = 0.0f,
            [ShioriBlendShapeNames.cheekSquintLeft] = 0.0f,
            [ShioriBlendShapeNames.cheekSquintRight] = 0.0f,

            //舌
            [ShioriBlendShapeNames.tongueOut] = 0.0f,
        };

        //NOTE: 生データでは目のオイラー角表現が入ってるけど無視(ブレンドシェイプ側の情報を使う為)
        private readonly Dictionary<string, Vector3> _rotationData = new Dictionary<string, Vector3>()
        {
            [iFacialMocapRotationNames.head] = Vector3.zero,
        };

        private bool _hasReceiveRawPosition = false;
        private Vector3 _rawPosition = Vector3.zero;

        //Shiori uses websocket.
        private WebSocketServer WSServer;

        private void Update()
        {
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
            LogOutput.Instance.Write("Start Shiori receiver");
            StopReceive();
            WSServer = new WebSocketServer(PortNumber);
            WSServer.AddWebSocketService("/", () => new ShioriWebSocketBehavior(this));
            WSServer.Start();

            IsRunning = true;
        }

        public override void StopReceive()
        {
            LogOutput.Instance.Write("Stop Shiori receiver");
            if (WSServer != null)
            {
                WSServer.Stop();
                WSServer = null;
            }
            IsRunning = false;
            RawMessage = "";
            _prevMessage = "";
        }

        #endregion

        #region デシリアライズまわり

        private void DeserializeMessageWithLessGcAlloc(string msg)
        {
            var data = JsonUtility.FromJson<ShioriInfo>(msg);

            foreach (var kv in data.GetType().GetFields())
            {
                _blendShapes[kv.Name] = (float)kv.GetValue(data);
            }

            _rotationData[iFacialMocapRotationNames.head] = new Vector3(-data.P, data.Y, -data.R);
            _hasReceiveRawPosition = true;
            _rawPosition = new Vector3(data.PX, data.PY, data.PZ);

        }

        private void ApplyDeserializeResult()
        {
            _faceTrackSource.FaceTransform.Rotation =
                Quaternion.Euler(_rotationData[iFacialMocapRotationNames.head]);
            _faceTrackSource.FaceTransform.HasValidPosition = _hasReceiveRawPosition;
            _faceTrackSource.FaceTransform.Position = _rawPosition;

            //目
            {
                _faceTrackSource.Eye.LeftBlink = _blendShapes[ShioriBlendShapeNames.eyeBlinkLeft];
                _faceTrackSource.Eye.LeftSquint = _blendShapes[ShioriBlendShapeNames.eyeSquintLeft];
                _faceTrackSource.Eye.LeftWide = _blendShapes[ShioriBlendShapeNames.eyeWideLeft];
                _faceTrackSource.Eye.LeftLookIn = _blendShapes[ShioriBlendShapeNames.eyeLookInLeft];
                _faceTrackSource.Eye.LeftLookOut = _blendShapes[ShioriBlendShapeNames.eyeLookOutLeft];
                _faceTrackSource.Eye.LeftLookUp = _blendShapes[ShioriBlendShapeNames.eyeLookUpLeft];
                _faceTrackSource.Eye.LeftLookDown = _blendShapes[ShioriBlendShapeNames.eyeLookDownLeft];

                _faceTrackSource.Eye.RightBlink = _blendShapes[ShioriBlendShapeNames.eyeBlinkRight];
                _faceTrackSource.Eye.RightSquint = _blendShapes[ShioriBlendShapeNames.eyeSquintRight];
                _faceTrackSource.Eye.RightWide = _blendShapes[ShioriBlendShapeNames.eyeWideRight];
                _faceTrackSource.Eye.RightLookIn = _blendShapes[ShioriBlendShapeNames.eyeLookInRight];
                _faceTrackSource.Eye.RightLookOut = _blendShapes[ShioriBlendShapeNames.eyeLookOutRight];
                _faceTrackSource.Eye.RightLookUp = _blendShapes[ShioriBlendShapeNames.eyeLookUpRight];
                _faceTrackSource.Eye.RightLookDown = _blendShapes[ShioriBlendShapeNames.eyeLookDownRight];
                LimitSquint();
            }

            //口: 単純に数が多い！
            {
                _faceTrackSource.Mouth.Left = _blendShapes[ShioriBlendShapeNames.mouthLeft];
                _faceTrackSource.Mouth.LeftSmile = _blendShapes[ShioriBlendShapeNames.mouthSmileLeft];
                _faceTrackSource.Mouth.LeftFrown = _blendShapes[ShioriBlendShapeNames.mouthFrownLeft];
                _faceTrackSource.Mouth.LeftPress = _blendShapes[ShioriBlendShapeNames.mouthPressLeft];
                _faceTrackSource.Mouth.LeftUpperUp = _blendShapes[ShioriBlendShapeNames.mouthUpperUpLeft];
                _faceTrackSource.Mouth.LeftLowerDown = _blendShapes[ShioriBlendShapeNames.mouthLowerDownLeft];
                _faceTrackSource.Mouth.LeftStretch = _blendShapes[ShioriBlendShapeNames.mouthStretchLeft];
                _faceTrackSource.Mouth.LeftDimple = _blendShapes[ShioriBlendShapeNames.mouthDimpleLeft];

                _faceTrackSource.Mouth.Right = _blendShapes[ShioriBlendShapeNames.mouthRight];
                _faceTrackSource.Mouth.RightSmile = _blendShapes[ShioriBlendShapeNames.mouthSmileRight];
                _faceTrackSource.Mouth.RightFrown = _blendShapes[ShioriBlendShapeNames.mouthFrownRight];
                _faceTrackSource.Mouth.RightPress = _blendShapes[ShioriBlendShapeNames.mouthPressRight];
                _faceTrackSource.Mouth.RightUpperUp = _blendShapes[ShioriBlendShapeNames.mouthUpperUpRight];
                _faceTrackSource.Mouth.RightLowerDown = _blendShapes[ShioriBlendShapeNames.mouthLowerDownRight];
                _faceTrackSource.Mouth.RightStretch = _blendShapes[ShioriBlendShapeNames.mouthStretchRight];
                _faceTrackSource.Mouth.RightDimple = _blendShapes[ShioriBlendShapeNames.mouthDimpleRight];

                _faceTrackSource.Mouth.Close = _blendShapes[ShioriBlendShapeNames.mouthClose];
                _faceTrackSource.Mouth.Funnel = _blendShapes[ShioriBlendShapeNames.mouthFunnel];
                _faceTrackSource.Mouth.Pucker = _blendShapes[ShioriBlendShapeNames.mouthPucker];
                _faceTrackSource.Mouth.ShrugUpper = _blendShapes[ShioriBlendShapeNames.mouthShrugUpper];
                _faceTrackSource.Mouth.ShrugLower = _blendShapes[ShioriBlendShapeNames.mouthShrugLower];
                _faceTrackSource.Mouth.RollUpper = _blendShapes[ShioriBlendShapeNames.mouthRollUpper];
                _faceTrackSource.Mouth.RollLower = _blendShapes[ShioriBlendShapeNames.mouthRollLower];
            }

            //その他いろいろ
            {
                _faceTrackSource.Brow.InnerUp = _blendShapes[ShioriBlendShapeNames.browInnerUp];
                _faceTrackSource.Brow.LeftDown = _blendShapes[ShioriBlendShapeNames.browDownLeft];
                _faceTrackSource.Brow.LeftOuterUp = _blendShapes[ShioriBlendShapeNames.browOuterUpLeft];
                _faceTrackSource.Brow.RightDown = _blendShapes[ShioriBlendShapeNames.browDownRight];
                _faceTrackSource.Brow.RightOuterUp = _blendShapes[ShioriBlendShapeNames.browOuterUpRight];

                _faceTrackSource.Jaw.Open = _blendShapes[ShioriBlendShapeNames.jawOpen];
                _faceTrackSource.Jaw.Forward = _blendShapes[ShioriBlendShapeNames.jawForward];
                _faceTrackSource.Jaw.Left = _blendShapes[ShioriBlendShapeNames.jawLeft];
                _faceTrackSource.Jaw.Right = _blendShapes[ShioriBlendShapeNames.jawRight];

                _faceTrackSource.Nose.LeftSneer = _blendShapes[ShioriBlendShapeNames.noseSneerLeft];
                _faceTrackSource.Nose.RightSneer = _blendShapes[ShioriBlendShapeNames.noseSneerRight];

                _faceTrackSource.Cheek.Puff = _blendShapes[ShioriBlendShapeNames.cheekPuff];
                _faceTrackSource.Cheek.LeftSquint = _blendShapes[ShioriBlendShapeNames.cheekSquintLeft];
                _faceTrackSource.Cheek.RightSquint = _blendShapes[ShioriBlendShapeNames.cheekSquintRight];

                _faceTrackSource.Tongue.TongueOut = _blendShapes[ShioriBlendShapeNames.tongueOut];
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
                _faceTrackSource.Eye.LeftBlink = Mathf.Lerp(_faceTrackSource.Eye.LeftBlink, _blendShapes[ShioriBlendShapeNames.eyeBlinkLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftSquint = Mathf.Lerp(_faceTrackSource.Eye.LeftSquint, _blendShapes[ShioriBlendShapeNames.eyeSquintLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftWide = Mathf.Lerp(_faceTrackSource.Eye.LeftWide, _blendShapes[ShioriBlendShapeNames.eyeWideLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftLookIn = Mathf.Lerp(_faceTrackSource.Eye.LeftLookIn, _blendShapes[ShioriBlendShapeNames.eyeLookInLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftLookOut = Mathf.Lerp(_faceTrackSource.Eye.LeftLookOut, _blendShapes[ShioriBlendShapeNames.eyeLookOutLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftLookUp = Mathf.Lerp(_faceTrackSource.Eye.LeftLookUp, _blendShapes[ShioriBlendShapeNames.eyeLookUpLeft], UpdateApplyRate);
                _faceTrackSource.Eye.LeftLookDown = Mathf.Lerp(_faceTrackSource.Eye.LeftLookDown, _blendShapes[ShioriBlendShapeNames.eyeLookDownLeft], UpdateApplyRate);

                _faceTrackSource.Eye.RightBlink = Mathf.Lerp(_faceTrackSource.Eye.RightBlink, _blendShapes[ShioriBlendShapeNames.eyeBlinkRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightSquint = Mathf.Lerp(_faceTrackSource.Eye.RightSquint, _blendShapes[ShioriBlendShapeNames.eyeSquintRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightWide = Mathf.Lerp(_faceTrackSource.Eye.RightWide, _blendShapes[ShioriBlendShapeNames.eyeWideRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightLookIn = Mathf.Lerp(_faceTrackSource.Eye.RightLookIn, _blendShapes[ShioriBlendShapeNames.eyeLookInRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightLookOut = Mathf.Lerp(_faceTrackSource.Eye.RightLookOut, _blendShapes[ShioriBlendShapeNames.eyeLookOutRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightLookUp = Mathf.Lerp(_faceTrackSource.Eye.RightLookUp, _blendShapes[ShioriBlendShapeNames.eyeLookUpRight], UpdateApplyRate);
                _faceTrackSource.Eye.RightLookDown = Mathf.Lerp(_faceTrackSource.Eye.RightLookDown, _blendShapes[ShioriBlendShapeNames.eyeLookDownRight], UpdateApplyRate);
                LimitSquint();
            }

            //口: 単純に数が多い！
            {
                _faceTrackSource.Mouth.Left = Mathf.Lerp(_faceTrackSource.Mouth.Left, _blendShapes[ShioriBlendShapeNames.mouthLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftSmile = Mathf.Lerp(_faceTrackSource.Mouth.LeftSmile, _blendShapes[ShioriBlendShapeNames.mouthSmileLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftFrown = Mathf.Lerp(_faceTrackSource.Mouth.LeftFrown, _blendShapes[ShioriBlendShapeNames.mouthFrownLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftPress = Mathf.Lerp(_faceTrackSource.Mouth.LeftPress, _blendShapes[ShioriBlendShapeNames.mouthPressLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftUpperUp = Mathf.Lerp(_faceTrackSource.Mouth.LeftUpperUp, _blendShapes[ShioriBlendShapeNames.mouthUpperUpLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftLowerDown = Mathf.Lerp(_faceTrackSource.Mouth.LeftLowerDown, _blendShapes[ShioriBlendShapeNames.mouthLowerDownLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftStretch = Mathf.Lerp(_faceTrackSource.Mouth.LeftStretch, _blendShapes[ShioriBlendShapeNames.mouthStretchLeft], UpdateApplyRate);
                _faceTrackSource.Mouth.LeftDimple = Mathf.Lerp(_faceTrackSource.Mouth.LeftDimple, _blendShapes[ShioriBlendShapeNames.mouthDimpleLeft], UpdateApplyRate);

                _faceTrackSource.Mouth.Right = Mathf.Lerp(_faceTrackSource.Mouth.Right, _blendShapes[ShioriBlendShapeNames.mouthRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightSmile = Mathf.Lerp(_faceTrackSource.Mouth.RightSmile, _blendShapes[ShioriBlendShapeNames.mouthSmileRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightFrown = Mathf.Lerp(_faceTrackSource.Mouth.RightFrown, _blendShapes[ShioriBlendShapeNames.mouthFrownRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightPress = Mathf.Lerp(_faceTrackSource.Mouth.RightPress, _blendShapes[ShioriBlendShapeNames.mouthPressRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightUpperUp = Mathf.Lerp(_faceTrackSource.Mouth.RightUpperUp, _blendShapes[ShioriBlendShapeNames.mouthUpperUpRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightLowerDown = Mathf.Lerp(_faceTrackSource.Mouth.RightLowerDown, _blendShapes[ShioriBlendShapeNames.mouthLowerDownRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightStretch = Mathf.Lerp(_faceTrackSource.Mouth.RightStretch, _blendShapes[ShioriBlendShapeNames.mouthStretchRight], UpdateApplyRate);
                _faceTrackSource.Mouth.RightDimple = Mathf.Lerp(_faceTrackSource.Mouth.RightDimple, _blendShapes[ShioriBlendShapeNames.mouthDimpleRight], UpdateApplyRate);

                _faceTrackSource.Mouth.Close = Mathf.Lerp(_faceTrackSource.Mouth.Close, _blendShapes[ShioriBlendShapeNames.mouthClose], UpdateApplyRate);
                _faceTrackSource.Mouth.Funnel = Mathf.Lerp(_faceTrackSource.Mouth.Funnel, _blendShapes[ShioriBlendShapeNames.mouthFunnel], UpdateApplyRate);
                _faceTrackSource.Mouth.Pucker = Mathf.Lerp(_faceTrackSource.Mouth.Pucker, _blendShapes[ShioriBlendShapeNames.mouthPucker], UpdateApplyRate);
                _faceTrackSource.Mouth.ShrugUpper = Mathf.Lerp(_faceTrackSource.Mouth.ShrugUpper, _blendShapes[ShioriBlendShapeNames.mouthShrugUpper], UpdateApplyRate);
                _faceTrackSource.Mouth.ShrugLower = Mathf.Lerp(_faceTrackSource.Mouth.ShrugLower, _blendShapes[ShioriBlendShapeNames.mouthShrugLower], UpdateApplyRate);
                _faceTrackSource.Mouth.RollUpper = Mathf.Lerp(_faceTrackSource.Mouth.RollUpper, _blendShapes[ShioriBlendShapeNames.mouthRollUpper], UpdateApplyRate);
                _faceTrackSource.Mouth.RollLower = Mathf.Lerp(_faceTrackSource.Mouth.RollLower, _blendShapes[ShioriBlendShapeNames.mouthRollLower], UpdateApplyRate);
            }

            //その他いろいろ
            {
                _faceTrackSource.Brow.InnerUp = Mathf.Lerp(_faceTrackSource.Brow.InnerUp, _blendShapes[ShioriBlendShapeNames.browInnerUp], UpdateApplyRate);
                _faceTrackSource.Brow.LeftDown = Mathf.Lerp(_faceTrackSource.Brow.LeftDown, _blendShapes[ShioriBlendShapeNames.browDownLeft], UpdateApplyRate);
                _faceTrackSource.Brow.LeftOuterUp = Mathf.Lerp(_faceTrackSource.Brow.LeftOuterUp, _blendShapes[ShioriBlendShapeNames.browOuterUpLeft], UpdateApplyRate);
                _faceTrackSource.Brow.RightDown = Mathf.Lerp(_faceTrackSource.Brow.RightDown, _blendShapes[ShioriBlendShapeNames.browDownRight], UpdateApplyRate);
                _faceTrackSource.Brow.RightOuterUp = Mathf.Lerp(_faceTrackSource.Brow.RightOuterUp, _blendShapes[ShioriBlendShapeNames.browOuterUpRight], UpdateApplyRate);

                _faceTrackSource.Jaw.Open = Mathf.Lerp(_faceTrackSource.Jaw.Open, _blendShapes[ShioriBlendShapeNames.jawOpen], UpdateApplyRate);
                _faceTrackSource.Jaw.Forward = Mathf.Lerp(_faceTrackSource.Jaw.Forward, _blendShapes[ShioriBlendShapeNames.jawForward], UpdateApplyRate);
                _faceTrackSource.Jaw.Left = Mathf.Lerp(_faceTrackSource.Jaw.Left, _blendShapes[ShioriBlendShapeNames.jawLeft], UpdateApplyRate);
                _faceTrackSource.Jaw.Right = Mathf.Lerp(_faceTrackSource.Jaw.Right, _blendShapes[ShioriBlendShapeNames.jawRight], UpdateApplyRate);

                _faceTrackSource.Nose.LeftSneer = Mathf.Lerp(_faceTrackSource.Nose.LeftSneer, _blendShapes[ShioriBlendShapeNames.noseSneerLeft], UpdateApplyRate);
                _faceTrackSource.Nose.RightSneer = Mathf.Lerp(_faceTrackSource.Nose.RightSneer, _blendShapes[ShioriBlendShapeNames.noseSneerRight], UpdateApplyRate);

                _faceTrackSource.Cheek.Puff = Mathf.Lerp(_faceTrackSource.Cheek.Puff, _blendShapes[ShioriBlendShapeNames.cheekPuff], UpdateApplyRate);
                _faceTrackSource.Cheek.LeftSquint = Mathf.Lerp(_faceTrackSource.Cheek.LeftSquint, _blendShapes[ShioriBlendShapeNames.cheekSquintLeft], UpdateApplyRate);
                _faceTrackSource.Cheek.RightSquint = Mathf.Lerp(_faceTrackSource.Cheek.RightSquint, _blendShapes[ShioriBlendShapeNames.cheekSquintRight], UpdateApplyRate);

                _faceTrackSource.Tongue.TongueOut = Mathf.Lerp(_faceTrackSource.Tongue.TongueOut, _blendShapes[ShioriBlendShapeNames.tongueOut], UpdateApplyRate);
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
            //NOTE: Shioriは端末座標系でpos/rotを送ってくる。ので、記録すべきデータはそのpos/rotそのもの。
            RotOffset = Quaternion.Inverse(Quaternion.Euler(_rotationData[iFacialMocapRotationNames.head]));
            PosOffset = -_rawPosition;
        }

        public override string CalibrationData
        {
            get
            {
                var rEuler = _rotOffset.eulerAngles;
                var data = new ShioriCalibrationData()
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
                    var data = JsonUtility.FromJson<ShioriCalibrationData>(value);
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
    public class ShioriCalibrationData
    {
        public float rx;
        public float ry;
        public float rz;
        public float px;
        public float py;
        public float pz;
    }

    [Serializable]
    public struct ShioriInfo
    {
        public float eBL;
        public float eLUL;
        public float eLDL;
        public float eLIL;
        public float eLOL;
        public float eWL;
        public float eSL;
        public float eBR;
        public float eLUR;
        public float eLDR;
        public float eLIR;
        public float eLOR;
        public float eWR;
        public float eSR;
        public float mL;
        public float mSL;
        public float mFL;
        public float mPL;
        public float mUUL;
        public float mLDL;
        public float mSTL;
        public float mDL;
        public float mR;
        public float mSR;
        public float mFR;
        public float mPR;
        public float mUUR;
        public float mLDR;
        public float mSTR;
        public float mDR;
        public float mC;
        public float mF;
        public float mP;
        public float mSSU;
        public float mSSL;
        public float mRU;
        public float mRL;
        public float jO;
        public float jF;
        public float jL;
        public float jR;
        public float nSL;
        public float nSR;
        public float cP;
        public float cSL;
        public float cSR;
        public float tO;
        public float bDL;
        public float bOUL;
        public float bDR;
        public float bOUR;
        public float bIU;
        public float R;
        public float Y;
        public float P;
        public float d;
        public float PX;
        public float PY;
        public float PZ;
    }
}