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
            if (BuddySourceFolderRestrictionUtil.IsNgPath(Path.GetFullPath(resolvedPath)))
            {
                return new MemoryStream();
            }
            else
            {
                return _defaultResolver.OpenRead(resolvedPath);
            }
        }
    }
}