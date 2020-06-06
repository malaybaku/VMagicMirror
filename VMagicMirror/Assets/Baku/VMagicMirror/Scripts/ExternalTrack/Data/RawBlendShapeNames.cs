namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// ARKitで規定されてるブレンドシェイプ名の一覧です。キャメルケースを採用しているのは本家APIに則っています。
    /// </summary>
    public static class RawBlendShapeNames
    {
        //目
        public const string eyeBlinkLeft = nameof(eyeBlinkLeft);
        public const string eyeLookUpLeft = nameof(eyeLookUpLeft);
        public const string eyeLookDownLeft = nameof(eyeLookDownLeft);
        public const string eyeLookInLeft = nameof(eyeLookInLeft);
        public const string eyeLookOutLeft = nameof(eyeLookOutLeft);
        public const string eyeWideLeft = nameof(eyeWideLeft);
        public const string eyeSquintLeft = nameof(eyeSquintLeft);

        public const string eyeBlinkRight = nameof(eyeBlinkRight);
        public const string eyeLookUpRight = nameof(eyeLookUpRight);
        public const string eyeLookDownRight = nameof(eyeLookDownRight);
        public const string eyeLookInRight = nameof(eyeLookInRight);
        public const string eyeLookOutRight = nameof(eyeLookOutRight);
        public const string eyeWideRight = nameof(eyeWideRight);
        public const string eyeSquintRight = nameof(eyeSquintRight);

        //あご
        public const string jawOpen = nameof(jawOpen);
        public const string jawForward = nameof(jawForward);
        public const string jawLeft = nameof(jawLeft);
        public const string jawRight = nameof(jawRight);

        //まゆげ
        public const string browDownLeft = nameof(browDownLeft);
        public const string browOuterUpLeft = nameof(browOuterUpLeft);
        public const string browDownRight = nameof(browDownRight);
        public const string browOuterUpRight = nameof(browOuterUpRight);
        public const string browInnerUp = nameof(browInnerUp);

        //口(多い)
        public const string mouthLeft = nameof(mouthLeft);
        public const string mouthSmileLeft = nameof(mouthSmileLeft);
        public const string mouthFrownLeft = nameof(mouthFrownLeft);
        public const string mouthPressLeft = nameof(mouthPressLeft);
        public const string mouthUpperUpLeft = nameof(mouthUpperUpLeft);
        public const string mouthLowerDownLeft = nameof(mouthLowerDownLeft);
        public const string mouthStretchLeft = nameof(mouthStretchLeft);
        public const string mouthDimpleLeft = nameof(mouthDimpleLeft);

        public const string mouthRight = nameof(mouthRight);
        public const string mouthSmileRight = nameof(mouthSmileRight);
        public const string mouthFrownRight = nameof(mouthFrownRight);
        public const string mouthPressRight = nameof(mouthPressRight);
        public const string mouthUpperUpRight = nameof(mouthUpperUpRight);
        public const string mouthLowerDownRight = nameof(mouthLowerDownRight);
        public const string mouthStretchRight = nameof(mouthStretchRight);
        public const string mouthDimpleRight = nameof(mouthDimpleRight);
        
        public const string mouthClose = nameof(mouthClose);
        public const string mouthFunnel = nameof(mouthFunnel);
        public const string mouthPucker = nameof(mouthPucker);
        public const string mouthShrugUpper = nameof(mouthShrugUpper);
        public const string mouthShrugLower = nameof(mouthShrugLower);
        public const string mouthRollUpper = nameof(mouthRollUpper);
        public const string mouthRollLower = nameof(mouthRollLower);

        //鼻
        public const string noseSneerLeft = nameof(noseSneerLeft);
        public const string noseSneerRight = nameof(noseSneerRight);

        //ほお
        public const string cheekPuff = nameof(cheekPuff);
        public const string cheekSquintLeft = nameof(cheekSquintLeft);
        public const string cheekSquintRight = nameof(cheekSquintRight);
        
        //舌
        public const string tongueOut = nameof(tongueOut);
    }
}
