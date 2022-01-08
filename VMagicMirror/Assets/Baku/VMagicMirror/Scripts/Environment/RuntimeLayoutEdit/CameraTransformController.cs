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

        private Camera _cam;
        private Vector3 preMousePos;
        private Vector3 rotateCenterInThisAngleMove;

        private void Start()
        {
            _cam = GetComponent<Camera>();
        }

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

            if (Input.GetMouseButtonDown(RightMouseButton))
            {
                rotateCenterInThisAngleMove = CheckRotateCenter(Input.mousePosition);
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

            //NOTE: Shift + 左クリックはマウスのないノートPC環境のための代替的なオプション
            if (Input.GetMouseButton(MiddleMouseButton) || 
                (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetMouseButton(LeftMouseButton)
               )
            {
                transform.Translate(-diff * Time.deltaTime * moveSpeed);
            }
            else if (Input.GetMouseButton(RightMouseButton))
            {
                CameraRotateAround(
                    new Vector2(-diff.y, diff.x) * rotateSpeed,
                    rotateCenterInThisAngleMove
                    );
            }

            preMousePos = mousePos;
        }

        private void CameraRotateAround(Vector2 angle, Vector3 rotateCenter)
        {
            transform.RotateAround(rotateCenter, transform.right, angle.x);
            transform.RotateAround(rotateCenter, Vector3.up, angle.y);
        }

        private Vector3 CheckRotateCenter(Vector3 mousePosition)
        {
            //やりたいこと: 
            // - マウス位置から求めたカメラレイと、Y軸の2直線間の最短距離になるようなカメラレイ上の点を求める
            // - 求めた点をそのまんまカメラの回転中心にする
            // - 結果として「Y軸にちかいところで頑張って回転してます」感が出ればOK
            //ref: http://marupeke296.com/COL_3D_No19_LinesDistAndPos.html

            var camRay = _cam.ScreenPointToRay(mousePosition);
            var p1 = camRay.origin;
            var v1 = camRay.direction;

            var p2 = Vector3.zero;
            var v2 = Vector3.up;

            float d1 = Vector3.Dot(p2 - p1, v1);
            float d2 = Vector3.Dot(p2 - p1, v2);
            float dv = Vector3.Dot(v1, v2);

            if (dv * dv > 0.999)
            {
                //ふたつのRayがほとんど平行: つまりカメラが真下か真上を向いているので、このときはその場回転でOK
                return transform.position;
            }
            else
            {
                //note: 元記事では最短距離をつくる点を求めてるが、ここではq1(カメラレイ側の点)が分かったら十分
                float t1 = (d1 - d2 * dv) / (1 - dv * dv);
                return p1 + t1 * v1;
            }
        }
    }

}
