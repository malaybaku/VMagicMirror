using System;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    //TODO: BuddyId自体をValueObject型にしたうえでGetBuddyIdをファクトリメソッド的に提供するほうが ">" のprefixの扱いが凝集しそう？
    public static class BuddyIdUtil
    {
        public static string GetBuddyId(string dir)
        {
            // ポイント
            // - フォルダ名ベースなのを踏まえて小文字化しちゃう
            // - デフォルトサブキャラに対してはフォルダで使えない文字をprefixにして区別する。
            // このprefixについてはBuddyFolderでも使っているし、
            var folderName = Path.GetFileName(dir).ToLower();
            return IsChildOfStreamingAssets(dir) ? ">" + folderName : folderName;
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
