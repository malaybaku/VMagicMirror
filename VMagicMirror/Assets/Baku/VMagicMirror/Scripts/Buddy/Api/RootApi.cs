using System;
using System.Threading;
using System.Threading.Tasks;
using VMagicMirror.Buddy;
using Cysharp.Threading.Tasks;

namespace Baku.VMagicMirror.Buddy.Api
{
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
            BuddyId buddyId,
            ApiImplementBundle apiImplementBundle)
        {
            _baseDir = baseDir;
            BuddyId = buddyId;
            BuddyFolder = BuddyFolder.Create(buddyId);
            _settingsRepository = apiImplementBundle.SettingsRepository;
            _logger = apiImplementBundle.Logger;
            _apiImplementBundle = apiImplementBundle;

            PropertyInternal = new PropertyApi(
                apiImplementBundle.BuddyPropertyRepository.GetOrCreate(buddyId)
            );
            AvatarLoadEventInternal = new AvatarLoadEventApi(apiImplementBundle.AvatarLoadApi);
            AvatarPose = new AvatarPoseApi(apiImplementBundle.AvatarPoseApi);
            AvatarFacialInternal = new AvatarFacialApi(apiImplementBundle.AvatarFacialApi);
            InputInternal = new InputApi(apiImplementBundle.InputApi);
            AudioInternal = new AudioApi(BuddyFolder, _logger, apiImplementBundle.AudioApi);
            DeviceLayout = new DeviceLayoutApi(apiImplementBundle.DeviceLayoutApi);
            Screen = new ScreenApi(apiImplementBundle.ScreenApi);

            _buddy3DInstanceCreator = apiImplementBundle.Buddy3DInstanceCreator;
            _spriteCanvas = apiImplementBundle.BuddySpriteCanvas;
            MainThreadContext = SynchronizationContext.Current;
            _gui = new GuiApi(apiImplementBundle.BuddyGuiCanvas);

            // NOTE: Directoryの生成はApiじゃなくてScriptCallerの責任であることに注意
            CacheDirectory = SpecialFiles.GetBuddyCacheDirectory(BuddyFolder);
        }

        internal void Dispose()
        {
            AvatarFacialInternal.Dispose();
            AudioInternal.Dispose();

            _gui.Dispose();
            
            _cts.Cancel();
            _cts.Dispose();
        }

        // NOTE: stringで引き回すとややこしいかもしれない == BuddyId型を作ったほうがいいかも
        /// <summary>
        /// NOTE: IdはRuntimeObjectやLayoutなどでBuddyを特定するときに用いる。
        /// </summary>
        internal BuddyId BuddyId { get; }
        /// <summary>
        /// NOTE: ログ情報の出力ではBuddyFolderを使う
        /// </summary>
        internal BuddyFolder BuddyFolder { get; }

        bool IRootApi.AvatarOutputFeatureEnabled => _settingsRepository.MainAvatarOutputActive.Value;

        // NOTE: Api.Log($"{Api}") のような(ミスで)書いたログに対して長過ぎる出力にならないようにしておく…というのが狙い
        public override string ToString() => nameof(IRootApi);

        public string BuddyDirectory => _baseDir;
        public string CacheDirectory { get; }

        internal void InvokeStarted() => Start?.Invoke();
        public event Action Start;

        internal void InvokeUpdated(float deltaTime) => Update?.Invoke(deltaTime);
        public event Action<float> Update;
        
        public SynchronizationContext MainThreadContext { get; }

        public void RunOnMainThread(Func<Task> task)
        {
            Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                await task();
            });
        }
        
        //TODO: FeatureLockについては、ここで記述されるプロパティ単位で
        //「丸ごとOK or 丸ごと塞がってる」となるのが分かりやすさ的には望ましい

        //NOTE: プロパティ形式で取得できるAPIは、スクリプトが最初に呼ばれる前に非nullで初期化されるのが期待値
        internal PropertyApi PropertyInternal { get; }
        public IProperty Property => PropertyInternal;
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
        
        internal AudioApi AudioInternal { get; }
        public IAudio Audio => AudioInternal;
        public IScreen Screen { get; }

        private readonly GuiApi _gui;
        public IGui Gui => _gui;
        
        public AppLanguage Language => _apiImplementBundle.LanguageSettingRepository.LanguageName switch
        {
            LanguageSettingRepository.LanguageNameJapanese => AppLanguage.Japanese,
            LanguageSettingRepository.LanguageNameEnglish => AppLanguage.English,
            _ => AppLanguage.Unknown,
        };

        public void Log(string value) => _logger.Log(BuddyFolder, value, BuddyLogLevel.Info);
        public void LogWarning(string value) => _logger.Log(BuddyFolder, value, BuddyLogLevel.Warning);
        public void LogError(string value) => _logger.Log(BuddyFolder, value, BuddyLogLevel.Error);

        public float Random() => UnityEngine.Random.value;

        public void InvokeDelay(Action func, float delaySeconds)
        {
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken: _cts.Token,
                    delayTiming: PlayerLoopTiming.LastPostLateUpdate
                    );
                ApiUtils.Try(BuddyFolder, _logger, () => func?.Invoke());
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
                    ApiUtils.Try(BuddyFolder, _logger, () => func?.Invoke());
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(intervalSeconds),
                        cancellationToken: _cts.Token,
                        delayTiming: PlayerLoopTiming.LastPostLateUpdate
                        );
                }
            });
        }
        
        public ISprite2D Create2DSprite()
        {
            var instance = _spriteCanvas.CreateSpriteInstance(
                BuddyFolder, _settingsRepository, _apiImplementBundle.AvatarFacialApi
                );
            var result = new Sprite2DApi(BuddyFolder, instance, _logger);
            return result;
        }

        public ISprite3D Create3DSprite()
        {
            var instance = _buddy3DInstanceCreator.CreateSprite3DInstance(BuddyFolder);
            return new Sprite3DApi(BuddyFolder, instance, _logger);
        }

        // NOTE: v4.0.0ではomitしてるがコンパイル出来たほうが嬉しいので、internalにして塞いでおく
        internal IGlb CreateGlb()
        {
            var instance = _buddy3DInstanceCreator.CreateGlbInstance(BuddyFolder);
            return new GlbApi(BuddyFolder, instance, _logger);
        }
        
        internal IVrm CreateVrm()
        {
            var instance = _buddy3DInstanceCreator.CreateVrmInstance(BuddyFolder);
            return new VrmApi(BuddyFolder, instance, _logger);
        }
        
        internal IVrmAnimation CreateVrmAnimation()
        {
            var instance = _buddy3DInstanceCreator.CreateVrmAnimationInstance(BuddyFolder);
            return new VrmAnimationApi(BuddyFolder, instance, _logger);
        }
    }
}
