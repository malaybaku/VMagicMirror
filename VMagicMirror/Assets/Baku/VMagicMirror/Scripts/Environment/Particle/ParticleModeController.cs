using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ParticleModeController : PresenterBase
    {
        private const int InvalidTypingEffectIndex = -1;
        private const int MangaTypingEffectIndex = 4;

        private readonly IMessageReceiver _receiver;
        private readonly DeviceVisibilityRepository _deviceVisibilityRepository;
        private readonly BodyMotionModeController _bodyMotionModeController;
        
        [Inject]
        public ParticleModeController(
            IMessageReceiver receiver,
            DeviceVisibilityRepository deviceVisibilityRepository,
            BodyMotionModeController bodyMotionModeController
            )
        {
            _receiver = receiver;
            _deviceVisibilityRepository = deviceVisibilityRepository;
            _bodyMotionModeController = bodyMotionModeController;
        }

        private readonly ReactiveProperty<int> _rawIndex = new(InvalidTypingEffectIndex);
        private readonly ReactiveProperty<GamepadMotionModes> _gamepadMotionMode = new(GamepadMotionModes.Gamepad);

        private readonly ReactiveProperty<bool> _mangaEffectActive = new(false);
        public ReadOnlyReactiveProperty<bool> MangaEffectActive => _mangaEffectActive;
        
        // 下記3つはそれぞれデバイスが表示されてて実際に使ってそうな場合のみ、-1以外の値を取る
        private readonly ReactiveProperty<int> _keyboardParticleIndex = new(-1);
        public ReadOnlyReactiveProperty<int> KeyboardParticleIndex => _keyboardParticleIndex;

        private readonly ReactiveProperty<int> _midiParticleIndex = new(-1);
        public ReadOnlyReactiveProperty<int> MidiParticleIndex => _keyboardParticleIndex;

        private readonly ReactiveProperty<int> _arcadeStickParticleIndex = new(-1);
        public ReadOnlyReactiveProperty<int> ArcadeStickParticleIndex => _keyboardParticleIndex;

        private ReadOnlyReactiveProperty<BodyMotionMode> MotionMode => _bodyMotionModeController.MotionMode;
        
        public override void Initialize()
        {
            _receiver.BindIntProperty(VmmCommands.SetKeyboardTypingEffectType, _rawIndex);
            _receiver.BindEnumProperty(VmmCommands.SetGamepadMotionMode, _gamepadMotionMode);

            // TODO: 実際にオブジェクトが見えてるかどうかの判定としては下記は微妙に不十分であることに注意
            // ちゃんとやるならKeyboardVisibilityUpdaterとかの計算結果を見に行ったほうがよい (MIDI等も同様)
            _rawIndex.CombineLatest(
                    _deviceVisibilityRepository.HidVisible,
                    MotionMode,
                    (index, hidVisible, motionMode) => hidVisible && motionMode is not BodyMotionMode.GameInputLocomotion
                        ? GetNormalParticleIndex(index)
                        : InvalidTypingEffectIndex
                )
                .Subscribe(index => _keyboardParticleIndex.Value = index)
                .AddTo(this);

            _rawIndex.CombineLatest(
                    _deviceVisibilityRepository.MidiControllerVisible,
                    MotionMode,
                    (index, midiVisible, motionMode) => midiVisible && motionMode is not BodyMotionMode.GameInputLocomotion
                        ? GetNormalParticleIndex(index)
                        : InvalidTypingEffectIndex
                )
                .Subscribe(index => _midiParticleIndex.Value = index)
                .AddTo(this);

            _rawIndex.CombineLatest(
                    _deviceVisibilityRepository.GamepadVisible,
                    _gamepadMotionMode,
                    MotionMode,
                    (index, gamepadVisible, gamepadMotionMode, motionMode) =>
                        gamepadVisible && gamepadMotionMode is GamepadMotionModes.ArcadeStick && motionMode is not BodyMotionMode.GameInputLocomotion
                            ? GetNormalParticleIndex(index)
                            : InvalidTypingEffectIndex
                )
                .Subscribe(index => _arcadeStickParticleIndex.Value = index)
                .AddTo(this);

            _rawIndex.CombineLatest(
                    MotionMode,
                    (index, motionMode) => motionMode is not BodyMotionMode.GameInputLocomotion &&
                        index == MangaTypingEffectIndex
                )
                .Subscribe(value => _mangaEffectActive.Value = value)
                .AddTo(this);
        }

        // NOTE: マンガ風エフェクトのインデックスは-1に変換される(=ParticleSystem的なパーティクルとしては何も出さない)
        private static int GetNormalParticleIndex(int index) => index switch
        {
            >= -1 and < MangaTypingEffectIndex => index,
            MangaTypingEffectIndex => -1,
            _ => -1,
        };
    }
}
