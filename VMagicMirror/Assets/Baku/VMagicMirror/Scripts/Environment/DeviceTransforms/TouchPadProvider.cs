﻿using Baku.VMagicMirror.Installer;
using mattatz.TransformControl;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class TouchPadProvider : MonoBehaviour
    {
        private MousePositionProvider _mousePositionProvider = null;

        [SerializeField] private TransformControl transformControl = null;
        public TransformControl TransformControl => transformControl;

        private TouchpadVisibilityView _visibilityView = null;
        public TouchpadVisibilityView GetVisibilityView()
        {
            if (_visibilityView == null)
            {
                _visibilityView = GetComponent<TouchpadVisibilityView>();
            }
            return _visibilityView;
        }

        public Pose GetPose() => new(transform.position, transform.rotation); 

        [Inject]
        public void Initialize(MousePositionProvider mousePositionProvider, IDevicesRoot parent)
        {
            _mousePositionProvider = mousePositionProvider;
            transform.parent = parent.Transform;
        }

        private void Start()
        {
            foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = HIDMaterialUtil.Instance.GetPadMaterial();
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Vector3 GetHandTipPosFromScreenPoint()
        {
            var cursorPosInVirtualScreen = _mousePositionProvider.NormalizedCursorPosition;
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
