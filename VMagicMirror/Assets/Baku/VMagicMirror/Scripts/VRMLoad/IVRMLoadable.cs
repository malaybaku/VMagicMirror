using System;

namespace Baku.VMagicMirror
{
    /// <summary> VRMロード元が提供すべきイベントの定義 </summary>
    public interface IVRMLoadable
    {
        event Action<VrmLoadedInfo> VrmLoaded;
        event Action VrmDisposing;
    }
}
