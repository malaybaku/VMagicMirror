using System;
using System.IO;

namespace Baku.VMagicMirror
{
    public readonly struct VrmaFileItem : IEquatable<VrmaFileItem>
    {
        public VrmaFileItem(string filePath, bool loop)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            FileNameWithExtension = Path.GetFileNameWithoutExtension(filePath);
            Loop = loop;
        }

        public string FileName { get; }
        public string FileNameWithExtension { get; }
        public string FilePath { get; }
        /// <summary> ループ想定のモーションを置いてるフォルダから読み出したファイルに対してtrueになる </summary>
        public bool Loop { get; }

        public bool IsValid => !string.IsNullOrEmpty(FileName);
        
        public bool Equals(VrmaFileItem other) => FilePath == other.FilePath;
        public override bool Equals(object obj) => obj is VrmaFileItem other && Equals(other);
        public override int GetHashCode() => (FilePath != null ? FilePath.GetHashCode() : 0);
    }
}
