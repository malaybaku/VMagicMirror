using System;
using System.Collections.Generic;
using System.Threading;
using VMagicMirror.Buddy;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class AudioApi : IAudio
    {
        private const int ClipCacheCount = 10;
        // 番号が若いほうが最近使ったclip
        private readonly List<(string path, AudioClip clip)> _clipCache = new(ClipCacheCount);

        private readonly BuddyFolder _buddyFolder;
        private readonly BuddyLogger _logger;
        private readonly AudioApiImplement _impl;
        private readonly CancellationTokenSource _cts = new();
        
        internal AudioApi(BuddyFolder buddyFolder, BuddyLogger logger, AudioApiImplement impl)
        {
            _buddyFolder = buddyFolder;
            _logger = logger;
            _impl = impl;
        }

        public void Play(string path, float volume = 1.0f, float pitch = 1.0f, string key = "")
            => ApiUtils.Try(_buddyFolder, _logger, () =>
            {
                var fullPath = ApiUtils.GetAssetFullPath(_buddyFolder, path);
                var args = new AudioPlayArgs(fullPath, volume, pitch, key);

                if (TryPlayFromCache(args))
                {
                    return;
                }

                GetClipAndPlayAsync(args, _cts.Token).Forget();
            });

        public void Stop(string key = "")
        {
            ApiUtils.Try(_buddyFolder, _logger, () =>
            {
                var stoppedKeys = _impl.TryStop(_buddyFolder.BuddyId, key);
                // イベントを発火させる場合は下記のようにすればよい、はず
                // for (var i = 0; i < stoppedKeys.Length; i++)
                // {
                //     var key = stoppedKeys[i];
                //     // NOTE: 普通のイベントはScriptEventInvokerに発火させるが、
                //     //   ここはStop関数自体がスクリプトから呼ばれる & Stopに成功する場合は直ちに停止したはずなので、
                //     //   ただちにイベントを呼ぶ。
                //     // NOTE: イベント発火をTryで囲うのもアリだが、そんなに害がない気がするのでパス
                //     AudioStopped?.Invoke(new AudioStoppedInfo(key, AudioStoppedReason.Stopped));
                // }
            });
        }

        // NOTE: IAudioで公開を見送ってるので一旦封鎖している
        // public event Action<AudioStartedInfo> AudioStarted;
        // public event Action<AudioStoppedInfo> AudioStopped;
        
        // NOTE: バイナリを扱う方法もホントは欲しいのだが、mp3のサポートができないうちは頑張らず、「ファイルに一旦保存してもろて…」というスタンスを取る
        // public void Play(byte[] data) => throw new NotImplementedException();

        private bool TryPlayFromCache(AudioPlayArgs args)
        {
            for (var i = 0; i < _clipCache.Count; i++)
            {
                var (path, clip) = _clipCache[i];
                if (string.Compare(path, args.FilePath, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _clipCache.RemoveAt(i);
                    _clipCache.Insert(0, (path, clip));
                    _impl.Play(clip, _buddyFolder.BuddyId, args);
                    return true;
                }
            }
            return false;
        }

        // NOTE: 同じpathに対して短時間で連続で呼ばれうることには注意
        private async UniTask GetClipAndPlayAsync(AudioPlayArgs args, CancellationToken cancellationToken)
        {
            var fullPath = args.FilePath;
            var audioClip = await _impl.GetAudioClipAsync(fullPath);
            cancellationToken.ThrowIfCancellationRequested();
            
            // await中に別のタスクがキャッシュを追加した場合、重複して登録するのを避けたいので、キャッシュ側を消す
            for (var i = 0; i < _clipCache.Count; i++)
            {
                var (path, cacheClip) = _clipCache[i];
                if (string.Compare(path, fullPath, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _clipCache.RemoveAt(i);
                    UnityEngine.Object.Destroy(cacheClip);
                    break;
                }
            }

            _clipCache.Insert(0, (fullPath, audioClip));
            if (_clipCache.Count > ClipCacheCount)
            {
                var clipToRemove = _clipCache[ClipCacheCount];
                _clipCache.RemoveAt(ClipCacheCount);
                UnityEngine.Object.Destroy(clipToRemove.clip);
            }

            _impl.Play(audioClip, _buddyFolder.BuddyId, args);
        }

        internal void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            
            foreach (var (_, clip) in _clipCache)
            {
                UnityEngine.Object.Destroy(clip);
            }
            _clipCache.Clear();
        }
    }
}
