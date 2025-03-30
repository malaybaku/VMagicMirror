using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // Execution Orderの都合を何かいい感じにするやつ
    public class KinematicSetterTimingInvoker : MonoBehaviour
    {
        private readonly Subject<Unit> _onLateUpdate = new();
        public IObservable<Unit> OnLateUpdate => _onLateUpdate;
        private void LateUpdate() => _onLateUpdate.OnNext(Unit.Default);
    }
}
