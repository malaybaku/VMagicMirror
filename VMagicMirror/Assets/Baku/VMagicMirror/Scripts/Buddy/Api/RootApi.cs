using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Baku.VMagicMirror.Buddy.Api.Interface;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class RootApi : IRootApi
    {
        private Buddy3DInstanceCreator _buddy3DInstanceCreator;
        private readonly CancellationTokenSource _cts = new();

        //TODO: Layoutと同じくSpriteにもInstanceのレポジトリとUpdaterを作りたい
        private readonly List<Sprite2DApi> _sprites = new();
        internal IReadOnlyList<Sprite2DApi> Sprites => _sprites;

        private readonly List<BuddyVrmInstance> _vrms = new();
        internal IReadOnlyList<BuddyVrmInstance> Vrms => _vrms;
        
        private readonly List<BuddyGlbInstance> _glbs = new();

        private readonly List<BuddySprite3DInstance> _sprite3Ds = new();
        internal IReadOnlyList<BuddySprite3DInstance> Sprite3Ds => _sprite3Ds;
        
        private readonly Subject<Sprite2DApi> _spriteCreated = new();
        internal IObservable<Sprite2DApi> SpriteCreated => _spriteCreated;
        
        private readonly string _baseDir;
        private LogLevel _logLevel = LogLevel.Log;

        public RootApi(
            string baseDir,
            string buddyId,
            ApiImplementBundle apiImplementBundle)
        {
            _baseDir = baseDir;
            BuddyId = buddyId;
            Property = apiImplementBundle.BuddyPropertyRepository.Get(buddyId);
            AvatarPose = new AvatarPoseApi(apiImplementBundle.AvatarPoseApi);
            _avatarFacial = new AvatarFacialApi(apiImplementBundle.AvatarFacialApi);
            _audio = new AudioApi(baseDir, apiImplementBundle.AudioApi);
            DeviceLayout = new DeviceLayoutApi(apiImplementBundle.DeviceLayoutApi);
            Screen = new ScreenApi(apiImplementBundle.ScreenApi);

            _buddy3DInstanceCreator = apiImplementBundle.Buddy3DInstanceCreator;
            MainThreadContext = SynchronizationContext.Current;
        }

        internal void Dispose()
        {
            foreach (var sprite in _sprites)
            {
                sprite.Dispose();
            }
            _sprites.Clear();

            foreach (var vrm in _vrms)
            {
                vrm.Dispose();
            }
            _vrms.Clear();
            
            foreach (var glb in _glbs)
            {
                glb.Dispose();
            }
            _glbs.Clear();

            foreach (var sprite in _sprite3Ds)
            {
                sprite.Dispose();
            }
            _sprite3Ds.Clear();
            
            _avatarFacial.Dispose();
            _audio.Dispose();

            _cts.Cancel();
            _cts.Dispose();
        }

        internal string BuddyId { get; }

        public Action Start { get; set; }
        public Action<float> Update { get; set; }

        public SynchronizationContext MainThreadContext { get; }

        public async Task RunOnMainThread(Task task)
        {
            await UniTask.SwitchToMainThread();
            await task;
        }
        
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
            var instance = _buddy3DInstanceCreator.CreateSprite3DInstance();
            _sprite3Ds.Add(instance);
            return new Sprite3DApi(_baseDir, BuddyId, instance);
        }

        public IGlbApi CreateGlb()
        {
            var instance = _buddy3DInstanceCreator.CreateGlbInstance();
            _glbs.Add(instance);
            return new GlbApi(_baseDir, BuddyId, instance);
        }

        public IVrmApi CreateVrm()
        {
            var instance = _buddy3DInstanceCreator.CreateVrmInstance();
            _vrms.Add(instance);
            return new VrmApi(_baseDir, BuddyId, instance);
        }
    }
}
