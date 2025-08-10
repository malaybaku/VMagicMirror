using System;
using R3;

namespace Baku.VMagicMirror
{
    /// <summary> VRMロード元が提供すべきイベントの定義 </summary>
    public interface IVRMLoadable
    {
        /// <summary>VRMがロードされると呼び出されます。VrmLoadedより先に呼ばれます。</summary>
        event Action<VrmLoadedInfo> PreVrmLoaded;
        /// <summary>VRMがロードされると呼び出されます。</summary>
        event Action<VrmLoadedInfo> VrmLoaded;

        /// <summary>VRMがロードされると呼び出されます。VrmLoadedが完全に呼び終わったあとで呼ばれます。</summary>
        event Action<VrmLoadedInfo> PostVrmLoaded;
        
        /// <summary>VRMをアンロードするときに呼び出されます。</summary>
        event Action VrmDisposing;

        /// <summary> ローカルファイルのVRMのロード処理について、成否によらず処理が終わると呼ばれます。 </summary>
        public event Action LocalVrmLoadEnded;
        
        /// <summary> 現在のモデルがVRM 0.xなのかVRM 1.0なのかが分かるプロパティ </summary>
        IReadOnlyReactiveProperty<CurrentModelVersion> ModelVersion { get; }
    }
}
