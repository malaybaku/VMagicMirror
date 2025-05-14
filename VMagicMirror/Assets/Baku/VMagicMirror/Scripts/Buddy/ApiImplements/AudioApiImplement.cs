using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Baku.VMagicMirror.Buddy
{
    public readonly struct AudioPlayArgs
    {
        public readonly string FilePath;
        public readonly float Volume;
        public readonly float Pitch;

        public AudioPlayArgs(string filePath, float volume, float pitch)
        {
            FilePath = filePath;
            // NOTE: ここで範囲制限するのもアリ
            Volume = volume;
            Pitch = pitch;
        }
    }

    public class AudioApiImplement
    {
        private readonly BuddyAudioSources _audioSources;

        public AudioApiImplement(BuddyAudioSources buddyAudioSources)
        {
            _audioSources = buddyAudioSources;
        }

        //NOTE: BuddyごとにAudioClipを適宜キャッシュしてほしいので、
        // PlayAsync(string filePath) で一括処理するのではなく、Clip取得と再生を分けている
        public async UniTask<AudioClip> GetAudioClipAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Audio file does not exists at path: {filePath}");
                return null;
            }
            
            var url = "file://" + filePath;
            var audioType = GetAudioType(filePath);

            using var request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load audio: {request.error}");
                return null;
            }

            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                Debug.LogError("Failed to create AudioClip.");
                return null;
            }

            return clip;
        }

        public void Play(AudioClip audioClip, AudioPlayArgs args)
        {
            var audioSource = _audioSources.GetAudioSource();
            audioSource.clip = audioClip;
            audioSource.volume = args.Volume;
            audioSource.pitch = args.Pitch;
            audioSource.Play();
        }
        
        private static AudioType GetAudioType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".wav" => AudioType.WAV,
                ".mp3" => AudioType.MPEG,
                _ => throw new NotSupportedException($"Unsupported audio format: {extension}")
            };
        }
    }    
}
