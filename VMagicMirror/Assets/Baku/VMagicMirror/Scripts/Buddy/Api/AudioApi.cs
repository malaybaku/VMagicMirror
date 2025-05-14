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
        private readonly AudioApiImplement _impl;
        private readonly CancellationTokenSource _cts = new();
        
        internal AudioApi(BuddyFolder buddyFolder, AudioApiImplement impl)
        {
            _buddyFolder = buddyFolder;
            _impl = impl;
        }

        public void Play(string path, float volume = 1.0f, float pitch = 1.0f)
        {
            var fullPath = ApiUtils.GetAssetFullPath(_buddyFolder, path);
            var args = new AudioPlayArgs(fullPath, volume, pitch);
            
            if (TryPlayFromCache(args))
            {
                return;
            }

            GetClipAndPlayAsync(args, _cts.Token).Forget();
        }

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
                    _impl.Play(clip, args);
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

            _impl.Play(audioClip, args);
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
