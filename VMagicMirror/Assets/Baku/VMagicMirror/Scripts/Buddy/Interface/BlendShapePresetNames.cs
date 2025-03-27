namespace VMagicMirror.Buddy
{
    // NOTE: PerfectSync用のキーも網羅的に定義したほうが親切かも。ただし、その場合はクラスを分けた方が良さそう

    /// <summary>
    /// <see cref="IAvatarFacial"/>などで適用されることのあるブレンドシェイプ名のうち、
    /// VRM1.0 の標準として定義されるブレンドシェイプの名称を定義したクラスです。
    /// </summary>
    /// <remarks>
    /// VRM1.0 の標準以外のブレンドシェイプとしては、パーフェクトシンクに関するブレンドシェイプ名が <see cref="PerfectSyncBlendShapeNames"/> で定義されています。
    /// </remarks>
    public static class BlendShapePresetNames
    {
        public static string Happy { get; } = ToCamelCase(nameof(Happy));
        public static string Angry { get; } = ToCamelCase(nameof(Angry));
        public static string Sad { get; } = ToCamelCase(nameof(Sad));
        public static string Relaxed { get; } = ToCamelCase(nameof(Relaxed));
        public static string Surprised { get; } = ToCamelCase(nameof(Surprised));

        public static string Aa { get; } = ToCamelCase(nameof(Aa));
        public static string Ih { get; } = ToCamelCase(nameof(Ih));
        public static string Ou { get; } = ToCamelCase(nameof(Ou));
        public static string Ee { get; } = ToCamelCase(nameof(Ee));
        public static string Oh { get; } = ToCamelCase(nameof(Oh));
        
        public static string Blink { get; } = ToCamelCase(nameof(Blink));
        public static string BlinkLeft { get; } = ToCamelCase(nameof(BlinkLeft));
        public static string BlinkRight { get; } = ToCamelCase(nameof(BlinkRight));
        
        public static string LookUp { get; } = ToCamelCase(nameof(LookUp));
        public static string LookDown { get; } = ToCamelCase(nameof(LookDown));
        public static string LookLeft { get; } = ToCamelCase(nameof(LookLeft));
        public static string LookRight { get; } = ToCamelCase(nameof(LookRight));
        public static string Neutral { get; } = ToCamelCase(nameof(Neutral));

        // NOTE: BlinkLeft => blinkLeft とかの変換があるのでToLowerではダメ
        private static string ToCamelCase(string v) => char.ToLowerInvariant(v[0]) + v[1..];
    }
}
