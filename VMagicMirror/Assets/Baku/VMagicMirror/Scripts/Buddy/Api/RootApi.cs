using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Baku.VMagicMirror.Buddy.Api.Interface;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class RootApi : IRootApi
    {
        private readonly CancellationTokenSource _cts = new();

        //TODO: Layoutと同じくSpriteにもInstanceのレポジトリとUpdaterを作りたい
        private readonly List<Sprite2DApi> _sprites = new();
        internal IReadOnlyList<Sprite2DApi> Sprites => _sprites;

        private readonly Subject<Sprite2DApi> _spriteCreated = new();
        internal IObservable<Sprite2DApi> SpriteCreated => _spriteCreated;
        
        private readonly string _baseDir;
        private LogLevel _logLevel = LogLevel.Log;

        public RootApi(string baseDir, string buddyId, ApiImplementBundle apiImplementBundle)
        {
            _baseDir = baseDir;
            BuddyId = buddyId;
            Property = apiImplementBundle.BuddyPropertyRepository.Get(buddyId);
            AvatarPose = new AvatarPoseApi(apiImplementBundle.AvatarPoseApi);
            _avatarFacial = new AvatarFacialApi(apiImplementBundle.AvatarFacialApi);
            _audio = new AudioApi(baseDir, apiImplementBundle.AudioApi);
            DeviceLayout = new DeviceLayoutApi(apiImplementBundle.DeviceLayoutApi);
            Screen = new ScreenApi(apiImplementBundle.ScreenApi);
        }

        internal void Dispose()
        {
            foreach (var sprite in _sprites)
            {
                sprite.Dispose();
            }
            _sprites.Clear();

            _avatarFacial.Dispose();
            _audio.Dispose();

            _cts.Cancel();
            _cts.Dispose();
        }

        internal string BuddyId { get; }

        public Action Start { get; set; }
        public Action<float> Update { get; set; }

        //TODO: FeatureLockについては、ここで記述されるプロパティ単位で
        //「丸ごとOK or 丸ごと塞がってる」となるのが分かりやすさ的には望ましい

        //NOTE: プロパティ形式で取得できるAPIは、スクリプトが最初に呼ばれる前に非nullで初期化されるのが期待値
        public IPropertyApi Property { get; }
        public ITransformsApi Transforms { get; internal set; }
        public IDeviceLayoutApi DeviceLayout { get; }
        
        // NOTE: このへん `api.Avatar.MotionEvent` みたく書けたほうが字面がいいから修正しそう
        public IAvatarLoadEventApi AvatarLoadEvent { get; } = new AvatarLoadEventApi();
        public IAvatarPoseApi AvatarPose { get; }
        public IAvatarMotionEventApi AvatarMotionEvent { get; } = new AvatarMotionEventApi();
        private readonly AvatarFacialApi _avatarFacial;
        public IAvatarFacialApi AvatarFacial => _avatarFacial;
        
        private readonly AudioApi _audio;
        public IAudioApi Audio => _audio;
        public IScreenApi Screen { get; }
        
        public void Log(string value)
        {
            if ((int)_logLevel >= (int)LogLevel.Log)
            {
                BuddyLogger.Instance.Log(BuddyId, GetLogHeader(LogLevel.Log) + value);
            }
        }

        public void LogWarning(string value)
        {
            if ((int)_logLevel >= (int)LogLevel.Warning)
            {
                BuddyLogger.Instance.Log(BuddyId, GetLogHeader(LogLevel.Warning) + value);
            }
        }

        public void LogError(string value)
        {
            if ((int)_logLevel >= (int)LogLevel.Error)
            {
                BuddyLogger.Instance.Log(BuddyId, GetLogHeader(LogLevel.Error) + value);
            }
        }

        public void SetLogLevel(LogLevel level) => _logLevel = level;

        private string GetLogHeader(LogLevel level) => $"[{level}]";
        
        public float Random() => UnityEngine.Random.value;

        public void InvokeDelay(Action func, float delaySeconds)
        {
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken: _cts.Token,
                    delayTiming: PlayerLoopTiming.LastPostLateUpdate
                    );
                ApiUtils.Try(BuddyId, () => func?.Invoke());
            });
        }

        public void InvokeInterval(Action func, float intervalSeconds)
            => InvokeInterval(func, intervalSeconds, 0f);

        public void InvokeInterval(Action func, float intervalSeconds, float firstDelay)
        {
            UniTask.Void(async () =>
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(firstDelay),
                    cancellationToken: _cts.Token,
                    delayTiming: PlayerLoopTiming.LastPostLateUpdate
                    );
                while (!_cts.IsCancellationRequested)
                {
                    ApiUtils.Try(BuddyId, () => func?.Invoke());
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(intervalSeconds),
                        cancellationToken: _cts.Token,
                        delayTiming: PlayerLoopTiming.LastPostLateUpdate
                        );
                }
            });
        }

        public bool ValidateFilePath(string path)
        {
            var fullPath = Path.Combine(_baseDir, path);
            return
                ApiUtils.IsChildDirectory(SpecialFiles.BuddyRootDirectory, fullPath) &&
                File.Exists(path);
        }
        
        public ISprite2DApi Create2DSprite()
        {
            var result = new Sprite2DApi(_baseDir, BuddyId);
            _sprites.Add(result);
            _spriteCreated.OnNext(result);
            return result;
        }

        public ISprite3DApi Create3DSprite()
        {
            throw new NotImplementedException();
        }

        public IGlbApi CreateGlb()
        {
            throw new NotImplementedException();
        }

        public IVrmApi CreateVrm()
        {
            throw new NotImplementedException();
        }
    }
}
