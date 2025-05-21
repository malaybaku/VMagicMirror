using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// フォルダ名、およびデフォルトサブキャラの場合は ">" のprefixがつくことで実行時にサブキャラを一意に特定できるような文字列
    /// </summary>
    public readonly struct BuddyId : IEquatable<BuddyId>
    {
        public BuddyId(string value)
        {
            // NOTE: Equalityをわかりやすくしたい && ベースがフォルダ名なので、大文字と小文字を区別しない
            Value = value?.ToLower(CultureInfo.InvariantCulture) ?? "";
        }
        
        public string Value { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(BuddyId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is BuddyId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        public static BuddyId Empty { get; } = new("");
    }

    public static class BuddyIdGenerator
    {
        public static BuddyId GetBuddyId(string dir)
        {
            // ポイント
            // - フォルダ名ベースなのを踏まえて小文字化しちゃう
            // - デフォルトサブキャラに対してはフォルダで使えない文字をprefixにして区別する。
            // このprefixについてはBuddyFolderでも使っているし、
            var folderName = Path.GetFileName(dir).ToLower();
            var value = IsChildOfStreamingAssets(dir) ? ">" + folderName : folderName;
            return new BuddyId(value);
        }
        
        private static bool IsChildOfStreamingAssets(string dir)
        {
            // NOTE: streamingAssetsのパス区切り文字が信用できないので明示的に揃える。
            // 引数のdirについては、WPFが指定した文字列だから \ で区切っているはず…という前提
            return dir.StartsWith(
                Application.streamingAssetsPath.Replace('/', '\\'),
                StringComparison.InvariantCultureIgnoreCase
            );
        }
    }
}
