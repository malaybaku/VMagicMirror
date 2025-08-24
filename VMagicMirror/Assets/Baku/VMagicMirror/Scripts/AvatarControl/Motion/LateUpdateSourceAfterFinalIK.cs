using System;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// FinalIKのIKスクリプトがIK処理を行った後、かつ他のスクリプトより前でLateUpdateを発火させるやつ
    /// </summary>
    public class LateUpdateSourceAfterFinalIK : MonoBehaviour
    {
        private readonly Subject<Unit> _onPreLateUpdate = new();
        /// <summary>
        /// <see cref="OnLateUpdate"/> の直前に発火する。ちょっとだけタイミング早めに実行したいものはここでSubscribeする
        /// </summary>
        public Observable<Unit> OnPreLateUpdate => _onPreLateUpdate;

        private readonly Subject<Unit> _onLateUpdate = new();
        public Observable<Unit> OnLateUpdate => _onLateUpdate;
        
        private void LateUpdate()
        {
            _onPreLateUpdate.OnNext(Unit.Default);
            _onLateUpdate.OnNext(Unit.Default);
        }
    }
}
