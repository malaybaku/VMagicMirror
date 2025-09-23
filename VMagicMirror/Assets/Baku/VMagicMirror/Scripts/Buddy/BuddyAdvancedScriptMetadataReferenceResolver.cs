using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Baku.VMagicMirror.Buddy
{
    // NOTE: ビルトインのサブキャラ用のResolverに対しては本実装を使わないほうが良さそうなことに注意
    /// <summary>
    /// #r を実行してよいが、そのDLLの配置場所はBuddyフォルダ以下でなければならない…という制限を適用するようなResolver実装
    /// </summary>
    public class BuddyAdvancedScriptMetadataReferenceResolver : MetadataReferenceResolver
    {
        private static MetadataReferenceResolver Resolver => ScriptOptions.Default.MetadataResolver;
            
        public static BuddyAdvancedScriptMetadataReferenceResolver Instance { get; }= new();
        
        // 同じ型でさえあれば等価扱いしとく
        public override bool Equals(object other)
            => other is BuddyAdvancedScriptMetadataReferenceResolver;
        public override int GetHashCode()
            => typeof(BuddyAdvancedScriptMetadataReferenceResolver).GetHashCode();
        
        // #r が無視したいので明示的に塞ぐ
        public override ImmutableArray<PortableExecutableReference> ResolveReference(
            string reference, string baseFilePath, MetadataReferenceProperties properties
        )
        {
            var result = Resolver.ResolveReference(reference, baseFilePath, properties);

            foreach (var r in result)
            {
                // #r で指定されたDLLの配置場所がBuddyフォルダ以下でなければならない
                if (BuddySourceFolderRestrictionUtil.IsNgPath(r.FilePath))
                {
                    throw new InvalidOperationException(
                        $"Found .dll file which is out of Buddy Folder: {r.FilePath}. The file mus be placed under the Buddy folder."
                        );
                }
            }
            
            return result;
        }
        
        // MissingAssemblyの解決 = WithReferences() で明示的に導入するアセンブリ解決については、デフォルト実装に乗っかる
        public override bool ResolveMissingAssemblies => Resolver.ResolveMissingAssemblies;

        public override PortableExecutableReference ResolveMissingAssembly(
            MetadataReference definition, AssemblyIdentity referenceIdentity
        )
        {
            var result = Resolver.ResolveMissingAssembly(definition, referenceIdentity);

            if (result != null && BuddySourceFolderRestrictionUtil.IsNgPath(result.FilePath))
            {
                throw new InvalidOperationException(
                    $"Found .dll file which is out of Buddy Folder: {result.FilePath}. The file mus be placed under the Buddy folder."
                    );
            }
            
            return result;
        }
    }
}