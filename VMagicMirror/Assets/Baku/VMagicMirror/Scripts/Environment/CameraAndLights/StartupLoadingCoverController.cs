using System;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class StartupLoadingCoverController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly IVRMLoadable _vrmLoadable;
        private readonly StartupLoadingCover _cover;
        private bool _fadeOutCalled;
        private bool _delayFadeout;

        //アプリ起動後に1回でもファイルからのVRMロードを試みた処理が終わってれば、その成否によらずtrue
        private readonly ReactiveProperty<bool> _localVrmLoadEndedAtLeastOnce = new ReactiveProperty<bool>();

        [Inject]
        public StartupLoadingCoverController(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable,
            StartupLoadingCover cover)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _cover = cover;
        }
        
        public override void Initialize()
        {
            //NOTE: このクラスの仕事かというと微妙だが、ビルド設定を吐かせておく(不具合調査上都合がよいので)
            LogOutput.Instance.Write(
                $"Environment: Feature Lock = {FeatureLocker.IsFeatureLocked}, Dev = {SpecialFiles.UseDevFolder}"
            );

            _vrmLoadable.LocalVrmLoadEnded += () => _localVrmLoadEndedAtLeastOnce.Value = true;

            _receiver.AssignCommandHandler(
                VmmCommands.StartupEnded,
                _ => FadeOutLoadingCover()
            );
            
            _receiver.AssignCommandHandler(
                VmmCommands.OpenVrm,
                _ =>
                {
                    if (!_fadeOutCalled)
                    {
                        _delayFadeout = true;
                    }
                });

            // 透過背景の場合は蓋絵が残るとかえって邪魔なため、早めに外す
            var settingReader = new DirectSettingFileReader();
            settingReader.Load();
            if (settingReader.TransparentBackground)
            {
                FadeOutLoadingCoverImmediate();
            }
        }

        private void FadeOutLoadingCover()
        {
            if (_fadeOutCalled)
            {
                return;
            }
            _fadeOutCalled = true;

            if (_delayFadeout)
            {
                //モデルがロード中かもしれないので、ロードが終わるまでは遅延させる
                //NOTE: Delayをつけるのは「どうせ遅延させるし物理演算が落ち着くまで待ちたい…」という意図に基づく
                _cover.SetModelLoadIndication();
                _localVrmLoadEndedAtLeastOnce
                    .Where(v => v)
                    .First()
                    .Delay(TimeSpan.FromSeconds(1f))
                    .Subscribe(_ => _cover.FadeOutAndDestroySelf())
                    .AddTo(this);
            }
            else
            {
                _cover.FadeOutAndDestroySelf();
            }
        }

        private void FadeOutLoadingCoverImmediate()
        {
            if (_fadeOutCalled)
            {
                return;
            }
            _fadeOutCalled = true;
            _cover.FadeOutImmediate();
        }
    }
}
