using Newtonsoft.Json;
using System;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{

    /// <summary> ハンドトラッキングの結果データのうちGUIで出したいもの </summary>
    public record HandTrackingResult(HandTrackingSingleResult Left, HandTrackingSingleResult Right);

    /// <summary> ハンドトラッキングの結果データのうち片手分 </summary>
    public record HandTrackingSingleResult(bool Detected, Point[] Points)
    {
        public static HandTrackingSingleResult Empty() => new HandTrackingSingleResult(false, Array.Empty<Point>());
    }

    public static class HandTrackingResultBuilder
    {
        public static HandTrackingResult FromJson(string json)
        {
            try
            {
                var serialized = JsonConvert.DeserializeObject<SerializedHandTrackingResult>(json);
                if (serialized == null)
                {
                    return new HandTrackingResult(HandTrackingSingleResult.Empty(), HandTrackingSingleResult.Empty());
                }

                return new HandTrackingResult(
                    new HandTrackingSingleResult(serialized.HasLeftHand, serialized.GetLeftHandPoints()),
                    new HandTrackingSingleResult(serialized.HasRightHand, serialized.GetRightHandPoints())
                    );
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return new HandTrackingResult(HandTrackingSingleResult.Empty(), HandTrackingSingleResult.Empty());
            }
        }
    }

    public class SerializedHandTrackingResult
    {

        public bool HasLeftHand { get; set; }

        public bool HasRightHand { get; set; }

        // NOTE: 下記はHasLeftHand/HasRightHandがtrueのときのみ有効。falseの場合、nullになったり空配列になったりする
        public SerializedHandTrackingResultPoint[]? LeftHandPoints { get; set; } 
            = Array.Empty<SerializedHandTrackingResultPoint>();

        public SerializedHandTrackingResultPoint[]? RightHandPoints { get; set; } 
            = Array.Empty<SerializedHandTrackingResultPoint>();

        public Point[] GetLeftHandPoints()
        {
            if (LeftHandPoints == null || LeftHandPoints.Length == 0)
            {
                return Array.Empty<Point>();
            }
            return LeftHandPoints.Select(p => new Point(p.X, p.Y)).ToArray();
        }

        public Point[] GetRightHandPoints()
        {
            if (RightHandPoints == null || RightHandPoints.Length == 0)
            {
                return Array.Empty<Point>();
            }
            return RightHandPoints.Select(p => new Point(p.X, p.Y)).ToArray();
        }
    }

    public class SerializedHandTrackingResultPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
    }
}
