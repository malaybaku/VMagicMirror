using System;
using System.Globalization;

namespace Baku.VMagicMirrorConfig
{
    public readonly struct BuddyId : IEquatable<BuddyId>
    {
        public string Value { get; }

        public BuddyId(string? value)
        {
            Value = value?.ToLower(CultureInfo.InvariantCulture) ?? "";
        }

        public override int GetHashCode() => Value.GetHashCode();
        public bool Equals(BuddyId other) => string.Equals(Value, other.Value, StringComparison.InvariantCultureIgnoreCase);
        public override bool Equals(object? obj) => obj is BuddyId id && Equals(id);
        public static BuddyId Create(string rawValue, bool isDefaultBuddy)
        {
            return new BuddyId(isDefaultBuddy ? ">" + rawValue : rawValue);
        }
    }
}
