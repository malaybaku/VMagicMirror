using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    /// <summary> 外部トラッカーからリップシンクを計算してくれるすごいやつだよ </summary>
    public class ExternalTrackerLipSync : MonoBehaviour
    {
        [Header("Non-Emphasize Mode")]
        [Range(0f, 0.4f)]
        [SerializeField] private float bsMin = 0.15f;
        [Range(0.4f, 1f)]
        [SerializeField] private float bsMax = 0.6f;

        //NOTE: 強調モード時のパラメータでSerializeなモノってあるかなあ？
        [Header("Emphasize Mode")] 
        [Range(1f, 2f)]
        [SerializeField] private float jawOpenEmphasizeFactor = 1.5f;
        [Range(0f, 0.5f)]
        [SerializeField] private float jawOpenCutoff = 0.2f;

        [Range(1f, 2f)]
        [SerializeField] private float smileEmphasizeFactor = 1.3f;
        [Range(0f, 0.5f)]
        [SerializeField] private float smileCutoff = 0.2f;

        [Range(1f, 3f)]
        [SerializeField] private float mouthPuckerEmphasizeFactor = 1.8f;
        [Range(0f, 0.5f)]
        [SerializeField] private float mouthPuckerCutoff = 0.2f;

        [Range(0f, 1f)]
        [SerializeField] private float mixedValueSumMax = 0.8f;
        [SerializeField] private float diffPerSecond = 18f;


        private readonly RecordLipSyncSource _source = new RecordLipSyncSource();
        public IMouthLipSyncSource LipSyncSource => _source;

        private ExternalTrackerDataSource _externalTracker = null;
        private bool _emphasizeExpression = false;
        
        [Inject]
        public void Initialize(ExternalTrackerDataSource externalTracker, IMessageReceiver receiver)
        {
            _externalTracker = externalTracker;
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnableEmphasizeExpression,
                message => _emphasizeExpression = message.ToBoolean()
                );
        }

        private void Update()
        {
            if (!_externalTracker.Connected)
            {
                _source.A = 0;
                _source.I = 0;
                _source.U = 0;
                _source.E = 0;
                _source.O = 0;
                return;
            }

            if (_emphasizeExpression)
            {
                UpdateValuesWithEmphasize();
            }
            else
            {
                UpdateValuesDefault();
            }
        }

        private void UpdateValuesWithEmphasize()
        {
            var mouth = _externalTracker.CurrentSource.Mouth;
            var jaw = _externalTracker.CurrentSource.Jaw;
            
            //NOTE: 強調モードは通常モードと異なり、以下を行う
            // - IとUに相当するはずのブレンドシェイプを大きめに補正かけて適用
            // - winner keep value + winnerじゃない値については、ブレンドシェイプの合計値の制限を考慮して加味
            // 初期実装と違ってAndroidでどうとか一切考えてないピーキーな実装に化けております…はい…

            float a = Mathf.Clamp01(jawOpenEmphasizeFactor * jaw.Open - jawOpenCutoff);
            float i = Mathf.Clamp01(0.5f * smileEmphasizeFactor * (mouth.LeftSmile + mouth.RightSmile) - smileCutoff);
            float u = Mathf.Clamp01(mouthPuckerEmphasizeFactor * mouth.Pucker - mouthPuckerCutoff);
            
            float resultA = 0f; 
            float resultI = 0f;
            float resultU = 0f;
            
            //同時に複数の値が1fになるケースがあり得ることに注意。
            //優先度がU, A, Iなのは、手前のものほど狙わないと作れない口の形であるため
            if (u >= Mathf.Max(a, i))
            {
                
                resultU = u;
                if (u < mixedValueSumMax && a + i > Mathf.Epsilon)
                {
                    float factor = Mathf.Min(1f, (mixedValueSumMax - u) / (a + i));
                    resultA = a * factor;
                    resultI = i * factor;
                }
            }
            else if (a > Mathf.Max(i, u))
            {
                resultA = a;
                if (a < mixedValueSumMax && i + u > Mathf.Epsilon)
                {
                    float factor = Mathf.Min(1f, (mixedValueSumMax - a) / (i + u));
                    resultI = i * factor;
                    resultU = u * factor;
                }
            }
            else
            {
                //ここに到達する場合は口が閉じてることも多いが、やること自体は一緒
                resultI = i;
                if (i < mixedValueSumMax && a + u > Mathf.Epsilon)
                {
                    float factor = Mathf.Min(1f, (mixedValueSumMax - i) / (a + u));
                    resultA = a * factor;
                    resultU = u * factor;
                }
            }

            var diffMax = diffPerSecond * Time.deltaTime;
            _source.A = resultA - 
                _source.A > diffMax ? _source.A + diffMax :
                resultA - _source.A < -diffMax ? _source.A - diffMax : 
                resultA;
            _source.I = resultI - 
                _source.I > diffMax ? _source.I + diffMax :
                resultI - _source.I < -diffMax ? _source.I - diffMax : 
                resultI;
            _source.U = resultU - 
                _source.U > diffMax ? _source.U + diffMax :
                resultU - _source.U < -diffMax ? _source.U - diffMax : 
                resultU;

            if (_source.A + _source.I + _source.U > 1f)
            {
                var factor = 1f / (_source.A + _source.I + _source.U);
                _source.A *= factor;
                _source.I *= factor;
                _source.U *= factor;
            }

            //NOTE: いちおう冪等に書いといた方が気分がいいので消しておく
            _source.E = 0f;
            _source.O = 0f;
        }
        
        private void UpdateValuesDefault()
        {
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
            //NOTE: いちおう冪等に書いといた方が気分がいいので消しておく
            _source.E = 0f;
            _source.O = 0f;
        }
        
        //0-1の範囲の値をmin-maxの幅のなかにギュッとあれします
        private float MapClamp(float value) => Mathf.Clamp01((value - bsMin) / (bsMax - bsMin));
        
        
    }
}
