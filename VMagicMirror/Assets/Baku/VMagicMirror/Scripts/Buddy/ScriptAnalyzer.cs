using System;
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
                var trivialResult = AnalyzeUsingStatement(syntaxTree);
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

        // NGリストのnamespaceを使ってたらNGにするようなフィルタ
        // うっかり使ったケースは基本このチェックに引っかかるはず
        private static AnalyzerResult AnalyzeUsingStatement(SyntaxTree syntaxTree)
        {
            var root = syntaxTree.GetRoot();
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u =>
                {
                    var namespaceLiteral = u.Name!.ToString();
                    return CheckNamespaceValidity(namespaceLiteral);
                });

            // NOTE: Defaultにfallbackすると、一見invalidっぽく見えるがnamespaceのほうも空になった値が戻ってくる
            var invalidUsing = usingDirectives.FirstOrDefault(v => !v.isValid);
            if (!string.IsNullOrEmpty(invalidUsing.invalidNamespace))
            {
                return new AnalyzerResult(
                    CSharpScriptAnalyzerResults.ForbiddenUsingStatement,
                    $"Access to namespace '{invalidUsing.invalidNamespace}' is not allowed in buddy scripts. See detail at developer doc."
                );
            }

            // NOTE: これ以上の解析はCompilationがないとムズいので、一旦通す
            return AnalyzerResult.Success;
        }
        
        private static AnalyzerResult AnalyzeSingleCompileResult(SyntaxTree syntaxTree, Compilation compilation)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            // 要はExpression全般でnamespaceにNGリストを適用したいので、Concatして見ていく。行の順番はあんまりこだわらない。
            var checkResult = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>().Cast<ExpressionSyntax>()
                .Concat(root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
                .Concat(root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
                .Select(expr =>
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(expr);
                    return CheckNamespaceValidity(symbolInfo);
                })
                .FirstOrDefault(v => !v.isValid);
            
            if (!string.IsNullOrEmpty(checkResult.invalidNamespace))
            {
                return new AnalyzerResult(
                    CSharpScriptAnalyzerResults.ForbiddenNamespaceAccess,
                    $"Access to namespace '{checkResult.invalidNamespace}' is not allowed in buddy scripts. See detail at developer doc."
                );
            }

            return AnalyzerResult.Success;
        }

        private static (bool isValid, string invalidNamespace) CheckNamespaceValidity(SymbolInfo symbolInfo)
        {
            var symbol = symbolInfo.Symbol;
            if (symbol == null)
            {
                return (true, "");
            }

            var containingNamespace = symbol.ContainingNamespace?.ToDisplayString();
            if (containingNamespace == null)
            {
                return (true, "");
            }
            
            return CheckNamespaceValidity(containingNamespace);
        }

        private static (bool isValid, string invalidNamespace) CheckNamespaceValidity(string value)
        {
            // 問題ないケースがメジャーなので、問題ないほうをさっさと通す
            if (!NgNamespaces.Contains(value) && !NgPrefixes.Any(value.StartsWith))
            {
                return (true, "");
            }

            // ダメなケース: どれに引っかかったのかチェック
            var result =
                NgNamespaces.FirstOrDefault(v => v == value)
                ?? NgPrefixes.FirstOrDefault(v => v.StartsWith(value))
                ?? throw new InvalidOperationException("logic error: this line must not be reached");
            return (false, result.TrimEnd('.'));
        }
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
