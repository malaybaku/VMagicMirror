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
        private static readonly string IgnoredPath 
            = $@"..\{SpecialFiles.BuddyReferenceDataDirectoryName}\{SpecialFiles.BuddyReferenceDataGlobalScriptName}";

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
                Debug.Log($"Open Read Path, resolved path = {resolvedPath}, replace by empty stream");
                return new MemoryStream();
            }
            else
            {
                Debug.Log($"Open Read Path, resolved path = {resolvedPath}, use normal stream");
                return _defaultResolver.OpenRead(resolvedPath);
            }
        }

        private static bool IsNgPath(string fullPath)
        {
            var ngFilePath = Path.GetFullPath(SpecialFiles.BuddyReferenceDataGlobalScriptPath);
            return fullPath.Equals(ngFilePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}