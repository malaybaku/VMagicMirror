using System;
using UnityEngine;

namespace Baku.VMagicMirror.Mmf
{
    /// <summary> MemoryMappedFileのチャンネル名を管理するクラス。Editor/Runtimeで挙動が変わります。 </summary>
    public static class MmfChannelIdSource
    {
        private const string DefaultChannelName = "Baku.VMagicMirror";
        
        private static string _channelId = null;
        /// <summary> UnityおよびWPFで使用すべき、メモリマップトファイルの名前を取得します。</summary>
        public static string ChannelId => _channelId ??= CreateChannelId();

        private static string CreateChannelId()
        {
            if (Application.isEditor)
            {
                //NOTE: エディタではデフォルト名を使う。
                //このときProcess.StartでのWPF起動は行われない。
                //WPFは手動でダブルクリックで立ち上がるが、この場合はWPFもデフォルト名を想定してくるため、結果として接続が成立する。
                return DefaultChannelName;
            }
            else
            {
                //NOTE: 実実行ではワンタイムで文字列を作る。
                //これをWPF側も受け取って使ってくれるため、多重起動したときにIPCがぶつからなくなる。
                return DefaultChannelName + Guid.NewGuid().ToString();
            }
        }
    }
}
