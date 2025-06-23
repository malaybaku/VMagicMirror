using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class HierarchyUtils 
    {
        /// <summary>
        /// オブジェクトの子孫を全て列挙する。
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static IEnumerable<Transform> GetAllChildrenRecurse(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            // 深さ優先で走査しているが特に意味はない
            yield return target;
            for (int i = 0; i < target.childCount; i++)
            {
                foreach (var t in GetAllChildrenRecurse(target.GetChild(i)))
                {
                    yield return t;
                }
            }
        }

        /// <summary>
        /// Scene直下側から順に `/` で区切ったオブジェクトの名称を返す。デバッグで使う想定
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetHierarchyIncludedGameObjectName(GameObject obj)
        {
            if (obj == null) return "";

            var sb = new StringBuilder();
            GetHierarchyIncludedGameObjectName(obj.transform, sb);
            return sb.ToString();
        }

        private static void GetHierarchyIncludedGameObjectName(Transform t, StringBuilder result)
        {
            while (t != null)
            {
                if (result.Length > 0)
                {
                    result.Insert(0, '/');
                }
                result.Insert(0, t.name);

                t = t.parent;
            }
        }
    }
}

