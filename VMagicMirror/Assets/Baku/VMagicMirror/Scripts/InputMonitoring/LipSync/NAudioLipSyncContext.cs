using System;
using NAudio.Wave;
using Zenject;

namespace Baku.VMagicMirror
{
    public class NAudioLipSyncContext : VmmLipSyncContextBase
    {
        //ほぼ全ての環境でアップサンプリングになる
        private const int SampleRate = 48000;
        //byte[]がこの長さになると0.5sec分のバッファになる
        private const int BufferLength = 96000;
        private WaveInEvent _waveIn = null;
        private string _deviceName = "";
        public override string DeviceName => _deviceName;

        private int _processBufferIndex = 0;
        private readonly float[] _processBuffer = new float[1024];

        //NOTE: 1秒分のリングバッファにしたうえで音に対するズレは許容する
        private readonly object _bufferLock = new object();
        private int _writeIndex = 0;
        private int _readIndex = 0;
        private readonly byte[] _buffer = new byte[BufferLength];
        private readonly byte[] _bufferOnRead = new byte[BufferLength];

        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender)
            => InitializeMessageIo(receiver, sender);
        
        private void Update()
        {
            if (_waveIn == null)
            {
                return;
            }

            int writeIndex = 0;
            lock (_bufferLock)
            {
                //読み込んでもProcessFrameする分量じゃない = 放置
                if (GetDataLength(BufferLength, _readIndex, _writeIndex) < _processBuffer.Length * 2)
                {
                    return;
                }

                //ちょっと贅沢だが、lockしてる行を狭くしたいのでガッとコピーしてしまう
                Array.Copy(_buffer, _bufferOnRead, _buffer.Length);
                writeIndex = _writeIndex;
            }
            ReadBuffer(writeIndex);
        }
        
        public override string[] GetAvailableDeviceNames()
        {
            var count = WaveInEvent.DeviceCount;
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = WaveInEvent.GetCapabilities(i).ProductName;
            }
            return result;
        }

        public override void StopRecording()
        {
            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= OnDataAvailable;
            }
            _waveIn?.StopRecording();
            _waveIn?.Dispose();
            _waveIn = null;

            //このタイミングでlock必須にはなりにくいが念のため。
            lock (_bufferLock)
            {
                _writeIndex = 0;
                _readIndex = 0;
            }
            _deviceName = "";
        }

        public override void StartRecording(string microphoneName)
        {
            if (_deviceName == microphoneName)
            {
                return;
            }
            
            StopRecording();
            int deviceNumber = FindDeviceNumber(microphoneName);
            if (deviceNumber < 0)
            {
                LogOutput.Instance.Write("Microphone with specified name was not detected: " + microphoneName);
                return;
            }

            _waveIn = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                //0.4secのバッファ: これだけあれば足りるはず…
                BufferMilliseconds = 16,
                NumberOfBuffers = 25,
                WaveFormat = new WaveFormat(SampleRate, 2),
            };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            lock (_bufferLock)
            {
                WriteBuffer(e.Buffer, e.BytesRecorded);
            }
        }

        private void WriteBuffer(byte[] data, int length)
        {
            if (length > _buffer.Length)
            {
                //コード上はこうならない想定だが一応。
                Array.Copy(data, _buffer, _buffer.Length);
                _writeIndex = 0;

            }
            else if (_writeIndex + length <= _buffer.Length)
            {
                //普通に書ききれる
                Array.Copy(data, 0, _buffer, _writeIndex, length);
                _writeIndex += length;
            }
            else
            {
                //端をまたぐ
                var tailLength = _buffer.Length - _writeIndex;
                Array.Copy(data, 0, _buffer, _writeIndex, tailLength);
                Array.Copy(data, tailLength, _buffer, 0, length - tailLength);
                _writeIndex = length - tailLength;
            }
        }

        private void ReadBuffer(int writeIndex)
        {
            //NOTE: どうせbytes -> float変換が挟まるのでもっさりやる。
            //呼び出し時点で_readIndexもwriteIndexも4の倍数なことが前提になっている点にも注意
            while (_readIndex != writeIndex)
            {
                float c1 = BitConverter.ToSingle(_bufferOnRead, _readIndex);
                _readIndex += 2;
                if (_readIndex >= _bufferOnRead.Length)
                {
                    _readIndex = 0;
                }
                
                float c2 = BitConverter.ToSingle(_bufferOnRead, _readIndex);
                _readIndex += 2;
                if (_readIndex >= _bufferOnRead.Length)
                {
                    _readIndex = 0;
                }

                _processBuffer[_processBufferIndex] = 0.5f * (c1 + c2);
                _processBufferIndex++;
                if (_processBufferIndex >= _processBuffer.Length)
                {
                    ApplySensitivityToProcessBuffer(_processBuffer);
                    SendVolumeLevelIfNeeded(_processBuffer);
                    OVRLipSync.ProcessFrame(Context, _processBuffer, Frame);
                    _processBufferIndex = 0;
                }
            }
        }
        
        private static int FindDeviceNumber(string microphoneName)
        {
            int count = WaveInEvent.DeviceCount;
            for (int i = 0; i < count; i++)
            {
                if (WaveInEvent.GetCapabilities(i).ProductName == microphoneName)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
