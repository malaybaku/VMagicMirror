using UnityEngine;

namespace Baku.VMagicMirror
{
    public class TouchPadProvider : MonoBehaviour
    {
        private void Start()
        {
            foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = HIDMaterialUtil.Instance.GetPadMaterial();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">-1(left) to 1(right) value of mouse horizontal pos</param>
        /// <param name="y">-1(bottom) to 1(top) value of mouse vertical pos</param>
        /// <returns></returns>
        public Vector3 GetHandTipPosFromScreenPoint(float x, float y)
        {
            //ベーシックな方法: ウィンドウのサイズとの比較で場所を決める
            //return transform.TransformPoint(new Vector2(x * 0.5f, y * 0.5f));
            
            //hackな方法: Windowsの全画面の領域と比較して「マウスがこの辺」という計算をする。入力値は捨てます
            
            //懸念事項: ここ毎フレーム呼んだら重くなったりしない？なんか重そうな気がする
            // -> そうでもなかった
            var p = NativeMethods.GetWindowsMousePosition();
            int left = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricsConsts.SM_XVIRTUALSCREEN);
            int top = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricsConsts.SM_YVIRTUALSCREEN);
            int width = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricsConsts.SM_CXVIRTUALSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeMethods.SystemMetricsConsts.SM_CYVIRTUALSCREEN);

            //NOTE: 右方向を+X, 上方向を+Y, 値域を(-0.5, 0.5)にするための変形がかかってます
            Vector2 cursorPosInVirtualScreen = new Vector2(
                (p.x - left) * 1.0f / width - 0.5f,
                0.5f - (p.y - top) * 1.0f / height
            );

            //NOTE: 0.95をかけて何が嬉しいかというと、パッドのギリギリのエリアを避けてくれるようになります
            return transform.TransformPoint(cursorPosInVirtualScreen * 0.95f);
        }

        /// <summary>
        /// <see cref="GetHandTipPosFromScreenPoint"/>の結果、またはその結果をローパスした座標を指定することで、
        /// その座標にVRMの右手を持っていくときの望ましいワールド回転値を計算し、取得します。
        /// </summary>
        /// <returns></returns>
        public Quaternion GetWristRotation(Vector3 pos)
            => transform.rotation *
               Quaternion.AngleAxis(-90, Vector3.right) * 
               Quaternion.AngleAxis(-90, Vector3.up);

        /// <summary>
        /// 手首から指までの位置を考慮するオフセットベクトルを、オフセットの量を指定して取得します。
        /// </summary>
        /// <param name="yOffset"></param>
        /// <param name="palmToTipLength"></param>
        /// <returns></returns>
        public Vector3 GetOffsetVector(float yOffset, float palmToTipLength)
        {
            var t = transform;
            return (-yOffset) * t.forward + (-palmToTipLength) * t.up;

        }
    }
}
