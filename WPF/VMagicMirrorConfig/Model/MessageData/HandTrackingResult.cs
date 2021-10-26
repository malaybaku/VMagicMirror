using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{

    /// <summary>
    /// ハンドトラッキングの結果データのうちGUIで出したいもの
    /// </summary>
    public record HandTrackingResult(HandTrackingSingleResult Left, HandTrackingSingleResult Right);

    /// <summary>
    /// ハンドトラッキングの結果データのうち片手分
    /// </summary>
    public record HandTrackingSingleResult(
        bool Detected, 
        float Confidence,
        Point[] Points
        )
    {
        public static HandTrackingSingleResult Empty() => new HandTrackingSingleResult(false, 0f, Array.Empty<Point>());
    }

    public static class HandTrackingResultBuilder
    {
        public static HandTrackingResult FromJson(string json)
        {
            try
            {
                var jobj = JObject.Parse(json);

                var left = jobj["left"] as JObject;
                var right = jobj["right"] as JObject;
                var leftPoints = new List<Point>();

                if (left?["points"] is JArray rawLeftPoints)
                {
                    foreach (var p in rawLeftPoints)
                    {
                        if (p?["x"] is JValue px && p?["y"] is JValue py)
                        {
                            leftPoints.Add(new Point((double)px, (double)py));
                        }
                    }
                }

                var rightPoints = new List<Point>();
                if (right?["points"] is JArray rawRightPoints)
                {
                    foreach (var p in rawRightPoints)
                    {
                        if (p?["x"] is JValue px && p?["y"] is JValue py)
                        {
                            rightPoints.Add(new Point((double)px, (double)py));
                        }
                    }
                }

                bool leftDetected = (left?["is_detected"] is JValue ld) && (bool)ld;
                float leftConfidence = (left?["confidence"] is JValue lc) ? (float)lc : 0f;
                bool rightDetected = (right?["is_detected"] is JValue rd) && (bool)rd;
                float rightConfidence = (right?["confidence"] is JValue rc) ? (float)rc : 0f;

                return new HandTrackingResult(
                    new HandTrackingSingleResult(leftDetected, leftConfidence, leftPoints.ToArray()),
                    new HandTrackingSingleResult(rightDetected, rightConfidence, rightPoints.ToArray())
                    );
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return new HandTrackingResult(HandTrackingSingleResult.Empty(), HandTrackingSingleResult.Empty());
            }
        }
    }
}
