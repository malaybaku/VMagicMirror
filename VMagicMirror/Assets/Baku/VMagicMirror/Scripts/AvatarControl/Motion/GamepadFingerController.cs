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
            _fingerPressButtonCounts[fingerNumber]++;
            fingerController.Hold(fingerNumber, GetBendingAngle(fingerNumber, true));
        }

        public void ButtonUp(GamepadKey key)
        {
            int fingerNumber = KeyToFingerNumber(key);
            _fingerPressButtonCounts[fingerNumber]--;
            if (_fingerPressButtonCounts[fingerNumber] < 0)
            {
                _fingerPressButtonCounts[fingerNumber] = 0;
            }
            
            fingerController.Hold(
                fingerNumber,
                GetBendingAngle(fingerNumber, _fingerPressButtonCounts[fingerNumber] > 0)
                );
        }

        public void LeftStick(Vector2 stickPos)
        {
            //とりあえず何もしない: 親指のIKした方がいい可能性はある
        }

        public void RightStick(Vector2 stickPos)
        {
            //とりあえず何もしない: 親指のIKした方がいい可能性はある
        }

        /// <summary>右手でコントローラを握ります。</summary>
        public void GripRightHand()
        {
            foreach (int fingerNumber in new[]
            {
                FingerConsts.RightThumb,
                FingerConsts.RightIndex,
                FingerConsts.RightMiddle,
                FingerConsts.RightRing,
                FingerConsts.RightLittle,
            })
            {
                fingerController.Hold(
                    fingerNumber,
                    GetBendingAngle(fingerNumber, _fingerPressButtonCounts[fingerNumber] > 0)
                );
            }
        }

        /// <summary>左手でコントローラを握ります。</summary>
        public void GripLeftHand()
        {
            foreach (int fingerNumber in new[]
            {
                FingerConsts.LeftThumb,
                FingerConsts.LeftIndex,
                FingerConsts.LeftMiddle,
                FingerConsts.LeftRing,
                FingerConsts.LeftLittle,
            })
            {
                fingerController.Hold(
                    fingerNumber,
                    GetBendingAngle(fingerNumber, _fingerPressButtonCounts[fingerNumber] > 0)
                );
            }
        }

        /// <summary>右手をコントローラから離します。</summary>
        public void ReleaseRightHand()
        {
            fingerController.Release(FingerConsts.RightThumb);
            fingerController.Release(FingerConsts.RightIndex);
            fingerController.Release(FingerConsts.RightMiddle);
            fingerController.Release(FingerConsts.RightRing);
            fingerController.Release(FingerConsts.RightLittle);
        }

        /// <summary>左手をコントローラから離します。</summary>
        public void ReleaseLeftHand()
        {
            fingerController.Release(FingerConsts.LeftThumb);
            fingerController.Release(FingerConsts.LeftIndex);
            fingerController.Release(FingerConsts.LeftMiddle);
            fingerController.Release(FingerConsts.LeftRing);
            fingerController.Release(FingerConsts.LeftLittle);
        }
        
        private static int KeyToFingerNumber(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.Start:
                case GamepadKey.A:
                case GamepadKey.B:
                case GamepadKey.X:
                case GamepadKey.Y:
                    return FingerConsts.RightThumb;
                case GamepadKey.Select:
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
        
        /// <summary>
        /// 指と、その指がボタンを押しているかどうかを用いて、指の望ましい曲げ角度を取得します。
        /// </summary>
        /// <param name="fingerNumber"></param>
        /// <param name="isPressed"></param>
        /// <returns></returns>
        private static float GetBendingAngle(int fingerNumber, bool isPressed)
        {
            //必要な場合分け:
            // - 親指, ひとさし-中指, 薬指-小指の区分
            // - 押す/押さない

            switch (fingerNumber)
            {
                case FingerConsts.LeftRing:
                case FingerConsts.LeftLittle:
                case FingerConsts.RightRing:
                case FingerConsts.RightLittle:
                    return 80f;
                case FingerConsts.RightIndex:
                case FingerConsts.RightMiddle:
                case FingerConsts.LeftIndex:
                case FingerConsts.LeftMiddle:
                    return isPressed ? 45f : 30f;
                case FingerConsts.LeftThumb:
                case FingerConsts.RightThumb:
                    return isPressed ? 45f: 30f;
                default:
                    return 25f;
            }
        }
    }
}
