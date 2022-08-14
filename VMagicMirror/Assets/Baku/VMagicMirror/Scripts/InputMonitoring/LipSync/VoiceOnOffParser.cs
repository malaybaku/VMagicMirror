using System;

namespace Baku.VMagicMirror
{
    /// <summary> リップシンクの音量レベルベースで喋ってる/喋ってないを判断する処理 </summary>
    public class VoiceOnOffParser
    {
        public VoiceOnOffParser(VmmLipSyncContextBase lipSync)
        {
            _lipSync = lipSync;
        }
        private readonly VmmLipSyncContextBase _lipSync = null;

        // Visemeのなかでこのしきい値を超える値が一つでもあれば、発声中だと判定する
        public float VisemeThreshold { get; set; }

        /// <summary> このフレーム数だけvisemeが連続でオンだったら発話状態と判断 </summary>
        public int OnCountThreshold { get; set; } = 3;

        /// <summary> このフレーム数だけvisemeが連続でオンだったら非発話状態と判断 </summary>
        public int OffCountThreshold { get; set; } = 3;

        private bool _isTalking = false;
        /// <summary> 現在発話中っぽいかどうかを取得します。 </summary>
        public bool IsTalking
        {
            get => _isTalking;
            private set
            {
                if (_isTalking != value)
                {
                    _isTalking = value;
                    IsTalkingChanged?.Invoke(value);
                }
            }
        }
        
        /// <summary> <see cref="IsTalking"/>が変化すると発火します。 </summary>
        public event Action<bool> IsTalkingChanged;

        private int _lipSyncOffCount = 0;
        private int _lipSyncOnCount = 0;
        
        /// <summary>
        /// 発生を検出していないデフォルト状態に戻す。
        /// この関数でIsTalkingが書き換わる場合もイベントが飛ばないことに注意
        /// </summary>
        public void Reset(bool raiseTalkingChange)
        {
            _isTalking = false;
            _lipSyncOffCount = 0;
            _lipSyncOnCount = 0;
        }
        
        /// <summary>
        /// 基本的に毎フレーム呼び出すことで、発話中かどうかの判定状態をアップデートします。
        /// </summary>
        public void Update()
        {
            //ざっくりやりたいこと: 音節の区切りをvisemeベースで推定し、visemeが有→無に転じたところで音節が区切れたものと扱う。
            //ただし、毎フレームでやるとノイズ耐性が低いので、数フレーム連続で続いた場合の立ち上がり/立ち下がりだけを扱う。
            if (!_lipSync.enabled || 
                false) //!(_lipSync.GetCurrentPhonemeFrame() is OVRLipSync.Frame frame))
            {
                return;
            }

            bool isTalking = false;
            // //NOTE: 0にはsil(無音)があるのでそれを避ける
            // for (int i = 1; i < frame.Visemes.Length; i++)
            // {
            //     if (frame.Visemes[i] > VisemeThreshold)
            //     {
            //         isTalking = true;
            //         break;
            //     }
            // }

            if (isTalking)
            {
                _lipSyncOnCount++;
                _lipSyncOffCount = 0;
                if (_lipSyncOnCount >= OnCountThreshold)
                {
                    IsTalking = true;
                    _lipSyncOnCount = OnCountThreshold;
                }
            }
            else
            {
                _lipSyncOffCount++;
                _lipSyncOnCount = 0;
                if (_lipSyncOffCount >= OffCountThreshold)
                {
                    IsTalking = false;
                    _lipSyncOffCount = OffCountThreshold;
                }
            }
        }
    }
}
