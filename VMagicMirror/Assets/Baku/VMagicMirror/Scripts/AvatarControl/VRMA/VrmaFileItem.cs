using System;
using System.IO;

namespace Baku.VMagicMirror
{
    public readonly struct VrmaFileItem : IEquatable<VrmaFileItem>
    {
        public VrmaFileItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            FileNameWithExtension = Path.GetFileNameWithoutExtension(filePath);
        }

        public string FileName { get; }
        public string FileNameWithExtension { get; }
        public string FilePath { get; }

        public bool IsValid => !string.IsNullOrEmpty(FileName);
        
        public bool Equals(VrmaFileItem other) => FilePath == other.FilePath;
        public override bool Equals(object obj) => obj is VrmaFileItem other && Equals(other);
        public override int GetHashCode() => (FilePath != null ? FilePath.GetHashCode() : 0);
    }
}
