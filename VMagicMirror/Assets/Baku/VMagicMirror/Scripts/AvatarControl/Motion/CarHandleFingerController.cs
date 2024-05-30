using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// アケコン用の指制御をまとめたクラス。裏では<see cref="GamepadFingerController"/>と同様にFingerControllerを使う
    /// </summary>
    public class CarHandleFingerController
    {
        private FingerController _fingerController;

        [Inject]
        public void Initialize(FingerController fingerController)
        {
            _fingerController = fingerController;
        }
        
        /// <summary>左手を、ハンドルを握った状態の曲げにします。</summary>
        public void GripLeftHand()
        {
            for (int fingerNumber = FingerConsts.LeftThumb; fingerNumber < FingerConsts.LeftLittle + 1; fingerNumber++)
            {
                _fingerController.Hold(fingerNumber, GetBendingAngle(fingerNumber));
            }
        }

        /// <summary>右手を、ハンドルを握った状態の曲げにします。</summary>
        public void GripRightHand()
        {
            for (int fingerNumber = FingerConsts.RightThumb; fingerNumber < FingerConsts.RightLittle + 1; fingerNumber++)
            {
                _fingerController.Hold(fingerNumber, GetBendingAngle(fingerNumber));
            }
        }
        
        /// <summary>右手を離します。IKが切り替わる以外で、ハンドルの持ち替え動作時にも呼び出せます</summary>
        public void ReleaseRightHand()
        {
            _fingerController.Release(FingerConsts.RightThumb);
            _fingerController.Release(FingerConsts.RightIndex);
            _fingerController.Release(FingerConsts.RightMiddle);
            _fingerController.Release(FingerConsts.RightRing);
            _fingerController.Release(FingerConsts.RightLittle);
        }

        /// <summary>左手を離します。IKが切り替わる以外で、ハンドルの持ち替え動作時にも呼び出せます</summary>
        public void ReleaseLeftHand()
        {
            _fingerController.Release(FingerConsts.LeftThumb);
            _fingerController.Release(FingerConsts.LeftIndex);
            _fingerController.Release(FingerConsts.LeftMiddle);
            _fingerController.Release(FingerConsts.LeftRing);
            _fingerController.Release(FingerConsts.LeftLittle);
        }
        
        /// <summary>
        /// 指の種類に応じた、ハンドルを握っているときの曲げ角度を取得します。
        /// </summary>
        /// <param name="fingerNumber"></param>
        /// <returns></returns>
        private static float GetBendingAngle(int fingerNumber)
        {
            switch (fingerNumber)
            {
                case FingerConsts.LeftThumb:
                case FingerConsts.RightThumb:
                    return 25f;
                default:
                    return 40f;
            }
        }
    }
}
