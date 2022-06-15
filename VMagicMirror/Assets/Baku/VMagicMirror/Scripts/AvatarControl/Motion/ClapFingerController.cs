namespace Baku.VMagicMirror
{
    /// <summary>
    /// 拍手のときの指を制御するクラス。
    /// 定形姿勢を取るだけの実装だが、もっとフクザツにする可能性があるためクラスは分けている
    /// </summary>
    public class ClapFingerController 
    {
        public ClapFingerController(FingerController fingerController)
        {
            _fingerController = fingerController;
        }
        private readonly FingerController _fingerController;
        
        /// <summary> IKが拍手のステートになったときに呼ぶことで、拍手のとき用の指の曲げを適用します。 </summary>
        public void Enable()
        {
            _fingerController.Hold(FingerConsts.RightThumb, 10);
            _fingerController.Hold(FingerConsts.RightIndex, 15);
            _fingerController.Hold(FingerConsts.RightMiddle, 12);
            _fingerController.Hold(FingerConsts.RightRing, 9);
            _fingerController.Hold(FingerConsts.RightLittle, 5);

            _fingerController.Hold(FingerConsts.LeftThumb, 5);
            _fingerController.Hold(FingerConsts.LeftIndex, 5);
            _fingerController.Hold(FingerConsts.LeftMiddle, 10);
            _fingerController.Hold(FingerConsts.LeftRing, 12);
            _fingerController.Hold(FingerConsts.LeftLittle, 15);
        }

        /// <summary> IKが拍手のステートではなくなるときに呼ぶことで、指の状態を元に戻します。 </summary>
        public void Release()
        {
            _fingerController.Release(FingerConsts.RightThumb);
            _fingerController.Release(FingerConsts.RightIndex);
            _fingerController.Release(FingerConsts.RightMiddle);
            _fingerController.Release(FingerConsts.RightRing);
            _fingerController.Release(FingerConsts.RightLittle);

            _fingerController.Release(FingerConsts.LeftThumb);
            _fingerController.Release(FingerConsts.LeftIndex);
            _fingerController.Release(FingerConsts.LeftMiddle);
            _fingerController.Release(FingerConsts.LeftRing);
            _fingerController.Release(FingerConsts.LeftLittle);
        }
    }
}
