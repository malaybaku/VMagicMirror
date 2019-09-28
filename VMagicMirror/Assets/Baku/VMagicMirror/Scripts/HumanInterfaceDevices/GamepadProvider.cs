using System;
using System.Linq;
using UnityEngine;

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

            //NOTE: この二つは人差し指で押すボタン
            public Transform LeftShoulder;
            public Transform RightShoulder;

            //連続値が拾えるトリガーだが、便宜的にボタンと同じ扱いにする
            public Transform LeftTrigger;
            public Transform RightTrigger;
            
            public Transform[] GetTransforms() => new Transform[]
            {
                B, A, X, Y, Right, Down, Left, Up, 
            };
        }

        [SerializeField] private ButtonTransforms buttons;
        [SerializeField] private Transform leftStick = null;
        [SerializeField] private Transform rightStick = null;
        
        private void Start()
        {
            var buttonMat = HIDMaterialUtil.Instance.GetButtonMaterial();
            foreach(var r in buttons
                .GetTransforms()
                .Select(v => v.GetComponent<MeshRenderer>())
                )
            {
                r.material = buttonMat;
            }

            var stickAreaMat = HIDMaterialUtil.Instance.GetStickAreaMaterial();
            leftStick.GetComponent<MeshRenderer>().material = stickAreaMat;
            rightStick.GetComponent<MeshRenderer>().material = stickAreaMat;
        }

        public Vector3 GetButtonPosition(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.B:
                    return buttons.B.position;
                case GamepadKey.A:
                    return buttons.A.position;
                case GamepadKey.X:
                    return buttons.X.position;
                case GamepadKey.Y:
                    return buttons.Y.position;
                case GamepadKey.RIGHT:
                    return buttons.Right.position;
                case GamepadKey.DOWN:
                    return buttons.Down.position;
                case GamepadKey.LEFT:
                    return buttons.Left.position;
                case GamepadKey.UP:
                    return buttons.Up.position;
                case GamepadKey.RShoulder:
                    return buttons.RightShoulder.position;
                case GamepadKey.LShoulder:
                    return buttons.LeftShoulder.position;
                case GamepadKey.RTrigger:
                    return buttons.RightTrigger.position;
                case GamepadKey.LTrigger:
                    return buttons.LeftTrigger.position;
                default:
                    //来ないはず
                    return buttons.A.position;
            }

        }

        public static bool IsLeftHandPreferred(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.B:
                case GamepadKey.A:
                case GamepadKey.X:
                case GamepadKey.Y:
                case GamepadKey.RShoulder:
                case GamepadKey.RTrigger:
                    return false;
                default:
                    return true;
            }
        }

        public static ReactedHand GetPreferredReactionHand(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.B:
                case GamepadKey.A:
                case GamepadKey.X:
                case GamepadKey.Y:
                case GamepadKey.RShoulder:
                case GamepadKey.RTrigger:
                    return ReactedHand.Right;
                case GamepadKey.UP:
                case GamepadKey.RIGHT:
                case GamepadKey.DOWN:
                case GamepadKey.LEFT:
                case GamepadKey.LShoulder:
                case GamepadKey.LTrigger:
                    return ReactedHand.Left;
                default:
                    return ReactedHand.None;
            }
        }

        public static bool IsSideKey(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.RShoulder:
                case GamepadKey.RTrigger:
                case GamepadKey.LShoulder:
                case GamepadKey.LTrigger:
                    return true;
                default:
                    return false;
            }
        }
        
        //値を半分にする理由: Stick相当の位置にはQuadを置いており、一辺の長さが1に相当するので±0.5で押さえたい
        public Vector3 GetRightStickPosition(float x, float y) 
            => rightStick.TransformPoint(new Vector2(x * 0.5f, y * 0.5f));

        public Vector3 GetLeftStickPosition(float x, float y)
            => leftStick.TransformPoint(new Vector2(x * 0.5f, y * 0.5f));

    }
}
