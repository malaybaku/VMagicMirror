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

        public event EventHandler<EventArgs>? IsActiveChanged;
    }

    public record BuddyProperty(BuddyPropertyMetadata Metadata, BuddyPropertyValue Value);
}
