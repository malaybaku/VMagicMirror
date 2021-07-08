namespace Baku.VMagicMirror
{
    /// <summary>
    /// 一部機能の起動に制限をかけるクラス。
    /// </summary>
    /// <remarks>
    /// ※この実装は有償機能のロックとしては脇が甘いので参考にしないようにして下さい…
    /// </remarks>
    internal static class FeatureLocker
    {
        /// <summary>
        /// このビルドで機能制限が掛かっているかどうかを取得します。
        /// </summary>
        /// <remarks>
        /// ここでだけifdefを使って他の全箇所を作法のいいC# にキープします
        /// </remarks>
        public const bool IsFeatureLocked = 
#if VMM_FEATURE_LOCKED
                true;
#else
                false;
#endif
    }
}
