using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// VMCPの受信設定の一覧
    /// </summary>
    /// <remarks>
    /// 順序に意味があることに注意。
    /// 複数ソースで同じ情報を受信しようとした場合、Indexが0寄りのソースのほうが優先的にて適用される
    /// </remarks>
    public class VMCPSources
    {
        public VMCPSources(IEnumerable<VMCPSource> sources)
        {
            Sources = sources.ToArray();
        }

        public IReadOnlyList<VMCPSource> Sources { get; }

        public static VMCPSources Default { get; } = new VMCPSources(new[]
        {
            new VMCPSource(),
            new VMCPSource(),
            new VMCPSource(),
        });
    }

    /// <summary>
    /// 単一のVMCPの受信設定に対応
    /// </summary>
    public class VMCPSource
    {
        public string Name { get; set; } = "";
        public int Port { get; set; } = 0;
        public bool ReceiveHeadPose { get; set; }
        public bool ReceiveFacial { get; set; }
        public bool ReceiveHandPose { get; set; }
    }
}
