using System;
using System.Collections.Generic;
using NAudio.Wave;
using Zenject;

namespace Baku.VMagicMirror
{
    public class NAudioLipSyncContext : VmmLipSyncContextBase
    {
        private const float ShortToSingle = 1.0f / 32768f;

        //ほぼ全ての環境でアップサンプリングになる
        private const int SampleRate = 48000;
        //byte[4] = short[2] = float[2] = 1サンプルなため、コレで24000サンプル = 0.5secぶん
        private const int BufferLength = 96000;
        private WaveInEvent _waveIn = null;
        private string _deviceName = "";
        public override string DeviceName => _deviceName;

        private int _processBufferIndex = 0;
        private readonly float[] _processBuffer = new float[2048];

        //NOTE: 1秒分のリングバッファにしたうえで音に対するズレは許容する
        private readonly object _bufferLock = new object();

        //次にバイナリを_bufferへ書き込むべきインデックスを保持し、0以上、(BufferLength - 1)以下
        private int _writeIndex = 0;
        //_bufferで読み込み済みの位置の次のインデックスを保持し、0以上、(BufferLength - 1)以下
        private int _readIndex = 0;
        private readonly byte[] _buffer = new byte[BufferLength];
        //NOTE: 最悪ケースのために_bufferと同じ長さだが、ふつう先頭付近のみを使う
        private readonly byte[] _bufferOnRead = new byte[BufferLength];

        private readonly Atomic<bool> _firstDataReceive = new Atomic<bool>();

        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender)
            => InitializeMessageIo(receiver, sender);
        
        private void Update()
        {
            if (_waveIn == null)
            {
                return;
            }

            var byteLen = 0;
            lock (_bufferLock)
            {
                byteLen = GetDataLength(BufferLength, _readIndex, _writeIndex);
                if (byteLen < (_processBuffer.Length - _processBufferIndex) * 4)
                {
                    //読み込んでもFrameとして処理する分量にならない = 無視
                    return;
                }

                //_readIndexと_writeIndexの間、つまり読み込めてない分を書き写す
                if (_readIndex + byteLen <= BufferLength)
                {
                    Array.Copy(_buffer, _readIndex, _bufferOnRead, 0, byteLen);
                }
                else
                {
                    var tailLength = BufferLength - _readIndex;
                    Array.Copy(
                        _buffer, _readIndex, 
                        _bufferOnRead, 0, tailLength
                        );
                    Array.Copy(
                        _buffer, 0, 
                        _bufferOnRead, tailLength, byteLen - tailLength
                        );
                }

                _readIndex += byteLen;
                if (_readIndex >= BufferLength)
                {
                    _readIndex -= BufferLength;
                }
            }
            
            ReadBuffer(byteLen);
        }
        
        public override string[] GetAvailableDeviceNames()
        {
            //NOTE: タイミングによっては切断したデバイスのCapを取りに行ってエラーになることがある。
            //こうなるとQueryが戻らなくなって都合が悪いため、エラーが起きたら拾える範囲の値だけ使って返却する
            var result = new List<string>();
            try
            {
                var count = WaveInEvent.DeviceCount;
                for (int i = 0; i < count; i++)
                {
                    result.Add(WaveInEvent.GetCapabilities(i).ProductName);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
            return result.ToArray();
        }

        public override void StopRecording()
        {
            if (_waveIn != null)
            {
                _waveIn.RecordingStopped -= OnRecordingStopped;
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
            _firstDataReceive.Value = false;
        }

        public override void StartRecording(string microphoneName)
        {
            if (_deviceName == microphoneName)
            {
                return;
            }
            
            StopRecording();
            var deviceNumber = FindDeviceNumber(microphoneName);
            if (deviceNumber < 0)
            {
                LogOutput.Instance.Write("Microphone with specified name was not detected: " + microphoneName);
                return;
            }
            LogOutput.Instance.Write("Start Recording:" + microphoneName);
            _deviceName = microphoneName;

            _waveIn = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                //0.4secのバッファ: これだけあれば足りるはず…
                BufferMilliseconds = 16,
                NumberOfBuffers = 25,
                WaveFormat = new WaveFormat(SampleRate, 2),
            };
            _waveIn.RecordingStopped += OnRecordingStopped;
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                LogOutput.Instance.Write($"Microphone Recording Stopped by exception, {e.Exception.Message}");
            }
            StopRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_firstDataReceive.Value)
            {
                _firstDataReceive.Value = true;
                LogOutput.Instance.Write("Receive First Audio Buffer: " + _deviceName);
            }
            
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
                if (_writeIndex >= BufferLength)
                {
                    _writeIndex = 0;
                }
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

        private void ReadBuffer(int length)
        {
            //4byte -> 1sampleに変化させつつ読んでいく。lengthは4の倍数な前提であることに注意
            for (var i = 0; i < length; i += 4)
            {
                float c1 = BitConverter.ToInt16(_bufferOnRead, i);
                float c2 = BitConverter.ToInt16(_bufferOnRead, i + 2);

                _processBuffer[_processBufferIndex] = ShortToSingle * c1;
                _processBuffer[_processBufferIndex + 1] = ShortToSingle * c2;
                _processBufferIndex += 2;
                if (_processBufferIndex >= _processBuffer.Length)
                {
                    ApplySensitivityToProcessBuffer(_processBuffer);
                    UpdateVolumeLevelAndSendIfNeeded(_processBuffer);
                    OVRLipSync.ProcessFrame(Context, _processBuffer, Frame, true);
                    _processBufferIndex = 0;
                }
            }
        }
        
        private static int FindDeviceNumber(string microphoneName)
        {
            var count = WaveInEvent.DeviceCount;
            for (var i = 0; i < count; i++)
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
