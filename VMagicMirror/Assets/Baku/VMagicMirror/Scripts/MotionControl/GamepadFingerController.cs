using UnityEngine;

namespace Baku.VMagicMirror
{
    public class GamepadFingerController : MonoBehaviour
    {
        [SerializeField] private FingerController fingerController = null;

        //どの指が何個のキーを押しているか、という値の一覧。親指の同時押しとかをちゃんとモニタリングするために使う
        private readonly int[] _fingerPressButtonCounts = new int[10];
        //NOTE: 親指のスティック操作に対して何が出来るか考えないといけないんですよね…

        public void ButtonDown(GamepadKey key)
        {
            int fingerNumber = KeyToFingerNumber(key);
            _fingerPressButtonCounts[fingerNumber - 1]++;
            fingerController.PressAndHold(fingerNumber);
        }

        public void ButtonUp(GamepadKey key)
        {
            int fingerNumber = KeyToFingerNumber(key);
            _fingerPressButtonCounts[fingerNumber - 1]--;
            if (_fingerPressButtonCounts[fingerNumber - 1] == 0)
            {
                fingerController.ReleaseHoldedFinger(fingerNumber);
            }
        }

        public void LeftStick(Vector2 stickPos)
        {
            //とりあえず何もしない: 親指のIKした方がいい可能性はある
        }

        public void RightStick(Vector2 stickPos)
        {
            //とりあえず何もしない: 親指のIKした方がいい可能性はある
        }

        private static int KeyToFingerNumber(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.A:
                case GamepadKey.B:
                case GamepadKey.X:
                case GamepadKey.Y:
                    return FingerConsts.RightThumb;
                case GamepadKey.UP:
                case GamepadKey.RIGHT:
                case GamepadKey.LEFT:
                case GamepadKey.DOWN:
                    return FingerConsts.LeftThumb;
                case GamepadKey.RShoulder:
                    return FingerConsts.RightIndex;
                case GamepadKey.RTrigger:
                    return FingerConsts.RightMiddle;
                case GamepadKey.LShoulder:
                    return FingerConsts.LeftIndex;
                case GamepadKey.LTrigger:
                    return FingerConsts.LeftMiddle;
                default:
                    return FingerConsts.RightThumb;
            }
        }
        
    }
}
