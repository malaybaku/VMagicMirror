using System;
using System.Linq;

namespace Baku.VMagicMirror.VMCP
{
    [Serializable]
    public class SerializedVMCPSources
    {
        public SerializedVMCPSource[] Sources;
        
        public VMCPSources ToSources() => new VMCPSources(
            Sources.Select(s => s.ToSource())
        );
    }

    [Serializable]
    public class SerializedVMCPSource
    {
        public string Name;
        public int Port;
        public bool ReceiveHeadPose;
        public bool ReceiveFacial;
        public bool ReceiveHandPose;

        public VMCPSource ToSource() => new VMCPSource(
            Name, Port,
            ReceiveHeadPose, ReceiveFacial, ReceiveHandPose
        );
    }
}