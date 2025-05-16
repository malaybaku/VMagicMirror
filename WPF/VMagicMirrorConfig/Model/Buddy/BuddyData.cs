using System;
using System.Collections.Generic;

namespace Baku.VMagicMirrorConfig
{
    public class BuddyData
    {
        public BuddyData(BuddyMetadata metadata, IReadOnlyList<BuddyProperty> properties)
        {
            Metadata = metadata;
            Properties = properties;
            IsActive = new RProperty<bool>(false, v => IsActiveChanged?.Invoke(this, EventArgs.Empty));
        }

        public BuddyMetadata Metadata { get; }
        public IReadOnlyList<BuddyProperty> Properties { get; }
        public RProperty<bool> IsActive { get; } = new(false);

        // NOTE: このフラグは「開発者モードはonだけど、このScriptはデバッグモード無しで実行されてるよ(再起動したほうがいいよ)」と伝える目的で用いる。
        // - IsActiveがfalse > trueになった瞬間にBuddy自体の開発者モードがオフだったら、trueに切り替える
        // - IsActiveがfalseになるときは、つねにfalseになる
        public RProperty<bool> IsEnabledWithoutDeveloperMode { get; } = new(false);

        public event EventHandler<EventArgs>? IsActiveChanged;
    }

    public record BuddyProperty(BuddyPropertyMetadata Metadata, BuddyPropertyValue Value)
    {
        /// <summary> 
        /// Unity側の操作に由来してTransform2Dの値が更新されると発火する。
        /// ViewModelはこの値を受信したとき、IPCのメッセージを送信せずにUI表示だけ更新することが望ましい。
        /// 3Dも同様
        /// </summary>
        public event EventHandler<EventArgs>? Transform2DValueUpdated;
        public void NotifyTransform2DUpdated() => Transform2DValueUpdated?.Invoke(this, EventArgs.Empty);

        /// <summary> 
        /// Unity側の操作に由来してTransform2Dの値が更新されると発火する。
        /// ViewModelはこの値を受信したとき、IPCのメッセージを送信せずにUI表示だけ更新することが望ましい
        /// </summary>
        public event EventHandler<EventArgs>? Transform3DValueUpdated;
        public void NotifyTransform3DUpdated() => Transform3DValueUpdated?.Invoke(this, EventArgs.Empty);

    }
}
