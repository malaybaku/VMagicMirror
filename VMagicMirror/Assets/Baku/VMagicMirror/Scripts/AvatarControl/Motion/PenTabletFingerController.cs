namespace Baku.VMagicMirror
{
    /// <summary>
    /// ペンタブモードでの手の制御
    /// </summary>
    public class PenTabletFingerController
    {
        public PenTabletFingerController(FingerController fingerController)
        {
            _fingerController = fingerController;
        }
        
        private readonly FingerController _fingerController;
        
        /// <summary>右手を握ります。</summary>
        public void GripRightHand()
        {
            _fingerController.Hold(FingerConsts.RightThumb, 40f);
            _fingerController.Hold(FingerConsts.RightIndex, 50f);
            _fingerController.Hold(FingerConsts.RightMiddle, 60f);
            _fingerController.Hold(FingerConsts.RightRing, 80f);
            _fingerController.Hold(FingerConsts.RightLittle, 90f);
        }
        
        /// <summary>右手を離します。</summary>
        public void ReleaseRightHand()
        {
            _fingerController.Release(FingerConsts.RightThumb);
            _fingerController.Release(FingerConsts.RightIndex);
            _fingerController.Release(FingerConsts.RightMiddle);
            _fingerController.Release(FingerConsts.RightRing);
            _fingerController.Release(FingerConsts.RightLittle);
        }
    }
}
