using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class DeviceSelectableLipSyncContext : OVRLipSyncContextBase
    {
        private const int SamplingFrequency = 48000;
        private const int LengthSeconds = 1;
        //この回数だけマイク音声の読み取り位置が変化しない状態が継続した場合、自動でマイクの再起動を試みる
        private const int PositionStopCountLimit = 120;
        
        private readonly float[] _processBuffer = new float[1024];
        private readonly float[] _microphoneBuffer = new float[LengthSeconds * SamplingFrequency];
        
        private AudioClip _clip;
        private int _head = 0;

        private int _prevPosition = -1;
        private int _positionNotMovedCount = 0;

        private int _sensitivity = 0;

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

        private float _sensitivityFactor = 1f;

        public bool IsRecording { get; private set; } = false;
        public string DeviceName { get; private set; } = "";
        
        public void StartRecording(string deviceName)
        {
            if (IsRecording || !Microphone.devices.Contains(deviceName))
            {
                return;
            }
            
            _head = 0;
            for (int i = 0; i < _microphoneBuffer.Length; i++)
            {
                _microphoneBuffer[i] = 0;
            }
            _clip = Microphone.Start(deviceName, true, LengthSeconds, SamplingFrequency);
            IsRecording = true;
            DeviceName = deviceName;
        }

        public void StopRecording()
        {
            if (IsRecording)
            {
                Microphone.End(DeviceName);
                IsRecording = false;
                DeviceName = "";
            }
        }

        private void Update()
        {
            if (!IsRecording)
            {
                return;
            }

            int position = Microphone.GetPosition(DeviceName);

            //読み取り位置がずっと動かない場合、マイクを復帰させる。PS4コンを挿抜したときにマイクが勝手に止まる事があります…。
            if (position == _prevPosition)
            {
                _positionNotMovedCount++;
                if (_positionNotMovedCount > PositionStopCountLimit)
                {
                    _positionNotMovedCount = 0;
                    RestartMicrophone();
                    return;
                }
            }
            else
            {
                _prevPosition = position;
                _positionNotMovedCount = 0;
            }

            //マイクの動いてる/動かないの検知とは別で範囲チェック
            if (position < 0 || _head == position)
            {
                return;
            }

            _clip.GetData(_microphoneBuffer, 0);
            while (GetDataLength(_microphoneBuffer.Length, _head, position) > _processBuffer.Length)
            {
                var remain = _microphoneBuffer.Length - _head;
                if (remain < _processBuffer.Length)
                {
                    Array.Copy(_microphoneBuffer, _head, _processBuffer, 0, remain);
                    Array.Copy(_microphoneBuffer, 0, _processBuffer, remain, _processBuffer.Length - remain);
                }
                else
                {
                    Array.Copy(_microphoneBuffer, _head, _processBuffer, 0, _processBuffer.Length);
                }

                ApplySensitivityToProcessBuffer();
                OVRLipSync.ProcessFrame(Context, _processBuffer, Frame);

                _head += _processBuffer.Length;
                if (_head > _microphoneBuffer.Length)
                {
                    _head -= _microphoneBuffer.Length;
                }
            }
        }

        //マイクの録音をリスタートしようとします。もし指定したマイクが完全に認識できない場合、ストップします。
        private void RestartMicrophone()
        {
            Microphone.End(DeviceName);
            IsRecording = false;
            if (Microphone.devices.Contains(DeviceName))
            {
                Debug.Log("Restart Microphone Success: " + DeviceName);
                StartRecording(DeviceName);
            }
            else
            {
                Debug.Log("Restart Microphone Failed: " + DeviceName);
            }
        }
        
        //マイク感度が0dB以外の場合、値を調整します。
        private void ApplySensitivityToProcessBuffer()
        {
            if (Sensitivity == 0)
            {
                return;
            }
            
            //ここ遅かったらヤダな～というポイント
            for (int i = 0; i < _processBuffer.Length; i++)
            {
                _processBuffer[i] = math.clamp(_processBuffer[i] * _sensitivityFactor, -1f, 1f);
            }
        }


        static int GetDataLength(int bufferLength, int head, int tail) 
            => (head < tail) 
                ? (tail - head) 
                : (bufferLength - head + tail);
    }
}