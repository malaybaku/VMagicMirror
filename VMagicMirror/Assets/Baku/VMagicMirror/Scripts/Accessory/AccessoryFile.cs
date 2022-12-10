using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniGLTF;

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
    // gltf / numbered pngは1フォルダ=1アイテム
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
        public AccessoryFile(AccessoryType type, string filePath, string folderPath = "")
        {
            Type = type;
            FilePath = filePath;
            _folderPath = folderPath;
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
        }        

        public AccessoryType Type { get; }

        public string FilePath { get; }

        private readonly string _folderPath;
        
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
 
        //.png, .glbの本体、あるいは.gltfファイルで.gltfファイルそのもの。連番画像の場合はカラ 
        public byte[] Bytes { get; private set; } = Array.Empty<byte>();

        //連番画像の場合の連番バイナリ
        public byte[][] Binaries { get; private set; } = Array.Empty<byte[]>();
        
        //バイナリロードが試行済みかどうか。「ロードしようとしたけど駄目でバイナリが空」というケースを検出するためのフラグ
        public bool DataLoadTried { get; private set; }
        
        /// <summary>
        /// アクセサリの実態が必要になった時点で呼び出すことで、ファイルからバイナリを取得します。
        /// </summary>
        public void LoadBinary()
        {
            if (DataLoadTried)
            {
                return;
            }
            DataLoadTried = true;
            
            switch (Type)
            {
                case AccessoryType.Png:
                case AccessoryType.Glb:
                case AccessoryType.Gltf:
                    if (File.Exists(FilePath))
                    {
                        Bytes = File.ReadAllBytes(FilePath);
                    }
                    break;
                case AccessoryType.NumberedPng:
                    //念のため改めてチェック
                    if (Directory.Exists(_folderPath))
                    {
                        var files = Directory.GetFiles(_folderPath);
                        if (files.All(f => Path.GetExtension(f) == ".png"))
                        {
                            Binaries = files.OrderBy(f => f).Select(File.ReadAllBytes).ToArray();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 取得したバイナリを使い終わったあとで呼ぶことで、バイナリをGC対象にします。
        /// </summary>
        /// <remarks>
        /// 例えばpng1枚のアクセサリの場合、Textureインスタンスが生成できた時点で呼び出してよい
        /// </remarks>
        public void ReleaseBinary()
        {
            Bytes = Array.Empty<byte>();
            Binaries = Array.Empty<byte[]>();
        }
        
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
                
                result.Add(new AccessoryFile(fileType, file));
            }

            foreach (var childDir in Directory.GetDirectories(dir))
            {
                var files = Directory.GetFiles(childDir);

                var gltfFiles = files.Where(f => Path.GetExtension(f) == ".gltf").ToArray();
                if (gltfFiles.Length == 1)
                {
                    var path = gltfFiles[0];
                    result.Add(new AccessoryFile(AccessoryType.Gltf, path, childDir));
                }
                else if (files.Length > 0 && files.All(f => Path.GetExtension(f) == ".png"))
                {
                    var binaries = files.OrderBy(f => f).Select(File.ReadAllBytes).ToArray();
                    result.Add(new AccessoryFile(AccessoryType.NumberedPng, childDir, childDir));
                }
            }
            
            return result.ToArray();
        }
    }
}
