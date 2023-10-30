using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// FinalIKのIKスクリプトがIK処理を行った後、かつ他のスクリプトより前でLateUpdateを発火させるやつ
    /// </summary>
    public class LateUpdateSourceAfterFinalIK : MonoBehaviour
    {
        private readonly Subject<Unit> _onLateUpdate = new();
        public IObservable<Unit> OnLateUpdate => _onLateUpdate;
        private void LateUpdate() => _onLateUpdate.OnNext(Unit.Default);
    }
}
