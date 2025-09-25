using System;
using System.Linq;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Vision.HolisticLandmarker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class MediaPipeTrackerStatusPreviewSender : PresenterBase, ITickable
    {
        // NOTE: HandednessNameはHandTaskクラスでも定義してる。constの定義クラスを分けて作っても良いかも
        private const string LeftHandHandednessName = "Left";
        private const string RightHandHandednessName = "Right";
        private const float TrackingLostTimeout = 0.3f;

        private readonly IMessageReceiver _receiver;
        private readonly IMessageSender _sender;

        private readonly Atomic<bool> _sendHandTrackingResult = new(false);
        private readonly Atomic<SerializedHandTrackingResult> _handResultToSend = new(null);
        private float _handTrackingLostTime;
        private bool _handTracked;

        private readonly Atomic<bool> _sendBlinkResult = new(false);
        private readonly Atomic<SerializedEyeBlendShapeValues> _blinkResultToSend = new(null);
        
        public MediaPipeTrackerStatusPreviewSender(IMessageReceiver receiver, IMessageSender sender)
        {
            _receiver = receiver;
            _sender = sender;
            
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.EnableSendHandTrackingResult,
                m =>
                {
                    var value = m.ToBoolean();
                    _sendHandTrackingResult.Value = value;
                    if (!value)
                    {
                        _handResultToSend.Value = null;
                        _handTracked = false;
                    }
                });

            _receiver.AssignCommandHandler(
                VmmCommands.SetEyeBlendShapePreviewActive,
                m =>
                {
                    var value = m.ToBoolean();
                    _sendBlinkResult.Value = value; 
                    if (!value)
                    {
                        _blinkResultToSend.Value = null;
                    }
                });
        }

        void ITickable.Tick()
        {
            UpdateHandResult();
            UpdateBlinkResult();
        }

        private void UpdateHandResult()
        {
            if (!_sendHandTrackingResult.Value)
            {
                return;
            }

            var data = _handResultToSend.Value;
            _handResultToSend.Value = null;

            if (data != null)
            {
                _handTracked = true;
                SendResult(data);
                _handTrackingLostTime = 0f;
            }
            
            if (_handTracked)
            {
                _handTrackingLostTime += Time.deltaTime;
                if (_handTrackingLostTime > TrackingLostTimeout)
                {
                    _handTracked = false;
                    SendResult(SerializedHandTrackingResult.Empty);
                }
            }
            
            return;

            void SendResult(SerializedHandTrackingResult result)
            {
                _sender.SendCommand(MessageFactory.SetHandTrackingResult(
                    JsonUtility.ToJson(result)
                ));
            }
        }

        private void UpdateBlinkResult()
        {
            if (!_sendBlinkResult.Value)
            {
                return;
            }

            var data = _blinkResultToSend.Value;
            _blinkResultToSend.Value = null;
            if (data != null)
            {
                _sender.SendCommand(MessageFactory.EyeBlendShapeValues(
                    JsonUtility.ToJson(data)
                ));
            }
        }
        
        /// <summary>
        /// 手のトラッキングを試みた結果を指定して、WPFに送信するプレビュー用データを更新する。
        /// トラッキングロストしている場合、呼んでも呼ばなくてもよい。
        /// また、メインスレッド外から呼び出してよい。
        /// </summary>
        /// <param name="result"></param>
        public void SetHandTrackingResult(HandLandmarkerResult result)
        {
            if (!_sendHandTrackingResult.Value ||
                result.handedness == null ||
                result.handLandmarks == null
                )
            {
                return;
            }

            var serialized = new SerializedHandTrackingResult();
            for (var i = 0; i < result.handedness.Count; i++)
            {
                // categoryが無い可能性は考慮しない
                var categoryName = result.handedness[i].categories[0].categoryName;
                switch (categoryName)
                {
                    case LeftHandHandednessName:
                        serialized.LeftHandPoints = result.handLandmarks[i].landmarks
                            .Select(m => new SerializedHandTrackingResultPoint()
                            {
                                X = m.x,
                                Y = m.y,
                            })
                            .ToArray();
                        serialized.HasLeftHand = true;
                        break;
                    case RightHandHandednessName:
                        serialized.RightHandPoints = result.handLandmarks[i].landmarks
                            .Select(m => new SerializedHandTrackingResultPoint()
                            {
                                X = m.x,
                                Y = m.y,
                            })
                            .ToArray();
                        serialized.HasRightHand = true;
                        break;
                }
            }

            if (serialized.HasLeftHand || serialized.HasRightHand)
            {
                _handResultToSend.Value = serialized;
            }
        }
        
        public void SetHandTrackingResult(HolisticLandmarkerResult result)
        {
            if (!_sendHandTrackingResult.Value ||
                (result.leftHandLandmarks.landmarks == null && result.rightHandLandmarks.landmarks == null)
                )
            {
                return;
            }

            var serialized = new SerializedHandTrackingResult();
            if (result.leftHandLandmarks.landmarks != null)
            {
                serialized.LeftHandPoints = result.leftHandLandmarks.landmarks
                    .Select(m => new SerializedHandTrackingResultPoint()
                    {
                        X = m.x,
                        Y = m.y,
                    })
                    .ToArray();
                serialized.HasLeftHand = true;
            }

            if (result.rightHandLandmarks.landmarks != null)
            {
                serialized.RightHandPoints = result.rightHandLandmarks.landmarks
                    .Select(m => new SerializedHandTrackingResultPoint()
                    {
                        X = m.x,
                        Y = m.y,
                    })
                    .ToArray();
                serialized.HasRightHand = true;
            }

            if (serialized.HasLeftHand || serialized.HasRightHand)
            {
                _handResultToSend.Value = serialized;
            }
        }

        /// <summary>
        /// 左右の目のまばたきブレンドシェイプ値をWPFに送信する値として指定する。
        /// 顔が検出できている間だけ呼び出す
        /// </summary>
        /// <param name="leftBlink"></param>
        /// <param name="rightBlink"></param>
        public void SetBlinkResult(float leftBlink, float rightBlink)
        {
            if (!_sendBlinkResult.Value)
            {
                return;
            }

            var serialized = new SerializedEyeBlendShapeValues
            {
                LeftBlink = leftBlink,
                RightBlink = rightBlink
            };
            _blinkResultToSend.Value = serialized;

            _sender.SendCommand(MessageFactory.EyeBlendShapeValues(
                JsonUtility.ToJson(serialized)
            ));
        }

        [Serializable]
        public class SerializedHandTrackingResult
        {
            public bool HasLeftHand;
            public bool HasRightHand;
            
            public SerializedHandTrackingResultPoint[] LeftHandPoints = Array.Empty<SerializedHandTrackingResultPoint>();
            public SerializedHandTrackingResultPoint[] RightHandPoints = Array.Empty<SerializedHandTrackingResultPoint>();

            public static SerializedHandTrackingResult Empty { get; } = new();
        }

        [Serializable]
        public class SerializedHandTrackingResultPoint
        {
            public float X;
            public float Y;
        }

        [Serializable]
        public class SerializedEyeBlendShapeValues
        {
            // NOTE: Squintとかを追加する方向に拡張する可能性があるので、Blinkと明記している
            public float LeftBlink;
            public float RightBlink;
        }
    }
}
