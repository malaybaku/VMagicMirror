using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyScriptMetadataReferenceResolver : MetadataReferenceResolver
    {
        private static MetadataReferenceResolver Resolver => ScriptOptions.Default.MetadataResolver;
            
        public static BuddyScriptMetadataReferenceResolver Instance { get; }= new();
        
        // 同じ型でさえあれば等価扱いしとく
        public override bool Equals(object other)
            => other is BuddyScriptMetadataReferenceResolver;
        public override int GetHashCode()
            => typeof(BuddyScriptMetadataReferenceResolver).GetHashCode();
        
        // #r が無視したいので明示的に塞ぐ
        public override ImmutableArray<PortableExecutableReference> ResolveReference(
            string reference, string baseFilePath, MetadataReferenceProperties properties
            )
        {
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
