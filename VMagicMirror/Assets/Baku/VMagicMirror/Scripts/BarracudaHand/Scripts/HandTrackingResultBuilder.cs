using System;
using MediaPipe.HandPose;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class HandTrackingResultBuilder
    {
        //NOTE: webCamTextureの映像を左右に分けたときの、片方ずつで使ってるwidthの比率
        public float SingleSideWidthRate { get; set; } = 0.6f;

        public HandTrackingResultBuilder(IMessageSender sender)
        {
            _sender = sender;
        }

        private readonly IMessageSender _sender;
        private readonly HandTrackingResult _result = new HandTrackingResult();

        public void SendResult(
            bool leftDetected,
            float leftConfidence,
            Vector3[] leftKeyPoints,
            bool rightDetected,
            float rightConfidence,
            Vector3[] rightKeyPoints
            )
        {
            _result.left.is_detected = leftDetected;
            _result.left.confidence = leftConfidence;
            for (int i = 0; i < leftKeyPoints.Length; i++)
            {
                _result.left.points[i] = CreatePoint(leftKeyPoints[i], true);
            }

            _result.right.is_detected = rightDetected;
            _result.right.confidence = rightConfidence;
            for (int i = 0; i < rightKeyPoints.Length; i++)
            {
                _result.right.points[i] = CreatePoint(rightKeyPoints[i], false);
            }

            _sender.SendCommand(MessageFactory.Instance.SetHandTrackingResult(JsonUtility.ToJson(_result)));
        }

        private HandTrackingKeyPoint CreatePoint(Vector3 p, bool isLeftHand)
        {
            //NOTE: 画像が左右に切れてるぶんp.x (-0.5 ~ 0.5)の値域の取り扱いにやや注意。
            //rateXは最終的に、webCamTexture全体に対する比率になるよう調整してます
            float rateX = (p.x + 0.5f) * SingleSideWidthRate;
            if (!isLeftHand)
            {
                rateX += 1.0f - SingleSideWidthRate;
            }

            return new HandTrackingKeyPoint()
            {
                x = rateX,
                y = p.y + 0.5f,
            };
        }
    }
    
    [Serializable]
    public class HandTrackingResult
    {
        public HandTrackingSingleResult left = new HandTrackingSingleResult();
        public HandTrackingSingleResult right = new HandTrackingSingleResult();

    }

    [Serializable]
    public class HandTrackingSingleResult
    {
        public bool is_detected;
        public float confidence;
        public HandTrackingKeyPoint[] points = new HandTrackingKeyPoint[HandPipeline.KeyPointCount];
    }

    [Serializable]
    public struct HandTrackingKeyPoint
    {
        public float x;
        public float y;
    }
}
