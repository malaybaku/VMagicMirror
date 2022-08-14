using System;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class DeviceSelectableLipSyncContext : VmmLipSyncContextBase
    {
        private const int SamplingFrequency = 48000;
        private const int LengthSeconds = 1;
        //この回数だけマイク音声の読み取り位置が変化しない状態が継続した場合、自動でマイクの再起動を試みる
        private const int PositionStopCountLimit = 120;

        private readonly float[] _processBuffer = new float[1024];
        private readonly float[] _microphoneBuffer = new float[LengthSeconds * SamplingFrequency];
        
        private bool _isRecording;
        private AudioClip _clip;
        private int _head = 0;

        private int _prevPosition = -1;
        private int _positionNotMovedCount = 0;

        private string _deviceName = "";
        public override string DeviceName => _deviceName;

        public override string[] GetAvailableDeviceNames() => Microphone.devices;
        
        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender)
        {
            InitializeMessageIo(receiver, sender);
        }
        
        public override void StartRecording(string deviceName)
        {
            if (_isRecording || !Microphone.devices.Contains(deviceName))
            {
                return;
            }
            
            _head = 0;
            for (int i = 0; i < _microphoneBuffer.Length; i++)
            {
                _microphoneBuffer[i] = 0;
            }
            _clip = Microphone.Start(deviceName, true, LengthSeconds, SamplingFrequency);
            _isRecording = true;
            _deviceName = deviceName;
        }

        public override void StopRecording()
        {
            if (_isRecording)
            {
                Microphone.End(DeviceName);
                _isRecording = false;
                _deviceName = "";
            }
        }

        private void Update()
        {
            if (!_isRecording)
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

                ApplySensitivityToProcessBuffer(_processBuffer);
                SendVolumeLevelIfNeeded(_processBuffer);
                //OVRLipSync.ProcessFrame(Context, _processBuffer, Frame);

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
            _isRecording = false;
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

    }
}