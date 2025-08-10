using System.Linq;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddySpriteShadowSettingUpdater : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly BuddySpriteCanvas _canvas;
        private readonly BuddyRuntimeObjectRepository _repository;

        // NOTE: 「有効かつIntensity=0.65f」というのは MainCamera.prefab の中のShadowLightの設定に基づく。
        // また、影色は今のところRGBで可変しない仕様なのでけ取っていない
        private readonly ReactiveProperty<bool> _mainAvatarShadowEnabled = new(true);
        private readonly ReactiveProperty<bool> _buddySyncShadowEnabled = new(true);
        private readonly ReactiveProperty<float> _shadowIntensity = new(0.65f);

        [Inject]
        public BuddySpriteShadowSettingUpdater(
            IMessageReceiver receiver,
            BuddySpriteCanvas canvas,
            BuddyRuntimeObjectRepository repository)
        {
            _receiver = receiver;
            _canvas = canvas;
            _repository = repository;
        }
        
        public override void Initialize()
        {
            _receiver.BindBoolProperty(VmmCommands.ShadowEnable, _mainAvatarShadowEnabled);
            _receiver.BindBoolProperty(VmmCommands.BuddySetSyncShadowEnabled, _buddySyncShadowEnabled);
            _receiver.BindPercentageProperty(VmmCommands.ShadowIntensity, _shadowIntensity);
            
            _mainAvatarShadowEnabled
                .CombineLatest(
                    _buddySyncShadowEnabled,
                    _shadowIntensity, 
                    (_, __, ___) => GetShadowSettings())
                .DistinctUntilChanged()
                .Subscribe(value => SetShadowSettings(value.shadowEnabled, value.shadowIntensity))
                .AddTo(this);

            _canvas.SpriteCreated
                .Subscribe(instance =>
                {
                    var (shadowEnabled, shadowIntensity) = GetShadowSettings();
                    SetShadowSettings(instance, shadowEnabled, shadowIntensity);
                })
                .AddTo(this);
        }
        
        private (bool shadowEnabled, float shadowIntensity) GetShadowSettings()
        {
            var shadowEnabled = _mainAvatarShadowEnabled.Value && _buddySyncShadowEnabled.Value;
            var intensity = shadowEnabled ? _shadowIntensity.Value : 0f;
            return (shadowEnabled, intensity);
        }

        // NOTE: ここより下で3D系のInstanceがケアできてないが、後発で設定を増やしていくと3D版の処理も欲しくなりそう
        
        // 新規作成された単一のSpriteの影の設定を更新する
        private void SetShadowSettings(BuddySprite2DInstance target, bool enabled, float intensity)
        {
            target.SetShadowEnabled(enabled);
            target.SetShadowColor(new Color(0, 0, 0, intensity));
        }
        
        // 影の設定が更新されたときに呼ぶことで、全てのSpriteの影の設定を更新する
        private void SetShadowSettings(bool enabled, float intensity)
        {
            var shadowColor = new Color(0, 0, 0, intensity);
            foreach (var instance in _repository
                .GetRepositories()
                .SelectMany(r => r.Sprite2Ds))
            {
                instance.SetShadowEnabled(enabled);
                instance.SetShadowColor(shadowColor);
            }
        }
    }
}
