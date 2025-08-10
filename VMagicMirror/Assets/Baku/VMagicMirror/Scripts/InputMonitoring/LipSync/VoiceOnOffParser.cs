using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> リップシンクの音量レベルベースで喋ってる/喋ってないを判断する処理 </summary>
    public class VoiceOnOffParser : ITickable
    {
        [Inject]
        public VoiceOnOffParser(VmmLipSyncContextBase lipSync)
        {
            _lipSync = lipSync;
        }

        private readonly VmmLipSyncContextBase _lipSync;

        // NOTE: デフォルト値は「そこそこちゃんと喋ってないと検出しない」という値になることを志向した60FPS想定の値になっている

        /// <summary> Visemeのなかでこのしきい値を超える値が一つでもあれば、発声中だと判定する </summary>
        public float VisemeThreshold { get; set; } = 0.2f;

        /// <summary> このフレーム数だけvisemeが連続でオンだったら発話状態と判断する </summary>
        public int OnCountThreshold { get; set; } = 6;

        /// <summary> このフレーム数だけvisemeが連続でオンだったら非発話状態と判断 </summary>
        public int OffCountThreshold { get; set; } = 16;

        private readonly ReactiveProperty<bool> _isTalking = new();
        /// <summary> 現在発話中かどうかを取得します。 </summary>
        public IReadOnlyReactiveProperty<bool> IsTalking => _isTalking;

        private int _lipSyncOffCount = 0;
        private int _lipSyncOnCount = 0;
        
        /// <summary> 発生を検出していないデフォルト状態に戻す。 </summary>
        public void Reset()
        {
            _isTalking.Value = false;
            _lipSyncOffCount = 0;
            _lipSyncOnCount = 0;
        }
        
        /// <summary> 発話中かどうかの判定をアップデートする </summary>
        void ITickable.Tick()
        {
            //ざっくりやりたいこと: 音節の区切りをvisemeベースで推定し、visemeが有→無に転じたところで音節が区切れたものと扱う。
            //ただし、毎フレームでやるとノイズ耐性が低いので、数フレーム連続で続いた場合の立ち上がり/立ち下がりだけを扱う。
            if (!_lipSync.enabled || 
                !(_lipSync.GetCurrentPhonemeFrame() is OVRLipSync.Frame frame))
            {
                return;
            }

            bool isTalking = false;
            //NOTE: 0にはsil(無音)があるのでそれを避ける
            for (int i = 1; i < frame.Visemes.Length; i++)
            {
                if (frame.Visemes[i] > VisemeThreshold)
                {
                    isTalking = true;
                    break;
                }
            }

            if (isTalking)
            {
                _lipSyncOnCount++;
                _lipSyncOffCount = 0;
                if (_lipSyncOnCount >= OnCountThreshold)
                {
                    _isTalking.Value = true;
                    _lipSyncOnCount = OnCountThreshold;
                }
            }
            else
            {
                _lipSyncOffCount++;
                _lipSyncOnCount = 0;
                if (_lipSyncOffCount >= OffCountThreshold)
                {
                    _isTalking.Value = false;
                    _lipSyncOffCount = OffCountThreshold;
                }
            }
        }
    }
}
