using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker.iFacialMocap
{
    /// <summary>
    /// iFacialMocapの6DOF値を単に受け取ったまま表示するデバッグ用クラス。
    /// </summary>
    public class iFacialMocapRaw6DofVisualizer : MonoBehaviour
    {
        [SerializeField] private iFacialMocapReceiver _receiver = null;
        [SerializeField] private bool useCalibratedData = false;
        
        private void Update()
        {
            var t = transform;

            if (useCalibratedData)
            {
                t.localPosition = _receiver.HeadPositionOffset;
                t.localRotation = _receiver.HeadRotation;
            }
            else
            {
                t.localPosition = _receiver.FaceTrackSource.FaceTransform.Position;
                t.localRotation = _receiver.FaceTrackSource.FaceTransform.Rotation;
            }
        }
    }
}
