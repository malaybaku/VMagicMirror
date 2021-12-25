using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//NOTE: ほぼUnity側からのコピペだが、バイナリを読み込まないでいいのが最大の違い
namespace Baku.VMagicMirrorConfig
{
    public enum AccessoryType
    {
        Unknown,
        Png,
        Glb,
        Gltf,
        //NOTE: 理想を言うと、これ以外でもanimated gifとか連番pngとかパーティクル的なのも読み込みたい可能性がある
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

    /// <summary>
    /// VMagicMirrorの起動時などに一括でロードされた、対象フォルダに含まれるアクセサリ1つぶんの情報のうち、
    /// ファイル自体から取得できる情報。
    /// VMMのなかの空間配置とかスケーリングの情報は含まない。また、バイナリのvalidityもこの時点ではチェックしない。
    /// </summary>
    public class AccessoryFile
    {
        public const string FolderIdSuffix = ">";
        public const char FolderIdSuffixChar = '>';

        //NOTE: ちょっと冗長だが、フォルダパスもファイルパスもフルパスで指定する。
        public AccessoryFile(string filePath, AccessoryType type, string folderPath = "")
        {
            FilePath = filePath;
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
        }

        public string FilePath { get; }

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
        public AccessoryType Type { get; }

        /// <summary>
        /// 対象のフォルダからモデルや画像のファイル情報の一覧を取得します。
        /// </summary>
        /// <returns></returns>
        public static AccessoryFile[] LoadAccessoryFiles(string dir)
        {
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

                result.Add(new AccessoryFile(file, fileType));
            }

            foreach (var childDir in Directory.GetDirectories(dir))
            {
                if (!Directory.Exists(childDir))
                {
                    continue;
                }

                var files = Directory.GetFiles(childDir);
                var gltfFiles = files.Where(f => Path.GetExtension(f) == ".gltf").ToArray();
                if (gltfFiles.Length != 1)
                {
                    continue;
                }

                var path = gltfFiles[0];
                //TODO: ここ、gltfの並び順が必ず後ろになるのが直感に反する。クリティカルではないけど…
                result.Add(new AccessoryFile(path, AccessoryType.Gltf, childDir));
            }

            return result.ToArray();
        }

    }
}
