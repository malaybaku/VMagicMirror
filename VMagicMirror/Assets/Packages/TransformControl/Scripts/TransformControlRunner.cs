using UnityEngine;

namespace mattatz.TransformControl
{
    public class TransformControlRunner : MonoBehaviour
    {
        [SerializeField] private TransformControl control;
        private void Update() => control.Control();
    }
}
