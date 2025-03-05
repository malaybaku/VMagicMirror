namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// パーフェクトシンク機能で参照するアバターのブレンドシェイプ名です。
    /// </summary>
    /// <remarks>
    /// 個別のブレンドシェイプの動作についてはこのドキュメントでは詳解しません。
    /// 詳しくはパーフェクトシンクに関連したウェブ上の情報や、ARKitのARFaceAnchor.BlendShapeLocationのドキュメントなどを参照して下さい。
    /// </remarks>
    /// <seealso href="https://developer.apple.com/documentation/arkit/arfaceanchor/blendshapelocation">ARFaceAnchor.BlendShapeLocation</seealso>
    public static class PerfectSyncBlendShapeNames
    {
        // 目
        public static string EyeBlinkLeft { get; } = nameof(EyeBlinkLeft);
        public static string EyeLookUpLeft { get; } = nameof(EyeLookUpLeft);
        public static string EyeLookDownLeft { get; } = nameof(EyeLookDownLeft);
        public static string EyeLookInLeft { get; } = nameof(EyeLookInLeft);
        public static string EyeLookOutLeft { get; } = nameof(EyeLookOutLeft);
        public static string EyeWideLeft { get; } = nameof(EyeWideLeft);
        public static string EyeSquintLeft { get; } = nameof(EyeSquintLeft);

        public static string EyeBlinkRight { get; } = nameof(EyeBlinkRight);
        public static string EyeLookUpRight { get; } = nameof(EyeLookUpRight);
        public static string EyeLookDownRight { get; } = nameof(EyeLookDownRight);
        public static string EyeLookInRight { get; } = nameof(EyeLookInRight);
        public static string EyeLookOutRight { get; } = nameof(EyeLookOutRight);
        public static string EyeWideRight { get; } = nameof(EyeWideRight);
        public static string EyeSquintRight { get; } = nameof(EyeSquintRight);

        // 口
        public static string MouthLeft { get; } = nameof(MouthLeft);
        public static string MouthSmileLeft { get; } = nameof(MouthSmileLeft);
        public static string MouthFrownLeft { get; } = nameof(MouthFrownLeft);
        public static string MouthPressLeft { get; } = nameof(MouthPressLeft);
        public static string MouthUpperUpLeft { get; } = nameof(MouthUpperUpLeft);
        public static string MouthLowerDownLeft { get; } = nameof(MouthLowerDownLeft);
        public static string MouthStretchLeft { get; } = nameof(MouthStretchLeft);
        public static string MouthDimpleLeft { get; } = nameof(MouthDimpleLeft);

        public static string MouthRight { get; } = nameof(MouthRight);
        public static string MouthSmileRight { get; } = nameof(MouthSmileRight);
        public static string MouthFrownRight { get; } = nameof(MouthFrownRight);
        public static string MouthPressRight { get; } = nameof(MouthPressRight);
        public static string MouthUpperUpRight { get; } = nameof(MouthUpperUpRight);
        public static string MouthLowerDownRight { get; } = nameof(MouthLowerDownRight);
        public static string MouthStretchRight { get; } = nameof(MouthStretchRight);
        public static string MouthDimpleRight { get; } = nameof(MouthDimpleRight);

        public static string MouthClose { get; } = nameof(MouthClose);
        public static string MouthFunnel { get; } = nameof(MouthFunnel);
        public static string MouthPucker { get; } = nameof(MouthPucker);
        public static string MouthShrugUpper { get; } = nameof(MouthShrugUpper);
        public static string MouthShrugLower { get; } = nameof(MouthShrugLower);
        public static string MouthRollUpper { get; } = nameof(MouthRollUpper);
        public static string MouthRollLower { get; } = nameof(MouthRollLower);

        // あご
        public static string JawOpen { get; } = nameof(JawOpen);
        public static string JawForward { get; } = nameof(JawForward);
        public static string JawLeft { get; } = nameof(JawLeft);
        public static string JawRight { get; } = nameof(JawRight);

        // 鼻
        public static string NoseSneerLeft { get; } = nameof(NoseSneerLeft);
        public static string NoseSneerRight { get; } = nameof(NoseSneerRight);

        // ほお
        public static string CheekPuff { get; } = nameof(CheekPuff);
        public static string CheekSquintLeft { get; } = nameof(CheekSquintLeft);
        public static string CheekSquintRight { get; } = nameof(CheekSquintRight);

        // 舌
        public static string TongueOut { get; } = nameof(TongueOut);

        // まゆげ
        public static string BrowDownLeft { get; } = nameof(BrowDownLeft);
        public static string BrowOuterUpLeft { get; } = nameof(BrowOuterUpLeft);
        public static string BrowDownRight { get; } = nameof(BrowDownRight);
        public static string BrowOuterUpRight { get; } = nameof(BrowOuterUpRight);
        public static string BrowInnerUp { get; } = nameof(BrowInnerUp);
    }
}