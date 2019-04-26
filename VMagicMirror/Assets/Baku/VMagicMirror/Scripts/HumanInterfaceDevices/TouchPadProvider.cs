using UnityEngine;

namespace Baku.VMagicMirror
{
    public class TouchPadProvider : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<MeshRenderer>().material = HIDMaterialUtil.Instance.GetPadMaterial();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">-1(left) to 1(right) value of mouse horizontal pos</param>
        /// <param name="y">-1(bottom) to 1(top) value of mouse vertical pos</param>
        /// <returns></returns>
        public Vector3 GetHandTipPosFromScreenPoint(float x, float y) 
            => transform.TransformPoint(new Vector2(x * 0.5f, y * 0.5f));
    }
}
