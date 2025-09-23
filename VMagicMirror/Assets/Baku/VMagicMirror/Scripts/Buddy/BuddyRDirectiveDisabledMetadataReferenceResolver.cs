using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// #r の実行を一律で禁止するようなResolver実装
    /// </summary>
    public class BuddyRDirectiveDisabledMetadataReferenceResolver : MetadataReferenceResolver
    {
        private static MetadataReferenceResolver Resolver => ScriptOptions.Default.MetadataResolver;
            
        public static BuddyRDirectiveDisabledMetadataReferenceResolver Instance { get; }= new();
        
        // 同じ型でさえあれば等価扱いしとく
        public override bool Equals(object other)
            => other is BuddyRDirectiveDisabledMetadataReferenceResolver;
        public override int GetHashCode()
            => typeof(BuddyRDirectiveDisabledMetadataReferenceResolver).GetHashCode();
        
        public override ImmutableArray<PortableExecutableReference> ResolveReference(
            string reference, string baseFilePath, MetadataReferenceProperties properties
            )
        {
            // ここが例外スローであることで一律禁止になる
            throw new InvalidOperationException("#r directive is not allowed in Buddy scripts.");
        }
        
        // MissingAssemblyの解決 = WithReferences() で明示的に導入するアセンブリ解決については、デフォルト実装に乗っかる
        public override bool ResolveMissingAssemblies => Resolver.ResolveMissingAssemblies;

        public override PortableExecutableReference ResolveMissingAssembly(
            MetadataReference definition, AssemblyIdentity referenceIdentity
            ) 
            => Resolver.ResolveMissingAssembly(definition, referenceIdentity);
    }
}
