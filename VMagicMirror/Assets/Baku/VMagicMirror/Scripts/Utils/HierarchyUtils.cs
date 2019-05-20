using System.Collections.Generic;
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
    }
}

