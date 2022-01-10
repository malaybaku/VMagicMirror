using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Baku.VMagicMirror
{
    public enum AccessoryType
    {
        Unknown,
        Png,
        Glb,
        Gltf,
        //連番png: FPSは固定なことに注意
        NumberedPng,
        //NOTE: 理想を言うと、これ以外でもanimated gifとかパーティクル的なのも読み込みたい可能性がある
    }

    // Expected Folder Structure Example:
    // gltfは1フォルダ=1アイテム
    //
    // Accessory 
    // - item1.png
    // - item2.glb
    // - item3
    //   - item3.gltf
    //   - data.bin
    //   - textures
    //     - albedo.png
    // - item4
    //  - 000.png
    //  - 001.png
    //  - ...
    //  - 070.png

    /// <summary>
    /// VMagicMirrorの起動時などに一括でロードされた、対象フォルダに含まれるアクセサリ1つぶんの情報のうち、
    /// ファイル自体から取得できる情報。
    /// VMMのなかの空間配置とかスケーリングの情報は含まない。また、バイナリのvalidityもこの時点ではチェックしない。
    /// </summary>
    public class AccessoryFile
    {
        public const string FolderIdSuffix = ">";
        
        //NOTE: ちょっと冗長だが、フォルダパスもファイルパスもフルパスで指定する。
        public AccessoryFile(string folderPath, AccessoryType type, byte[][] binaries)
            : this("", folderPath, type, Array.Empty<byte>(), binaries)
        {
        }

        public AccessoryFile(string filePath, AccessoryType type, byte[] bytes, string folderPath = "")
            : this(filePath, folderPath, type, bytes, Array.Empty<byte[]>())
        {
        }
        
        private AccessoryFile(string path, string folderPath, AccessoryType type, byte[] bytes, byte[][] binaries)
        {
            FilePath = path;
            if (string.IsNullOrEmpty(folderPath))
            {
                IsFolder = false;
                FileId = Path.GetFileName(FilePath);
            }
            else
            {
                IsFolder = true;
                FileId = Path.GetFileName(folderPath) + FolderIdSuffix;
            }
            Type = type;
            Bytes = bytes;
            Binaries = binaries;
        }        

        public string FilePath { get; }
        
        //NOTE: 今のところ連番pngでのみ使う。
        public byte[][] Binaries { get; }
        
        //NOTE: いまはフォルダパスが不要だから省いているが、プロパティとして保持してもよい
        
        /// <summary>
        /// Accessoryフォルダ以下のファイル名またはフォルダ名によって指す識別子で、
        /// ファイルの場合はファイル名そのもの、
        /// フォルダを指す場合はフォルダ名+">"、というフォーマットの文字列。
        /// </summary>
        /// <remarks>
        /// フォルダの場合の末尾を"/"で区切っても良いのだけど、非Win環境に移植する場合かえって面倒な気がするので、
        /// フォルダ区切り文字ではないものを明示的に選んでます
        /// </remarks>
        public string FileId { get; }
        public bool IsFolder { get; }
        
        //NOTE: 連番画像のような複数ファイルデータを扱う様になった場合、
        //byte[][]みたいなデータ構造に変えてもよい
        public byte[] Bytes { get; }
        public AccessoryType Type { get; }
        
        /// <summary>
        /// アプリの起動方法によって定まる検索先フォルダから、モデル、画像等を一括でロードします。
        /// </summary>
        /// <returns></returns>
        public static AccessoryFile[] LoadAccessoryFiles()
        {
            var dir = SpecialFiles.AccessoryDirectory;
            if (!Directory.Exists(dir))
            {
                return Array.Empty<AccessoryFile>();
            }

            var result = new List<AccessoryFile>();
            //NOTE: 将来的に連番画像とかを扱いたい場合、GetFilesではなくGetDirectoriesも呼ぶ事になるはず
            foreach (var file in Directory.GetFiles(dir))
            {
                //素朴に拡張子を信じる
                var fileType = Path.GetExtension(file) switch
                {
                    ".png" => AccessoryType.Png,
                    ".glb" => AccessoryType.Glb,
                    // ".gltf" => AccessoryType.Gltf,
                    _ => AccessoryType.Unknown,
                };

                if (fileType == AccessoryType.Unknown)
                {
                    continue;
                }
                
                var bytes = File.ReadAllBytes(file);
                result.Add(new AccessoryFile(file, fileType, bytes));
            }

            foreach (var childDir in Directory.GetDirectories(dir))
            {
                var files = Directory.GetFiles(childDir);

                var gltfFiles = files.Where(f => Path.GetExtension(f) == ".gltf").ToArray();
                if (gltfFiles.Length == 1)
                {
                    var path = gltfFiles[0];
                    result.Add(new AccessoryFile(path, AccessoryType.Gltf, File.ReadAllBytes(path), childDir));
                }
                else if (files.Length > 0 && files.All(f => Path.GetExtension(f) == ".png"))
                {
                    var binaries = files.OrderBy(f => f).Select(File.ReadAllBytes).ToArray();
                    result.Add(new AccessoryFile(childDir, AccessoryType.NumberedPng, binaries));
                }
            }
            
            return result.ToArray();
        }
    }
}
