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
}
