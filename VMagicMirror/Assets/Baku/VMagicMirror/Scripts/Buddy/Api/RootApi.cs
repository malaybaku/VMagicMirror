using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VMagicMirror.Buddy;
using Cysharp.Threading.Tasks;

namespace Baku.VMagicMirror.Buddy.Api
{
    // TODO: 一部のAPIの戻り値は _settingsRepository に基づいて制限したい
    
    public class RootApi : IRootApi
    {
        private readonly BuddySettingsRepository _settingsRepository;
        private readonly BuddyLogger _logger;
        private readonly Buddy3DInstanceCreator _buddy3DInstanceCreator;
        private readonly BuddySpriteCanvas _spriteCanvas;
        // NOTE: 他のreadonlyフィールドを↓から導出されるプロパティに変更してもOK
        private readonly ApiImplementBundle _apiImplementBundle;

        private readonly CancellationTokenSource _cts = new();
        
        private readonly string _baseDir;

        public RootApi(
            string baseDir,
            string buddyId,
            ApiImplementBundle apiImplementBundle)
        {
            _baseDir = baseDir;
            BuddyId = buddyId;
            _settingsRepository = apiImplementBundle.SettingsRepository;
            _logger = apiImplementBundle.Logger;
            _apiImplementBundle = apiImplementBundle;
            
            Property = apiImplementBundle.BuddyPropertyRepository.Get(buddyId);
            AvatarLoadEventInternal = new AvatarLoadEventApi(apiImplementBundle.AvatarLoadApi);
            AvatarPose = new AvatarPoseApi(apiImplementBundle.AvatarPoseApi);
            AvatarFacialInternal = new AvatarFacialApi(apiImplementBundle.AvatarFacialApi);
            InputInternal = new InputApi(apiImplementBundle.InputApi);
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
            AvatarFacialInternal.Dispose();
            _audio.Dispose();

            _gui.Dispose();
            
            _cts.Cancel();
            _cts.Dispose();
        }

        internal string BuddyId { get; }

        bool IRootApi.AvatarOutputFeatureEnabled => _settingsRepository.MainAvatarOutputActive.Value;

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

        public void Log(string value) => _logger.Log(BuddyId, value, BuddyLogLevel.Info);
        public void LogWarning(string value) => _logger.Log(BuddyId, value, BuddyLogLevel.Warning);
        public void LogError(string value) => _logger.Log(BuddyId, value, BuddyLogLevel.Error);

        public float Random() => UnityEngine.Random.value;

        public void InvokeDelay(Action func, float delaySeconds)
        {
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken: _cts.Token,
                    delayTiming: PlayerLoopTiming.LastPostLateUpdate
                    );
                ApiUtils.Try(BuddyId, _logger, () => func?.Invoke());
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
                    ApiUtils.Try(BuddyId, _logger, () => func?.Invoke());
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
            var instance = _spriteCanvas.CreateSpriteInstance(
                BuddyId, _settingsRepository, _apiImplementBundle.AvatarFacialApi
                );
            var result = new Sprite2DApi(_baseDir, instance, _logger);
            return result;
        }

        public ISprite3D Create3DSprite()
        {
            var instance = _buddy3DInstanceCreator.CreateSprite3DInstance(BuddyId);
            return new Sprite3DApi(_baseDir, instance, _logger);
        }

        public IGlb CreateGlb()
        {
            var instance = _buddy3DInstanceCreator.CreateGlbInstance(BuddyId);
            return new GlbApi(_baseDir, instance, _logger);
        }

        public IVrm CreateVrm()
        {
            var instance = _buddy3DInstanceCreator.CreateVrmInstance(BuddyId);
            return new VrmApi(_baseDir, instance);
        }

        public IVrmAnimation CreateVrmAnimation()
        {
            var instance = _buddy3DInstanceCreator.CreateVrmAnimationInstance(BuddyId);
            return new VrmAnimationApi(_baseDir, instance);
        }
    }
}
