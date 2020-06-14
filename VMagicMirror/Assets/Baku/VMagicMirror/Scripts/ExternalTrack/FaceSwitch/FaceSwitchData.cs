using System;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// WPFから飛んでくる、FaceSwitchの設定一覧
    /// </summary>
    [Serializable]
    public class FaceSwitchSettings
    {
        public FaceSwitchItem[] items;
    }

    /// <summary>
    /// NOTE: sourceがthreshold以上ならclipNameを適用し、リップシンクをそのままにするかはkeepLipSyncで判断、という内容
    /// </summary>
    [Serializable]
    public class FaceSwitchItem
    {
        public string source;
        public int threshold;
        public string clipName;
        public bool keepLipSync;
    }
}
