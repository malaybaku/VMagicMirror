using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// Buddyのスクリプトで使用した #load ディレクティブのうち、
    /// VSCode上の編集補助だけのために定義したスクリプトのロードだけを明示的に無視してくれるすごいやつだよ
    /// </summary>
    public class IgnoreFileDefinedScriptSourceResolver : SourceReferenceResolver
    {
        private readonly SourceReferenceResolver _defaultResolver;
        
        private static IgnoreFileDefinedScriptSourceResolver _instance;
        public static IgnoreFileDefinedScriptSourceResolver Instance 
            => _instance ??= new IgnoreFileDefinedScriptSourceResolver(
                ScriptOptions.Default.SourceResolver ?? ScriptSourceResolver.Default
                ); 
        private IgnoreFileDefinedScriptSourceResolver(SourceReferenceResolver defaultResolver)
        {
            _defaultResolver = defaultResolver;
        }

        public override bool Equals(object other) => _defaultResolver.Equals(other);
        public override int GetHashCode() => _defaultResolver.GetHashCode();

        public override string NormalizePath(string path, string baseFilePath) 
            => _defaultResolver.NormalizePath(path, baseFilePath);

        public override string ResolveReference(string path, string baseFilePath)
            => _defaultResolver.ResolveReference(path, baseFilePath);

        public override Stream OpenRead(string resolvedPath)
        {
            if (IsNgPath(Path.GetFullPath(resolvedPath)))
            {
                return new MemoryStream();
            }
            else
            {
                return _defaultResolver.OpenRead(resolvedPath);
            }
        }

        private static bool IsNgPath(string fullPath)
        {
            var ngFilePath = Path.GetFullPath(SpecialFiles.BuddyReferenceDataGlobalScriptPath);
            if (fullPath.Equals(ngFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // NOTE: 明らかにサブキャラと関係ないフォルダを見に行こうとするのを全体的に禁止する。
            // この処置のpositiveな副作用として、デフォルトサブキャラ(VMM.exe付近のフォルダに存在)の #load は全て無視される
            if (!fullPath.StartsWith(SpecialFiles.BuddyRootDirectory))
            {
                return true;
            }
            
            // NOTEのNOTE: ひとつ手前の条件によって下記のガードが冗長になるので、省いている
            // NOTE: VMagicMirror.exeのあるフォルダ以下の #load を一律で禁止する。
            // 普通にスクリプトを書いて本処理が適用されるのはデフォルトサブキャラのみである
            // (※デフォルトサブキャラで #load が使いたくなったら書き方を考える必要がある)
            // var exeDir = Path.GetDirectoryName(Application.dataPath)?.Replace('/', '\\');
            // if (!string.IsNullOrEmpty(exeDir) && fullPath.StartsWith(exeDir, StringComparison.OrdinalIgnoreCase))
            // {
            //     return true;
            // }

            return false;
        }
    }
}