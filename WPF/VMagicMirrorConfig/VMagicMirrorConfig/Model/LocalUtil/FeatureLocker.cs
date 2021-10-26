namespace Baku.VMagicMirrorConfig
{
    internal static class FeatureLocker
    {
        public const bool FeatureLocked =
#if VMM_FEATURE_LOCKED
            true;
#else
            false;
#endif
    }
}
