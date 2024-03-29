﻿using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// アケコン用の指制御をまとめたクラス。裏では<see cref="GamepadFingerController"/>と同様にFingerControllerを使う
    /// </summary>
    public class ArcadeStickFingerController : MonoBehaviour
    {
        [SerializeField] private FingerController fingerController = null;

        //どの指が何個のキーを押しているか、という値の一覧。
        //右手だけ考慮するため、右指の番号(5-9)から5を引いた値が実際のインデックスになることに注意
        private readonly int[] _fingerPressButtonCounts = new int[5];
        
        public void ButtonDown(GamepadKey key)
        {
            int fingerNumber = KeyToFingerNumber(key);
            int index = fingerNumber - 5;
            if (index < 0 || index >= _fingerPressButtonCounts.Length)  
            {
                return;
            }
            
            _fingerPressButtonCounts[index]++;
            fingerController.Hold(fingerNumber, GetBendingAngle(fingerNumber, true));
        }

        public void ButtonUp(GamepadKey key)
        {
            int fingerNumber = KeyToFingerNumber(key);
            int index = fingerNumber - 5;
            if (index < 0 || index >= _fingerPressButtonCounts.Length)  
            {
                return;
            }
            
            _fingerPressButtonCounts[index]--;
            fingerController.Hold(
                fingerNumber,
                GetBendingAngle(fingerNumber, _fingerPressButtonCounts[index] > 0)
                );
        }
        
        /// <summary>左手を、スティックを握った状態にします。IKがアケコンに切り替わった直後に呼び出します。</summary>
        public void GripLeftHand()
        {
            for (int fingerNumber = FingerConsts.LeftThumb; fingerNumber < FingerConsts.LeftLittle + 1; fingerNumber++)
            {
                fingerController.Hold(fingerNumber, GetBendingAngle(fingerNumber, true));
            }
        }

        /// <summary>右手をボタン群の上にある状態にします。IKがアケコンに切り替わった直後に呼び出します。</summary>
        public void GripRightHand()
        {
            for (int fingerNumber = FingerConsts.RightThumb; fingerNumber < FingerConsts.RightLittle + 1; fingerNumber++)
            {
                fingerController.Hold(
                    fingerNumber,
                    GetBendingAngle(fingerNumber, _fingerPressButtonCounts[fingerNumber - 5] > 0)
                );
            }
        }
        
        /// <summary>右手を離します。IKがアケコン以外になるとき呼び出します。</summary>
        public void ReleaseRightHand()
        {
            fingerController.Release(FingerConsts.RightThumb);
            fingerController.Release(FingerConsts.RightIndex);
            fingerController.Release(FingerConsts.RightMiddle);
            fingerController.Release(FingerConsts.RightRing);
            fingerController.Release(FingerConsts.RightLittle);
        }

        /// <summary>左手を離します。IKがアケコン以外になるとき呼び出します。</summary>
        public void ReleaseLeftHand()
        {
            fingerController.Release(FingerConsts.LeftThumb);
            fingerController.Release(FingerConsts.LeftIndex);
            fingerController.Release(FingerConsts.LeftMiddle);
            fingerController.Release(FingerConsts.LeftRing);
            fingerController.Release(FingerConsts.LeftLittle);
        }
        
        public static int KeyToFingerNumber(GamepadKey key)
        {
            //NOTE: モデルの制御上は親指が扱いづらいんだけど、それはさておき現実っぽい振り分けにしておく
            switch (key)
            {
                case GamepadKey.A:
                case GamepadKey.B:
                    return FingerConsts.RightThumb;
                case GamepadKey.X:
                    return FingerConsts.RightIndex;
                case GamepadKey.Y:
                    return FingerConsts.RightMiddle;
                //RとLに注意: アーケードスティックはRが左側、Lが右側にある
                case GamepadKey.RShoulder:
                case GamepadKey.RTrigger:
                    return FingerConsts.RightRing;
                case GamepadKey.LShoulder:
                case GamepadKey.LTrigger:
                    return FingerConsts.RightLittle;
                default:
                    return -1;
            }
        }
        
        /// <summary>
        /// 指の種類と、その指でボタンを押しているかどうかを指定することで、指の曲げ角度を取得します。
        /// </summary>
        /// <param name="fingerNumber"></param>
        /// <param name="isPressed"></param>
        /// <returns></returns>
        private static float GetBendingAngle(int fingerNumber, bool isPressed)
        {
            //左手: スティック握りっぱなし
            //右手: ボタン押す/押さないで変化
            switch (fingerNumber)
            {
                case FingerConsts.LeftRing:
                    return 60f;
                case FingerConsts.LeftLittle:
                    return 80f;
                case FingerConsts.LeftIndex:
                case FingerConsts.LeftMiddle:
                case FingerConsts.LeftThumb:
                    return 50f;
                case FingerConsts.RightLittle:
                case FingerConsts.RightRing:
                case FingerConsts.RightIndex:
                case FingerConsts.RightMiddle:
                    return isPressed ? 30f : 20f;
                case FingerConsts.RightThumb:
                    //NOTE: 親指は最初から手のひら寄りに曲げておく。
                    return isPressed ? 40f : 30f;
                default:
                    //こない
                    return 25f;
            }
        }
    }
}
