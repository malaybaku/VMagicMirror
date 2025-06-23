using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyObjectRaycastChecker : IInitializable
    {
        private readonly CameraUtilWrapper _camera;
        private readonly BuddySpriteCanvas _canvas;
        private readonly BuddyRuntimeObjectRepository _repository;

        private readonly List<RaycastResult> _raycastResults = new(8);
        private GraphicRaycaster _raycaster;

        [Inject]
        public BuddyObjectRaycastChecker(
            CameraUtilWrapper camera,
            BuddySpriteCanvas canvas,
            BuddyRuntimeObjectRepository repository
        )
        {
            _camera = camera;
            _canvas = canvas;
            _repository = repository;
        }

        void IInitializable.Initialize()
        {
            _raycaster = _canvas.GetComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// とくにウィンドウの背景が透過しているときに呼ぶことで、ポインターがサブキャラ関連のvisibleなオブジェクト上にあるかどうかを判定する。
        /// 呼び出し元ではメインアバターへのマウスオーバーと同様、この関数がtrueを返すときはクリックスルーを無効化することが望ましい
        /// </summary>
        /// <returns></returns>
        public bool IsPointerOnBuddyObject() => IsPointerOnBuddyObject(Input.mousePosition);

        // NOTE: 呼び出し元がInput.mousePositionを指定するような建付けにしてもOK
        private bool IsPointerOnBuddyObject(Vector2 mousePosition)
        {
            // そもそもポインターが画面外なら評価しない
            if (!_camera.PixelRectContains(mousePosition))
            {
                return false;
            }
            
            // visibleなオブジェクトがないケースも自明にガードする。
            // NOTE: 今のところ2Dオブジェクトだけケアしている。これは判定がGraphicRaycasterのぶんしかやってないことと一貫させている
            // TODO: 3Dオブジェクトもケアする (とくにGLB or VRMが入る段階では必須)
            if (!_repository.GetRepositories().Any(r => r.TalkTexts.Count > 0) &&
                !_repository.GetRepositories().Any(r => r.Sprite2Ds.Count > 0))
            {
                return false;
            }

            var pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = mousePosition
            };
            
            _raycastResults.Clear();
            _raycaster.Raycast(pointerEventData, _raycastResults);
            return _raycastResults.Count > 0;
        }    
    }
}
