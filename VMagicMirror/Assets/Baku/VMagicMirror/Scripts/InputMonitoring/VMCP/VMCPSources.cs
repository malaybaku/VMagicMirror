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

        public static VMCPSources Empty { get; } = new(Enumerable.Empty<VMCPSource>());
    }

    public readonly struct VMCPSource
    {
        public string Name { get; }
        public int Port { get; }
        public bool ReceiveHeadPose { get; }
        public bool ReceiveHandPose { get; }
        public bool ReceiveLowerBodyPose { get; }
        public bool ReceiveFacial { get; }

        public bool HasValidSetting() =>
            Port > 0 && Port < 65536 &&
            (ReceiveHeadPose || ReceiveHandPose || ReceiveLowerBodyPose || ReceiveFacial);

        public VMCPSource(
            string name,
            int port, 
            bool receiveHeadPose,
            bool receiveHandPose,
            bool receiveLowerBodyPose,
            bool receiveFacial
            )
        {
            Name = name;
            Port = port;
            ReceiveHeadPose = receiveHeadPose;
            ReceiveHandPose = receiveHandPose;
            ReceiveLowerBodyPose = receiveLowerBodyPose;
            ReceiveFacial = receiveFacial;
        }
    }

    /// <summary>
    /// VMCPSourceの付随情報で、「結局このソースから流していいデータはどれなのか」というのをメモっておくための構造体
    /// </summary>
    public readonly struct VMCPDataPassSettings
    {
        public bool ReceiveHeadPose { get; }
        public bool ReceiveFacial { get; }
        public bool ReceiveLowerBodyPose { get; }
        public bool ReceiveHandPose { get; }

        public VMCPDataPassSettings(
            bool receiveHeadPose, bool receiveHandPose, bool receiveLowerBodyPose, bool receiveFacial)
        {
            ReceiveHeadPose = receiveHeadPose;
            ReceiveHandPose = receiveHandPose;
            ReceiveLowerBodyPose = receiveLowerBodyPose;
            ReceiveFacial = receiveFacial;
        }

        public static VMCPDataPassSettings Empty { get; } = new(false, false, false, false);
    }
}