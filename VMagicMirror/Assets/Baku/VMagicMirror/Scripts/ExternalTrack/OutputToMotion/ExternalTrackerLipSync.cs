using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    /// <summary> 外部トラッカーからリップシンクを計算してくれるすごいやつだよ </summary>
    public class ExternalTrackerLipSync : MonoBehaviour
    {
        [Inject] 
        private ExternalTrackerDataSource _externalTracker = null;
        
        private readonly RecordLipSyncSource _source = new RecordLipSyncSource();
        public IMouthLipSyncSource LipSyncSource => _source;
        
        private void Update()
        {
            if (!_externalTracker.Connected)
            {
                _source.A = 0;
                _source.I = 0;
                _source.U = 0;
                _source.E = 0;
                _source.O = 0;
                //Debug.Log("ex tracker seems not connected!");
                return;
            }

            var mouth = _externalTracker.CurrentSource.Mouth;
            var jaw = _externalTracker.CurrentSource.Jaw;

            //ここが難しいんですよ～VRMと相性が悪いからね～

            float a = jaw.Open;
     
            //ということなので、ひとまずAにjaw.Openを入れるだけの残念実装にします。
            _source.A = a;
            //Debug.Log($"jaw open = {jaw.Open:0.000}");
            
            //TODO: せめてA/I/Uくらいは使い分けるように…
            
        }
    }
}
