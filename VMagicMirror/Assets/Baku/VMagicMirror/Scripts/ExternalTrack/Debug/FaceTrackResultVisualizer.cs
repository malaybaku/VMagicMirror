using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// 顔トラッキングデバイスから見た顔の位置情報を、必要ならば座標系を直しつつ適用します。
    /// </summary>
    /// <remarks>
    /// 逆に言うと、このスクリプトがアタッチされてるオブジェクトの親を「iOS端末がありそうな位置」にUnity上で配置する必要がある。
    /// (それによって首が吹っ飛んだりしない、いい感じの動作が約束される。はず。)
    /// </remarks>
    public class FacePoseAdaptor : MonoBehaviour
    {
        [SerializeField] private FaceTrackToPose trackToPose = null;

        private void Update()
        {
            //NOTE: これがウソになるかもしれない、ということ。右手系とか軸の差とか色々あるので。
            var t = transform;
            t.localPosition = trackToPose.FaceRelativePosition;
            t.localRotation = Quaternion.AngleAxis(
                trackToPose.FaceRotationAngle * Mathf.Rad2Deg, 
                trackToPose.FaceRotationAxis
                );
        }
    }
}