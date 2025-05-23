using System.Collections.Generic;
using System.Linq;
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
        /// コンパイルされたスクリプトについて違反を検出する
        /// - using で怪しいものを使っている
        /// - using していないが、怪しいnamespaceに完全修飾名とかでアクセスしている
        /// </summary>
        /// <param name="compilation"></param>
        /// <returns></returns>
        public static AnalyzerResult AnalyzeCompileResult(Compilation compilation)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var trivialResult = AnalyzeSingleTrivialResult(syntaxTree);
                if (trivialResult.HasError)
                {
                    return trivialResult;
                }

                var result = AnalyzeSingleCompileResult(syntaxTree, compilation);
                if (result.HasError)
                {
                    return result;
                }
            }

            return AnalyzerResult.Success;
        }

        private static AnalyzerResult AnalyzeSingleTrivialResult(SyntaxTree syntaxTree)
        {
            // スクリプトを構文解析
            var root = syntaxTree.GetRoot();

            // NGリストのnamespaceを使ってたらNG
            // usingのほうが検出が trivial に実施できるので、まずはusingをチェックしていく
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name!.ToString());
            if (usingDirectives.Any(IsInvalidNamespace))
            {
                return new AnalyzerResult(
                    CSharpScriptAnalyzerResults.ForbiddenUsingStatement,
                    "Forbidden namespace detected. See detail at developer doc."
                );
            }

            // NOTE: これ以上の解析はCompilationがないとムズいので、一旦通す
            return AnalyzerResult.Success;
        }
        
        private static AnalyzerResult AnalyzeSingleCompileResult(SyntaxTree syntaxTree, Compilation compilation)
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
                    CSharpScriptAnalyzerResults.ForbiddenNamespaceAccess,
                    "Forbidden namespace access detected. See detail at developer doc."
                );
            }

            return AnalyzerResult.Success;
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
        public CSharpScriptAnalyzerResults Result { get; }
        public string Message { get; }

        public bool HasError => Result is not CSharpScriptAnalyzerResults.Success;
        
        public AnalyzerResult(CSharpScriptAnalyzerResults result, string message)
        {
            Result = result;
            Message = message;
        }

        public static AnalyzerResult Success { get; } = new(CSharpScriptAnalyzerResults.Success, "");
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
    }
}
