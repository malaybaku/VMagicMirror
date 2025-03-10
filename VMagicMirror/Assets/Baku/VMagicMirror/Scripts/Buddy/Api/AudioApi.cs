using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly string _baseDir;
        private readonly AudioApiImplement _impl;
        private readonly CancellationTokenSource _cts = new();
        
        internal AudioApi(string baseDir, AudioApiImplement impl)
        {
            _baseDir = baseDir;
            _impl = impl;
        }

        public void Play(string path)
        {
            var fullPath = Path.Combine(_baseDir, path);
            if (!ApiUtils.IsInBuddyDirectory(fullPath))
            {
                LogOutput.Instance.Write("Specified path is not in Buddy directory: " + fullPath);
                return;
            }

            if (TryPlayFromCache(fullPath))
            {
                return;
            }

            GetClipAndPlayAsync(fullPath, _cts.Token).Forget();
        }

        public void Play(byte[] data)
        {
            // TODO: バイナリからAudioClipを取得する処理が欲しい
            // というか、使い回しを考えるとAPIシグネチャがアカンのでは…？
            throw new NotImplementedException();
        }
        
        private bool TryPlayFromCache(string fullPath)
        {
            for (var i = 0; i < _clipCache.Count; i++)
            {
                var (path, clip) = _clipCache[i];
                if (string.Compare(path, fullPath, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _clipCache.RemoveAt(i);
                    _clipCache.Insert(0, (path, clip));
                    _impl.Play(clip);
                    return true;
                }
            }
            return false;
        }

        // NOTE: 同じpathに対して短時間で連続で呼ばれうることには注意
        private async UniTask GetClipAndPlayAsync(string fullPath, CancellationToken cancellationToken)
        {
            var audioClip = await _impl.GetAudioClipAsync(fullPath);
            cancellationToken.ThrowIfCancellationRequested();
            
            // await中に別のタスクがキャッシュを追加した場合、重複して登録するのを避ける
            for (var i = 0; i < _clipCache.Count; i++)
            {
                var (path, _) = _clipCache[i];
                if (string.Compare(path, fullPath, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _clipCache.RemoveAt(i);
                    break;
                }
            }

            _clipCache.Insert(0, (fullPath, audioClip));
            if (_clipCache.Count > ClipCacheCount)
            {
                _clipCache.RemoveAt(ClipCacheCount);
            }

            _impl.Play(audioClip);
        }

        internal void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
