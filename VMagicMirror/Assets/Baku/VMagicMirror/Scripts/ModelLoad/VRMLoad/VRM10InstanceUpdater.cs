using System;
using UniRx;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VRM10Instance.Runtime.Processの呼び出しタイミングを調整してくれるやつ.
    /// UPMのコードの実行タイミングは勝手に変更できないので…
    /// </summary>
    public class VRM10InstanceUpdater : MonoBehaviour
    {
        private bool _hasModel;
        private Vrm10Instance _instance;

        private readonly Subject<Unit> _preRuntimeProcessed = new();
        private readonly Subject<Unit> _postRuntimeProcessed = new();
        public IObservable<Unit> PostRuntimeProcess => _postRuntimeProcessed;
        public IObservable<Unit> PreRuntimeProcess => _preRuntimeProcessed;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _instance = info.instance;
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _instance = null;
                _hasModel = false;
            };
        }

        private void LateUpdate()
        {
            if (_hasModel)
            {
                _preRuntimeProcessed.OnNext(Unit.Default);
                _instance.Runtime.Process();
                _postRuntimeProcessed.OnNext(Unit.Default);
            }
        }
    }
}
