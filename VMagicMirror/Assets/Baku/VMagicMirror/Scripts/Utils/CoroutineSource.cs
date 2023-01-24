using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public interface ICoroutineSource
    {
        MonoBehaviour AssociatedBehaviour { get; }
    }

    public class CoroutineSource : MonoBehaviour, ICoroutineSource
    {
        public MonoBehaviour AssociatedBehaviour => this;
    }

    public static class CoroutineSourceExtension
    {
        public static void StartCoroutine(this ICoroutineSource source, IEnumerator coroutine)
        {
            source.AssociatedBehaviour.StartCoroutine(coroutine);
        }
    }
}
