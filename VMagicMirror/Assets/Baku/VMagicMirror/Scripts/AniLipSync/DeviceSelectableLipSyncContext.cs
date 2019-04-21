using System;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class DeviceSelectableLipSyncContext : OVRLipSyncContextBase
    {
        AudioClip clip;
        int head = 0;
        const int samplingFrequency = 48000;
        const int lengthSeconds = 1;
        readonly float[] processBuffer = new float[1024];
        readonly float[] microphoneBuffer = new float[lengthSeconds * samplingFrequency];

        public bool IsRecording { get; private set; } = false;
        public string DeviceName { get; private set; } = "";

        public void StartRecording(string deviceName)
        {
            if (!IsRecording && Microphone.devices.Contains(deviceName))
            {
                clip = Microphone.Start(deviceName, true, lengthSeconds, samplingFrequency);
                IsRecording = true;
                DeviceName = deviceName;
            }
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

        void Update()
        {
            if (!IsRecording)
            {
                return;
            }

            var position = Microphone.GetPosition(DeviceName);
            if (position < 0 || head == position)
            {
                return;
            }

            clip.GetData(microphoneBuffer, 0);
            while (GetDataLength(microphoneBuffer.Length, head, position) > processBuffer.Length)
            {
                var remain = microphoneBuffer.Length - head;
                if (remain < processBuffer.Length)
                {
                    Array.Copy(microphoneBuffer, head, processBuffer, 0, remain);
                    Array.Copy(microphoneBuffer, 0, processBuffer, remain, processBuffer.Length - remain);
                }
                else
                {
                    Array.Copy(microphoneBuffer, head, processBuffer, 0, processBuffer.Length);
                }

                OVRLipSync.ProcessFrame(Context, processBuffer, Frame);

                head += processBuffer.Length;
                if (head > microphoneBuffer.Length)
                {
                    head -= microphoneBuffer.Length;
                }
            }
        }

        static int GetDataLength(int bufferLength, int head, int tail)
        {
            if (head < tail)
            {
                return tail - head;
            }
            else
            {
                return bufferLength - head + tail;
            }
        }
    }
}