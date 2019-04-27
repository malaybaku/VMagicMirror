using System;
using UnityEngine;
using XinputGamePad;
using mattatz.TransformControl;
using System.Linq;

namespace Baku.VMagicMirror
{
    public class GamepadProvider : MonoBehaviour
    {
        [Serializable]
        public struct ButtonTransforms
        {
            public Transform B;
            public Transform A;
            public Transform X;
            public Transform Y;

            public Transform Right;
            public Transform Down;
            public Transform Left;
            public Transform Up;

            public Transform[] GetTransforms() => new Transform[]
            {
                B, A, X, Y, Right, Down, Left, Up
            };
        }

        [SerializeField]
        ButtonTransforms buttons;

        [SerializeField]
        Transform leftStick;

        [SerializeField]
        Transform rightStick;

        private TransformControl _transformControl;

        private void Start()
        {
            //_transformControl = GetComponent<TransformControl>();
            var buttonMat = HIDMaterialUtil.Instance.GetButtonMaterial();
            foreach(var renderer in buttons
                .GetTransforms()
                .Select(v => v.GetComponent<MeshRenderer>())
                )
            {
                renderer.material = buttonMat;
            }

            var stickAreaMat = HIDMaterialUtil.Instance.GetStickAreaMaterial();
            leftStick.GetComponent<MeshRenderer>().material = stickAreaMat;
            rightStick.GetComponent<MeshRenderer>().material = stickAreaMat;
        }

        private void Update()
        {
            _transformControl?.Control();
        }

        public Vector3 GetButtonPosition(XinputKey key)
        {
            switch (key)
            {
                case XinputKey.B:
                    return buttons.B.position;
                case XinputKey.A:
                    return buttons.A.position;
                case XinputKey.X:
                    return buttons.X.position;
                case XinputKey.Y:
                    return buttons.Y.position;
                case XinputKey.RIGHT:
                    return buttons.Right.position;
                case XinputKey.DOWN:
                    return buttons.Down.position;
                case XinputKey.LEFT:
                    return buttons.Left.position;
                case XinputKey.UP:
                    return buttons.Up.position;
                default:
                    //来ないはず
                    return buttons.A.position;
            }

        }

        public bool IsLeftHandPreffered(XinputKey key)
        {
            switch (key)
            {
                case XinputKey.B:
                case XinputKey.A:
                case XinputKey.X:
                case XinputKey.Y:
                    return false;
                default:
                    return true;
            }
        }
        
        //値を半分にする理由: Stick相当の位置にはQuadを置いており、一辺の長さが1に相当するので±0.5で押さえたい
        public Vector3 GetRightStickPosition(float x, float y) 
            => rightStick.TransformPoint(new Vector2(x * 0.5f, y * 0.5f));

        public Vector3 GetLeftStickPosition(float x, float y)
            => leftStick.TransformPoint(new Vector2(x * 0.5f, y * 0.5f));

    }
}
