using System;
using System.Collections.Generic;
using System.IO;

namespace Baku.VMagicMirror
{
    public enum AccessoryType
    {
        Unknown,
        Png,
        Glb,
        Gltf,
        //NOTE: 理想を言うと、これ以外でもanimated gifとか連番pngとかパーティクル的なのも読み込みたい可能性がある
    }
    
    /// <summary>
    /// VMagicMirrorの起動時などに一括でロードされた、対象フォルダに含まれるアクセサリ1つぶんの情報のうち、
    /// ファイル自体から取得できる情報。
    /// VMMのなかの空間配置とかスケーリングの情報は含まない。また、バイナリのvalidityもこの時点ではチェックしない。
    /// </summary>
    public class AccessoryFile
    {
        public AccessoryFile(string filePath, AccessoryType type, byte[] bytes)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Type = type;
            Bytes = bytes;
        }
        public string FilePath { get; }
        public string FileName { get; }
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
                    ".gltf" => AccessoryType.Gltf,
                    _ => AccessoryType.Unknown,
                };

                if (fileType == AccessoryType.Unknown)
                {
                    continue;
                }
                
                var bytes = File.ReadAllBytes(file);
                result.Add(new AccessoryFile(file, fileType, bytes));
            }

            return result.ToArray();
        }
        
    }
}
