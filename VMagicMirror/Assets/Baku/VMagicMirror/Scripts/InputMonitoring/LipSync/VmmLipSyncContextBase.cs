using UnityEngine;

namespace Baku.VMagicMirror
{
    public abstract class VmmLipSyncContextBase : OVRLipSyncContextBase
    {
        //この値(dB)から+50dBまでの範囲を0 ~ 50のレベル情報として通知する。適正音量の目安になる値。
        private const int BottomVolumeDb = -38;
        
        //ボリューム情報は数フレームに1回送ればいいよね、という値
        private const int SendVolumeLevelSkip = 2;
        //ボリュームが急に下がった場合は実際の値ではなく、直前フレームからこのdB値だけ落とした値をWPFに通知する
        private const int LevelDecreasePerRefresh = 1;
        
        private bool _sendMicrophoneVolumeLevel = false;
        private int _volumeLevelSendCount = 0;
        //ボリューム値を記憶する。よくある「ガッと上がってスーッと下がる」をやるために必要。
        private int _currentVolumeLevel = 0;
        private int _sensitivity = 0;
        
        private float _sensitivityFactor = 1f;
        /// <summary> マイク感度を[dB]単位で取得、設定します。 </summary>
        public int Sensitivity
        {
            get => _sensitivity;
            set
            {
                if (_sensitivity == value)
                {
                    return;
                }
                _sensitivity = value;
                _sensitivityFactor = Mathf.Pow(10f, Sensitivity * 0.1f);
            }
        }
        
        private IMessageSender _sender;
        
        
        public abstract void StopRecording();
        public abstract void StartRecording(string microphoneName);
        
        /// <summary>
        /// 現在録音をしていれば録音に使っているデバイス名、そうでなければ空文字列を返します。
        /// </summary>
        public abstract string DeviceName { get; }
        
        /// <summary>
        /// 利用可能なマイク名の一覧を返します。 
        /// </summary>
        /// <returns></returns>
        public abstract string[] GetAvailableDeviceNames();

        protected void InitializeMessageIo(IMessageReceiver receiver, IMessageSender sender)
        {
            receiver.AssignCommandHandler(
                VmmCommands.SetMicrophoneVolumeVisibility,
                message =>
                {
                    _sendMicrophoneVolumeLevel = bool.TryParse(message.Content, out var v) && v;
                    if (!_sendMicrophoneVolumeLevel)
                    {
                        _volumeLevelSendCount = 0;
                        _currentVolumeLevel = 0;
                    }
                });
            _sender = sender;            
        }

        protected void SendVolumeLevelIfNeeded(float[] buffer)
        {
            if (!_sendMicrophoneVolumeLevel)
            {
                return;
            }
            
            _volumeLevelSendCount++;
            if (_volumeLevelSendCount >= SendVolumeLevelSkip)
            {
                _volumeLevelSendCount = 0;
                UpdateVolumeLevel(buffer);
                _sender.SendCommand(MessageFactory.Instance.MicrophoneVolumeLevel(_currentVolumeLevel));
            }
        }
        
                
        //マイク感度が0dB以外の場合、値を調整します。
        protected void ApplySensitivityToProcessBuffer(float[] buffer)
        {
            if (Sensitivity == 0)
            {
                return;
            }
            
            //ここ遅かったらヤダな～というポイント
            for (int i = 0; i < buffer.Length; i++)
            {
                //NOTE: clampしないでもOVRLipSyncは動くのでclampしないでいい
                buffer[i] *= _sensitivityFactor;
            }
        }

        //バッファに載ってる音に対してゲインを0 ~ 50の数値に変換したものを計算してデータを更新する。
        private void UpdateVolumeLevel(float[] buffer)
        {
            //NOTE: この方法はちょっと桁落ちとかのリスクあるんだけど、そんなにシビアな計算じゃないのでザツにやる
            float sum = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                sum += buffer[i] * buffer[i];
            }
            
            float mean = sum / buffer.Length;
            float meanDb = Mathf.Log10(mean) * 10f;
            int rawResult = Mathf.Clamp((int)(meanDb - BottomVolumeDb), 0, 50);
            _currentVolumeLevel = Mathf.Max(rawResult, _currentVolumeLevel - LevelDecreasePerRefresh);
        }

        protected static int GetDataLength(int bufferLength, int head, int tail) 
            => (head < tail) 
                ? (tail - head) 
                : (bufferLength - head + tail);
    }
}
