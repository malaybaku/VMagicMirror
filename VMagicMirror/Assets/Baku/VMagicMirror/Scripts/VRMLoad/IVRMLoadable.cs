using System;

namespace Baku.VMagicMirror
{
    /// <summary> VRMロード元が提供すべきイベントの定義 </summary>
    public interface IVRMLoadable
    {
        /// <summary>VRMがロードされると呼び出されます。VrmLoadedより先に呼ばれます。</summary>
        event Action<VrmLoadedInfo> PreVrmLoaded;
        /// <summary>VRMがロードされると呼び出されます。</summary>
        event Action<VrmLoadedInfo> VrmLoaded;
        /// <summary>VRMをアンロードするときに呼び出されます。</summary>
        event Action VrmDisposing;
    }
}
