using System;
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
    public class FaceTrackResultVisualizer : MonoBehaviour
    {
        [SerializeField] private ExternalTrackSourceProvider provider = null;

        [SerializeField] private bool useCalibrated = false;
        
        private void Update()
        {
            //NOTE: 右手系/左手系の差とか軸の扱いによってはここがウソになるかもしれないので注意
            var t = transform;
            var source = provider.FaceTrackSource;

            if (useCalibrated)
            {
                t.localPosition = provider.HeadPositionOffset;
                t.localRotation = provider.HeadRotation;
            }
            else
            {
                t.localPosition =
                    source.FaceTransform.HasValidPosition ? source.FaceTransform.Position : Vector3.zero;
                t.localRotation = provider.FaceTrackSource.FaceTransform.Rotation;
            }
        }
    }
}