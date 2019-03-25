using System;
using UnityEngine;
using XinputGamePad;

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
        }

        [SerializeField]
        ButtonTransforms buttons;

        [SerializeField]
        Transform leftStick;

        [SerializeField]
        Transform rightStick;

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

        public Vector3 GetRightStickPosition(float x, float y) 
            => rightStick.TransformPoint(new Vector2(x, y));

        public Vector3 GetLeftStickPosition(float x, float y)
            => leftStick.TransformPoint(new Vector2(x, y));

    }
}
