using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// コードを読み取って、想定してないnamespaceへのアクセスがあると怒ってくるすごいやつだよ
    /// </summary>
    public static class CSharpScriptAnalyzer
    {
        private static readonly HashSet<string> NgNamespaces = new()
        {
            // == 禁止しないとかなり色々できちゃうやつ ==
            "System.IO",
            "System.Net",
            "System.Reflection",

            // == 禁止しないでもなんとかなるかもだが、塞いでおく
            "System.Diagnostics", // StackTraceをこの方法で触ってほしくない
            "System.Environment", // CurrentDirectoryとかCommandLineArgとかをあんまり公開したくない
            "System.AppDomain", // そもそも .NET Standard では使えないはずだけど一応
            "System.Resources", // この辺もI/Oが絡みそうなので一応
            "System.Configuration", // 同上
            
            // == 意思を持って許可する(のでコメントアウト) ==
            // "System.Threading", 
        };

        private static readonly HashSet<string> NgPrefixes;

        static CSharpScriptAnalyzer()
        {
            NgPrefixes = new HashSet<string>(
                NgNamespaces.Select(v => v + ".")
            );
        }

        /// <summary>
        /// コンパイル前に見つけられる自明な違反として以下を検出する
        /// - #r ディレクティブの使用
        /// - using で怪しいものを使っている
        /// </summary>
        /// <param name="code"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<AnalyzerResult> AnalyzeCodeAsync(string code, CancellationToken ct)
        {
            // スクリプトを構文解析
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = await syntaxTree.GetRootAsync(ct);

            // #r ディレクティブは内容によらずNG
            var hasReferenceDirectives = root.DescendantNodes()
                .OfType<ReferenceDirectiveTriviaSyntax>()
                .Any();
            if (hasReferenceDirectives)
            {
                return new AnalyzerResult(
                    syntaxTree,
                    CSharpScriptAnalyzerResults.ReferenceDirectiveExists,
                    "#r directive is not allowed."
                    );
            }
            
            // NGリストのnamespaceを使ってたらNG
            // usingのほうが検出が trivial に実施できるので、まずはusingをチェックしていく
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name!.ToString());
            if (usingDirectives.Any(IsInvalidNamespace))
            {
                return new AnalyzerResult(
                    syntaxTree,
                    CSharpScriptAnalyzerResults.ForbiddenUsingStatement,
                    "Forbidden namespace detected. See detail at developer doc.s"
                );
            }

            // NOTE: これ以上の解析はCompilationがないとムズいので、一旦通す
            return AnalyzerResult.Success(syntaxTree);
        }

        /// <summary>
        /// コンパイル後の情報に基づいて違反を検出する
        /// </summary>
        /// <param name="syntaxTree"></param>
        /// <param name="compilation"></param>
        /// <returns></returns>
        public static AnalyzerResult AnalyzeCompileResultAsync(SyntaxTree syntaxTree, Compilation compilation)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();
            // 要はExpression全般でnamespaceにNGリストを適用したいので、ガッとConcatしてしまえばよい
            var forbiddenNamespaceAccessExists = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>().Cast<ExpressionSyntax>()
                .Concat(root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                .Concat(root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
                .Any(expr => 
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(expr);
                    return UsesInvalidNamespace(symbolInfo);
                });
            if (forbiddenNamespaceAccessExists)
            {
                return new AnalyzerResult(
                    syntaxTree,
                    CSharpScriptAnalyzerResults.ForbiddenNamespaceAccess,
                    "Forbidden namespace access detected. See detail at developer doc."
                    );
            }

            return AnalyzerResult.Success(syntaxTree);
        }

        private static bool UsesInvalidNamespace(SymbolInfo symbolInfo)
        {
            var symbol = symbolInfo.Symbol;
            if (symbol == null)
            {
                return false;
            }

            var containingNamespace = symbol.ContainingNamespace?.ToDisplayString();
            return containingNamespace != null && IsInvalidNamespace(containingNamespace);
        }

        private static bool IsInvalidNamespace(string value) 
            => NgNamespaces.Contains(value) || NgPrefixes.Any(value.StartsWith);
    }

    public readonly struct AnalyzerResult
    {
        /// <summary>
        /// NOTE: Compilationを含まない解析を行い、それがSuccessした場合は必ず有効な値が入る。
        /// それ以外の場合は値が有効なことは保証されない
        /// </summary>
        public SyntaxTree SyntaxTree { get; }
        public CSharpScriptAnalyzerResults Result { get; }
        public string Message { get; }

        public bool HasError => Result is not CSharpScriptAnalyzerResults.Success;
        
        public AnalyzerResult(SyntaxTree syntaxTree, CSharpScriptAnalyzerResults result, string message)
        {
            SyntaxTree = syntaxTree;
            Result = result;
            Message = message;
        }

        public static AnalyzerResult Success(SyntaxTree tree) => new(tree, CSharpScriptAnalyzerResults.Success, "");
    }

    /// <summary>
    /// <see cref="CSharpScriptAnalyzer"/> によるスクリプトの解析結果のパターン
    /// </summary>
    public enum CSharpScriptAnalyzerResults
    {
        /// <summary> 有効 </summary>
        Success,
        /// <summary> 使ってはいけない想定のnamespaceをusingしている </summary>
        ForbiddenUsingStatement,
        /// <summary> usingはしていないが、使ってはいけないnamespaceにアクセスしている </summary>
        ForbiddenNamespaceAccess,
        /// <summary> #r ディレクティブを使っている </summary>
        ReferenceDirectiveExists,
    }
}
