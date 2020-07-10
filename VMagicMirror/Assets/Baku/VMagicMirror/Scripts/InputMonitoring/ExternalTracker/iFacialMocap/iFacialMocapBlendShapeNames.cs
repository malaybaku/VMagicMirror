
namespace Baku.VMagicMirror.ExternalTracker.iFacialMocap
{
    /// <summary>
    /// iFacialMocapでiOSから飛んでくるブレンドシェイプの名前。
    /// NOTE: 変数名はiOS標準の名前にしてあります
    /// </summary>
    public static class iFacialMocapBlendShapeNames 
    {
        //目
        public const string eyeBlinkLeft = "eyeBlink_L";
        public const string eyeLookUpLeft = "eyeLookUp_L";
        public const string eyeLookDownLeft = "eyeLookDown_L";
        public const string eyeLookInLeft = "eyeLookIn_L";
        public const string eyeLookOutLeft = "eyeLookOut_L";
        public const string eyeWideLeft = "eyeWide_L";
        public const string eyeSquintLeft = "eyeSquint_L";

        public const string eyeBlinkRight = "eyeBlink_R";
        public const string eyeLookUpRight = "eyeLookUp_R";
        public const string eyeLookDownRight = "eyeLookDown_R";
        public const string eyeLookInRight = "eyeLookIn_R";
        public const string eyeLookOutRight = "eyeLookOut_R";
        public const string eyeWideRight = "eyeWide_R";
        public const string eyeSquintRight = "eyeSquint_R";

        //口(多い)
        public const string mouthLeft = nameof(mouthLeft);
        public const string mouthSmileLeft = "mouthSmile_L";
        public const string mouthFrownLeft = "mouthFrown_L";
        public const string mouthPressLeft = "mouthPress_L";
        public const string mouthUpperUpLeft = "mouthUpperUp_L";
        public const string mouthLowerDownLeft = "mouthLowerDown_L";
        public const string mouthStretchLeft = "mouthStretch_L";
        public const string mouthDimpleLeft = "mouthDimple_L";

        public const string mouthRight = nameof(mouthRight);
        public const string mouthSmileRight = "mouthSmile_R";
        public const string mouthFrownRight = "mouthFrown_R";
        public const string mouthPressRight = "mouthPress_R";
        public const string mouthUpperUpRight = "mouthUpperUp_R";
        public const string mouthLowerDownRight = "mouthLowerDown_R";
        public const string mouthStretchRight = "mouthStretch_R";
        public const string mouthDimpleRight = "mouthDimple_R";
        
        public const string mouthClose = nameof(mouthClose);
        public const string mouthFunnel = nameof(mouthFunnel);
        public const string mouthPucker = nameof(mouthPucker);
        public const string mouthShrugUpper = nameof(mouthShrugUpper);
        public const string mouthShrugLower = nameof(mouthShrugLower);
        public const string mouthRollUpper = nameof(mouthRollUpper);
        public const string mouthRollLower = nameof(mouthRollLower);
        
        //あご
        public const string jawOpen = nameof(jawOpen);
        public const string jawForward = nameof(jawForward);
        public const string jawLeft = nameof(jawLeft);
        public const string jawRight = nameof(jawRight);
        
        //鼻
        public const string noseSneerLeft = "noseSneer_L";
        public const string noseSneerRight = "noseSneer_R";

        //ほお
        public const string cheekPuff = nameof(cheekPuff);
        public const string cheekSquintLeft = "cheekSquint_L";
        public const string cheekSquintRight = "cheekSquint_R";
        
        //舌
        public const string tongueOut = nameof(tongueOut);
        
        //まゆげ
        public const string browDownLeft = "browDown_L";
        public const string browOuterUpLeft = "browOuterUp_L";
        public const string browDownRight = "browDown_Right";
        public const string browOuterUpRight = "browOuterUp_R";
        public const string browInnerUp = nameof(browInnerUp);

    }
}
