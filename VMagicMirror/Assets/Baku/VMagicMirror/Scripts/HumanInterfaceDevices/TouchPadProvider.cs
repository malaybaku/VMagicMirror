using UnityEngine;

namespace Baku.VMagicMirror
{
    public class TouchPadProvider : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">-1(left) to 1(right) value of mouse horizontal pos</param>
        /// <param name="y">-1(bottom) to 1(top) value of mouse vertical pos</param>
        /// <returns></returns>
        public Vector3 GetHandTipPosFromScreenPoint(float x, float y)
        {
            //var bottomLeft = new Vector2(-0.3f, -0.3f);
            //var topRight = new Vector2(0.3f, 0.3f);

            //return transform.TransformPoint(new Vector2(
            //    Mathf.Lerp(bottomLeft.x, topRight.x, (x + 1) * 0.5f),
            //    Mathf.Lerp(bottomLeft.y, topRight.y, (y + 1) * 0.5f)
            //    ));

            return transform.TransformPoint(new Vector2(x, y));
        }
    }
}
