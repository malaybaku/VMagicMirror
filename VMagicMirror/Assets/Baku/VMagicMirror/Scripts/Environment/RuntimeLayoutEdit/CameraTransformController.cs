using System.Windows.Forms;
using UniRx;
using UnityEngine;
using Zenject;

//コード元: http://esprog.hatenablog.com/entry/2016/03/20/033322
namespace Baku.VMagicMirror
{
    /// <summary>
    /// GameビューにてSceneビューのようなカメラの動きをマウス操作によって実現する
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraTransformController : MonoBehaviour
    {
        private const int LeftMouseButton = 0;
        private const int RightMouseButton = 1;
        private const int MiddleMouseButton = 2;

        [SerializeField, Range(0.1f, 10f)] private float wheelSpeed = 1f;
        [SerializeField, Range(0.1f, 10f)] private float moveSpeed = 0.3f;
        [SerializeField, Range(0.1f, 10f)] private float rotateSpeed = 0.3f;

        private Camera _cam;
        private Vector3 _preMousePos;
        private Vector3 _rotateCenterInThisAngleMove;

        //NOTE: ビルドではRawInputによる妨害があるため、Input.GetKeyDownはまともに動作しない
        private bool _alt;
        private bool _shift;
        
        //Input.GetKeyDownの値に似ているが、Update()の冒頭でtrueになり、Update()が終わる時点でfalseになる。trueの期間が短い
        private bool _altDown;
        private bool _shiftDown;
        private bool _altDownHandled;
        private bool _shiftDownHandled;

        [Inject]
        public void Inject(IKeyMouseEventSource keySource)
        {
            //NOTE: ここでSubscribeした内容が使われるのはビルドのみ
            keySource.RawKeyDown
                .Subscribe(key =>
                {
                    if (key == nameof(Keys.RMenu) || key == nameof(Keys.LMenu))
                    {
                        _alt = true;
                    }
                    else if (key == nameof(Keys.LShiftKey) || key == nameof(Keys.RShiftKey))
                    {
                        _shift = true;
                    }
                })
                .AddTo(this);
            
            keySource.RawKeyUp
                .Subscribe(key =>
                {
                    //NOTE: LAlt押す > RAlt押す > LAlt離す、みたいな手順をとると整合しなくなるが、これは許容する。
                    //狙って押さなければ問題にならないし、両方とも離せばデフォルト状態には戻るため。
                    if (key == nameof(Keys.RMenu) || key == nameof(Keys.LMenu))
                    {
                        _alt = false;
                        _altDownHandled = false;
                    }
                    else if (key == nameof(Keys.LShiftKey) || key == nameof(Keys.RShiftKey))
                    {
                        _shift = false;
                        _shiftDownHandled = false;
                    }
                })
                .AddTo(this);
        }
        
        private void Start()
        {
            _cam = GetComponent<Camera>();
        }

        private void Update()
        {
            if (_shift && !_shiftDownHandled)
            {
                _shiftDown = true;
                _shiftDownHandled = true;
            }
            if (_alt && !_altDownHandled)
            {
                _altDown = true;
                _altDownHandled = true;
            }
            
            float scrollWheel = Input.mouseScrollDelta.y * 0.1f;
            if (scrollWheel != 0.0f && CheckMousePositionIsInsideWindow())
            {
                MouseWheel(scrollWheel);
            }

            if (Input.GetMouseButtonDown(LeftMouseButton) ||
               Input.GetMouseButtonDown(RightMouseButton) ||
               Input.GetMouseButtonDown(MiddleMouseButton))
            {
                _preMousePos = Input.mousePosition;
            }

            if (CheckRotateStart())
            {
                _rotateCenterInThisAngleMove = CheckRotateCenter(Input.mousePosition);
            }

            MouseDrag(Input.mousePosition);
        }

        private bool CheckMousePositionIsInsideWindow()
        {
            var mousePos = Input.mousePosition;
            return
                mousePos.x > 0 && mousePos.x < Screen.width &&
                mousePos.y > 0 && mousePos.y < Screen.height;
        }

        private void MouseWheel(float delta) 
            => transform.position += transform.forward * delta * wheelSpeed;

        private void MouseDrag(Vector3 mousePos)
        {
            Vector3 diff = mousePos - _preMousePos;
            if (diff.magnitude < Vector3.kEpsilon)
            {
                return;
            }

            if (IsTranslating())
            {
                transform.Translate(-diff * Time.deltaTime * moveSpeed);
            }
            else if (IsRotating())
            {
                CameraRotateAround(
                    new Vector2(-diff.y, diff.x) * rotateSpeed,
                    _rotateCenterInThisAngleMove
                    );
            }

            _preMousePos = mousePos;
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

        private bool CheckRotateStart()
        {
            if (Input.GetMouseButtonDown(RightMouseButton))
            {
                return true;
            }
            
            if (Input.GetMouseButtonDown(LeftMouseButton) && GetAltKey() || 
                Input.GetMouseButton(LeftMouseButton) && GetAltKeyDown())
            {
                return true;
            }

            return false;
        }

        private bool IsTranslating()
        {
            // 2つ目はマウスのないノートPC環境のための代替的なオプション
            return Input.GetMouseButton(MiddleMouseButton) || 
                Input.GetMouseButton(LeftMouseButton) && GetShiftKey();
        }
        
        private bool IsRotating()
        {
            // 2つ目はマウスのないノートPC環境のための代替的なオプション
            return Input.GetMouseButton(RightMouseButton) ||
                Input.GetMouseButton(LeftMouseButton) && GetAltKey();
        }

        private bool GetShiftKey()
        {
#if UNITY_EDITOR
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
            return _shift;
#endif
        }
        
        private bool GetAltKey()
        {
#if UNITY_EDITOR
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
#else
            return _alt;
#endif
        }

        private bool GetAltKeyDown()
        {
#if UNITY_EDITOR
            return Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
#else
            return _altDown;
#endif
        }
    }
}
