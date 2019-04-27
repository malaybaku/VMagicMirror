using UnityEngine;

//コード元: http://esprog.hatenablog.com/entry/2016/03/20/033322
namespace Baku.VMagicMirror
{
    /// <summary>
    /// GameビューにてSceneビューのようなカメラの動きをマウス操作によって実現する
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraTransformController : MonoBehaviour
    {
        const int LeftMouseButton = 0;
        const int RightMouseButton = 1;
        const int MiddleMouseButton = 2;

        [SerializeField, Range(0.1f, 10f)]
        private float wheelSpeed = 1f;

        [SerializeField, Range(0.1f, 10f)]
        private float moveSpeed = 0.3f;

        [SerializeField, Range(0.1f, 10f)]
        private float rotateSpeed = 0.3f;

        private Vector3 preMousePos;

        private void Update()
        {
            float scrollWheel = Input.mouseScrollDelta.y * 0.1f;
            if (scrollWheel != 0.0f)
            {
                MouseWheel(scrollWheel);
            }

            if (Input.GetMouseButtonDown(LeftMouseButton) ||
               Input.GetMouseButtonDown(RightMouseButton) ||
               Input.GetMouseButtonDown(MiddleMouseButton))
            {
                preMousePos = Input.mousePosition;
            }

            MouseDrag(Input.mousePosition);
        }

        private void MouseWheel(float delta) 
            => transform.position += transform.forward * delta * wheelSpeed;

        private void MouseDrag(Vector3 mousePos)
        {
            Vector3 diff = mousePos - preMousePos;
            if (diff.magnitude < Vector3.kEpsilon)
            {
                return;
            }

            if (Input.GetMouseButton(MiddleMouseButton))
            {
                transform.Translate(-diff * Time.deltaTime * moveSpeed);
            }
            else if (Input.GetMouseButton(RightMouseButton))
            {
                CameraRotate(new Vector2(-diff.y, diff.x) * rotateSpeed);
            }

            preMousePos = mousePos;
        }

        private void CameraRotate(Vector2 angle)
        {
            transform.RotateAround(transform.position, transform.right, angle.x);
            transform.RotateAround(transform.position, Vector3.up, angle.y);
        }
    }
}
