using System;
using System.Linq;

namespace Baku.VMagicMirror.VMCP
{
    [Serializable]
    public class SerializedVMCPSources
    {
        public SerializedVMCPSource[] Sources;
        
        public VMCPSources ToSources() => new(Sources.Select(s => s.ToSource()));
    }

    [Serializable]
    public class SerializedVMCPSource
    {
        public string Name;
        public int Port;
        public bool ReceiveHeadPose;
        public bool ReceiveHandPose;
        public bool ReceiveLowerBodyPose;
        public bool ReceiveFacial;

        public VMCPSource ToSource() => new(
            Name,
            Port,
            ReceiveHeadPose,
            ReceiveHandPose,
            ReceiveLowerBodyPose,
            ReceiveFacial
        );
    }
}