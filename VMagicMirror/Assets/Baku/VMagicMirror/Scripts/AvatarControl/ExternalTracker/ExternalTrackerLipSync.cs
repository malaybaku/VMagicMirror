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

        [Range(0f, 0.4f)]
        [SerializeField] private float bsMin = 0.15f;

        [Range(0.4f, 1f)]
        [SerializeField] private float bsMax = 0.6f;
        
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
            //手がかりになるブレンドシェイプがとても多いので、加重平均をclampでもいいかもしれない
            //(あんま凝るとAndroid対応とかが怪しくなるのが嫌なんだけど…)
            float a = MapClamp(2.0f * jaw.Open);
            float i = MapClamp(0.6f * (mouth.LeftSmile + mouth.RightSmile) - 0.1f);
            float u = MapClamp(0.4f * (mouth.Pucker + mouth.Funnel + jaw.Forward));

            if (a + i + u > 1.0f)
            {
                float factor = 1.0f / (a + i + u);
                a *= factor;
                i *= factor;
                u *= factor;
            }

            _source.A = a;
            _source.I = i;
            _source.U = u;
        }
        
        //0-1の範囲の値をmin-maxの幅のなかにギュッとあれします
        private float MapClamp(float value) 
            => Mathf.Clamp01((value - bsMin) / (bsMax - bsMin));
    }
}
