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
        private readonly Buddy3DInstanceCreator _buddy3DInstanceCreator;
        private readonly BuddySpriteCanvas _spriteCanvas;

        private readonly CancellationTokenSource _cts = new();

        //TODO: Layoutと同じくSpriteにもInstanceのレポジトリとUpdaterを作りたい
        private readonly List<Sprite2DApi> _sprite2Ds = new();
        internal IReadOnlyList<Sprite2DApi> Sprite2Ds => _sprite2Ds;

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
            AvatarLoadEventInternal = new AvatarLoadEventApi(apiImplementBundle.AvatarLoadApi);
            AvatarPose = new AvatarPoseApi(apiImplementBundle.AvatarPoseApi);
            AvatarFacialInternal = new AvatarFacialApi(apiImplementBundle.AvatarFacialApi);
            InputInternal = new(apiImplementBundle.InputApi);
            _audio = new AudioApi(baseDir, apiImplementBundle.AudioApi);
            DeviceLayout = new DeviceLayoutApi(apiImplementBundle.DeviceLayoutApi);
            Screen = new ScreenApi(apiImplementBundle.ScreenApi);

            _buddy3DInstanceCreator = apiImplementBundle.Buddy3DInstanceCreator;
            _spriteCanvas = apiImplementBundle.BuddySpriteCanvas;
            MainThreadContext = SynchronizationContext.Current;
            _gui = new GuiApi(apiImplementBundle.BuddyGuiCanvas);
        }

        internal void Dispose()
        {
            _sprite2Ds.Clear();
            _vrms.Clear();
            _glbs.Clear();
            _sprite3Ds.Clear();
            
            AvatarFacialInternal.Dispose();
            _audio.Dispose();

            _gui.Dispose();
            
            _cts.Cancel();
            _cts.Dispose();
        }

        internal string BuddyId { get; }

        internal void InvokeStarted() => Start?.Invoke();
        public event Action Start;

        internal void InvokeUpdated(float deltaTime) => Update?.Invoke(deltaTime);
        public event Action<float> Update;
        
        public SynchronizationContext MainThreadContext { get; }

        public async Task RunOnMainThread(Task task)
        {
            await UniTask.SwitchToMainThread();
            await task;
        }
        
        //TODO: FeatureLockについては、ここで記述されるプロパティ単位で
        //「丸ごとOK or 丸ごと塞がってる」となるのが分かりやすさ的には望ましい

        //NOTE: プロパティ形式で取得できるAPIは、スクリプトが最初に呼ばれる前に非nullで初期化されるのが期待値
        public IProperty Property { get; }
        public IManifestTransforms Transforms { get; internal set; }
        public IDeviceLayout DeviceLayout { get; }
        
        // NOTE: このへん `api.Avatar.MotionEvent` みたく書けたほうが字面がいいから修正しそう

        internal AvatarLoadEventApi AvatarLoadEventInternal { get; }
        public IAvatarLoadEvent AvatarLoadEvent => AvatarLoadEventInternal;
        public IAvatarPose AvatarPose { get; }

        internal AvatarMotionEventApi AvatarMotionEventInternal { get; } = new();
        public IAvatarMotionEvent AvatarMotionEvent => AvatarMotionEventInternal;

        internal AvatarFacialApi AvatarFacialInternal { get; }
        public IAvatarFacial AvatarFacial => AvatarFacialInternal;

        internal InputApi InputInternal { get; }
        public IInput Input => InputInternal;
        
        private readonly AudioApi _audio;
        public IAudio Audio => _audio;
        public IScreen Screen { get; }

        private readonly GuiApi _gui;
        public IGui Gui => _gui;
        
        // TODO: 実際に選択中の言語を返す
        public AppLanguage Language => throw new NotImplementedException();
        
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
        
        public ISprite2D Create2DSprite()
        {
            var instance = _spriteCanvas.CreateSpriteInstance(BuddyId);
            var result = new Sprite2DApi(_baseDir, BuddyId, instance);
            _sprite2Ds.Add(result);
            _spriteCreated.OnNext(result);
            return result;
        }

        public ISprite3D Create3DSprite()
        {
            var instance = _buddy3DInstanceCreator.CreateSprite3DInstance(BuddyId);
            _sprite3Ds.Add(instance);
            return new Sprite3DApi(_baseDir, BuddyId, instance);
        }

        public IGlb CreateGlb()
        {
            var instance = _buddy3DInstanceCreator.CreateGlbInstance(BuddyId);
            _glbs.Add(instance);
            return new GlbApi(_baseDir, BuddyId, instance);
        }

        public IVrm CreateVrm()
        {
            var instance = _buddy3DInstanceCreator.CreateVrmInstance(BuddyId);
            _vrms.Add(instance);
            return new VrmApi(_baseDir, BuddyId, instance);
        }
    }
}
