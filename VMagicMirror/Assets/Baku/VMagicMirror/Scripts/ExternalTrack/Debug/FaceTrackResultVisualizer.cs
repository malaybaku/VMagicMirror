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
        public IFaceTrackSource Source { get; set; } = null;

        private void Update()
        {
            if (Source == null)
            {
                return;
            }
            
            //NOTE: 右手系/左手系の差とか軸の扱いによってはここがウソになるかもしれないので注意
            var t = transform;
            t.localPosition = Source.FaceTransform.Position;
            t.localRotation = Source.FaceTransform.Rotation;
        }
    }
}