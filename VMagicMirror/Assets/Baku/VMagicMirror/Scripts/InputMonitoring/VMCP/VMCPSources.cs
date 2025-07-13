using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPSources
    {
        public VMCPSources(IEnumerable<VMCPSource> sources)
        {
            Sources = sources.ToArray();
        }

        public IReadOnlyList<VMCPSource> Sources { get; }

        public static VMCPSources Empty { get; } = new VMCPSources(Enumerable.Empty<VMCPSource>());
    }

    public readonly struct VMCPSource
    {
        public string Name { get; }
        public int Port { get; }
        public bool ReceiveHeadPose { get; }
        public bool ReceiveFacial { get; }
        public bool ReceiveHandPose { get; }

        public bool HasValidSetting() =>
            Port > 0 && Port < 65536 &&
            (ReceiveHeadPose || ReceiveFacial || ReceiveHandPose);

        public VMCPSource(
            string name, int port, 
            bool receiveHeadPose, bool receiveFacial, bool receiveHandPose)
        {
            Name = name;
            Port = port;
            ReceiveHeadPose = receiveHeadPose;
            ReceiveFacial = receiveFacial;
            ReceiveHandPose = receiveHandPose;
        }
    }

    /// <summary>
    /// VMCPSourceの付随情報で、「結局このソースから流していいデータはどれなのか」というのをメモっておくための構造体
    /// </summary>
    public readonly struct VMCPDataPassSettings
    {
        public bool ReceiveHeadPose { get; }
        public bool ReceiveFacial { get; }
        public bool ReceiveHandPose { get; }

        // TODO: ctorからちゃんとフラグを受け取って、手とは別の設定にする
        public bool ReceiveLowerBodyPose => ReceiveHandPose;

        public VMCPDataPassSettings(
            bool receiveHeadPose, bool receiveFacial, bool receiveHandPose)
        {
            ReceiveHeadPose = receiveHeadPose;
            ReceiveFacial = receiveFacial;
            ReceiveHandPose = receiveHandPose;
        }

        public static VMCPDataPassSettings Empty { get; } = new VMCPDataPassSettings(false, false, false);
    }
}