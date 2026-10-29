using System;
using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirror
{
    /// <summary> Resolves only the supported Perfect Sync names, preserving exact matches first. </summary>
    public static class PerfectSyncExpressionNames
    {
        /// <summary> Mesh matching is always case-insensitive; existing expressions, including empty ones, win. </summary>
        public static IEnumerable<(string Name, int Index)> ResolveMeshShapes(
            IEnumerable<string> meshNames, IEnumerable<string> availableExpressions, IEnumerable<string> perfectSyncNames)
        {
            var names = meshNames.ToArray();
            var available = new HashSet<string>(availableExpressions, StringComparer.Ordinal);
            foreach (var name in perfectSyncNames)
            {
                if (available.Contains(name))
                {
                    continue;
                }
                var index = Array.FindIndex(names, n => string.Equals(n, name, StringComparison.Ordinal));
                if (index < 0)
                {
                    index = Array.FindIndex(names, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                }
                if (index >= 0)
                {
                    yield return (name, index);
                }
            }
        }

        public static IEnumerable<(string Name, string SourceName)> ResolveAliases(
            IEnumerable<string> originalNames, IEnumerable<string> perfectSyncNames)
        {
            var names = originalNames.ToArray();
            var exactNames = new HashSet<string>(names, StringComparer.Ordinal);
            foreach (var name in perfectSyncNames)
            {
                if (exactNames.Contains(name))
                {
                    continue;
                }

                // In ambiguous models, consistently prefer the first original definition.
                var source = names.FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                if (source != null)
                {
                    yield return (name, source);
                }
            }
        }
    }
}
