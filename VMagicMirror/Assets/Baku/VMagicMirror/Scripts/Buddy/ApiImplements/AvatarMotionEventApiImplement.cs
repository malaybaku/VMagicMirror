using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    public class AvatarMotionEventApiImplement : PresenterBase
    {
        private readonly ReactiveProperty<string> _leftHandTargetType = new();
        public IReadOnlyReactiveProperty<string> LeftHandTargetType => _leftHandTargetType;

        private readonly ReactiveProperty<string> _rightHandTargetType = new();
        public IReadOnlyReactiveProperty<string> RightHandTargetType => _rightHandTargetType;

        private readonly Subject<string> _keyboardKeyDown = new();
        /// <summary>
        /// アバターがキーボードのタイピング動作を開始すると発火する。
        /// - ユーザーがキーボードを押した場合でも、アバターが実際に動いてなければ発火しない。
        /// - キー名はごく一部(ENTERとか)だけが取得でき、大体のキーは空文字列になる (主にプライバシー観点がモチベ)
        /// TODO: 左手と右手どっちが動いたかくらいは教えたいかも？
        /// </summary>
        public IObservable<string> KeyboardKeyDown => _keyboardKeyDown;

        private readonly Subject<Unit> _touchPadMouseButtonDown = new();
        /// <summary>
        /// タッチパッドが表示された状態でアバターがクリック動作をしたとき、押し込みに対して発火する。どのボタンをクリックしたかは公開されない
        /// </summary>
        public IObservable<Unit> TouchPadMouseButtonDown => _touchPadMouseButtonDown;

        private readonly Subject<Unit> _penTabletMouseButtonDown = new();
        /// <summary>
        /// ペンタブレットが表示された状態でアバターがクリック動作をしたとき、押し込みに対して発火する。どのボタンをクリックしたかは公開されない
        /// </summary>
        public IObservable<Unit> PenTabletMouseButtonDown => _penTabletMouseButtonDown;

        private readonly Subject<(ReactedHand, GamepadKey)> _gamepadButtonDown = new();
        /// <summary>
        /// ゲームパッドが表示された状態で何らかのゲームパッドのボタンを押すと、押し込みに対して発火する。
        /// スティック入力に対しては発火しない
        /// </summary>
        public IObservable<(ReactedHand, GamepadKey)> GamepadButtonDown => _gamepadButtonDown;

        private readonly Subject<GamepadKey> _arcadeStickButtonDown = new();
        /// <summary>
        /// アーケードスティックが表示された状態でゲームパッドのボタンを押すと、押し込みに対して発火する。
        /// スティックには反応せず、かつアーケードスティック上で対応していないボタンを押した場合も反応しない
        /// </summary>
        public IObservable<GamepadKey> ArcadeStickButtonDown => _arcadeStickButtonDown;

        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly HandIKIntegrator _handIKIntegrator;
        private readonly CancellationTokenSource _cts = new();

        public AvatarMotionEventApiImplement(
            BodyMotionModeController bodyMotionModeController,
            HandIKIntegrator handIKIntegrator
            )
        {
            _bodyMotionModeController = bodyMotionModeController;
            _handIKIntegrator = handIKIntegrator;
        }

        public override void Initialize()
        {
            // TODO: 「clap中」を完全に無視したほうがいいかもしれない (空文字じゃなくてnullを入れてWhere句で弾く…とかのworkaroundを取ると行けそう)
            _handIKIntegrator.LeftTargetType
                .CombineLatest(
                    _bodyMotionModeController.MotionMode,
                    (type, mode) => mode != BodyMotionMode.Default ? HandTargetType.Unknown : type)
                .Select(ConvertHandTargetTypeToString)
                .DistinctUntilChanged()
                .Where(v => v != null)
                .Subscribe(targetTypeName => _leftHandTargetType.Value = targetTypeName)
                .AddTo(this);

            _handIKIntegrator.RightTargetType
                .CombineLatest(
                    _bodyMotionModeController.MotionMode,
                    (type, mode) => mode != BodyMotionMode.Default ? HandTargetType.Unknown : type)
                .Select(ConvertHandTargetTypeToString)
                .DistinctUntilChanged()
                .Where(v => v != null)
                .Subscribe(targetTypeName => _rightHandTargetType.Value = targetTypeName)
                .AddTo(this);
            
            InitializeAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid InitializeAsync(CancellationToken cancellationToken)
        {
            // NOTE: 1Frame目で絶対に起動したい…というほどの処理でもないので、単に待つことでHandIKIntegratorの初期化完了を待つ
            await UniTask.NextFrame(cancellationToken);
            
            // 基本的なイベント監視のアプローチ
            // - 個々のIKGeneratorの自己申告を見る
            // - 体の動作モード + 手IKのモードの実態を見ることで、そのモーションが本当に適用されてそうかを見に行く
            //   - こっちはイベントハンドラっぽい関数の中で随時やってる
            _handIKIntegrator.Typing.KeyDownMotionStarted
                .Subscribe(value => OnKeyboardKeyDownMotionStarted(value.hand, value.key))
                .AddTo(this);

            _handIKIntegrator.MouseMove.MouseClickMotionStarted
                .Where(CanRaiseMouseClickMotionStartEvent)
                .Subscribe(_ => _touchPadMouseButtonDown.OnNext(Unit.Default))
                .AddTo(this);
            
            _handIKIntegrator.PenTabletHand.MouseClickMotionStarted
                .Where(CanRaiseMouseClickMotionStartEvent)
                .Subscribe(_ => _penTabletMouseButtonDown.OnNext(Unit.Default))
                .AddTo(this);
            
            _handIKIntegrator.GamepadHand.ButtonDownMotionStarted
                .Subscribe(v => OnGamepadButtonDownMotionStarted(v.hand, v.key))
                .AddTo(this);

            _handIKIntegrator.ArcadeStickHand.ButtonDownMotionStarted
                .Subscribe(OnArcadeStickButtonDownMotionStarted)
                .AddTo(this);
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void OnKeyboardKeyDownMotionStarted(ReactedHand hand, string keyName)
        {
            if (hand == ReactedHand.None ||
                _bodyMotionModeController.MotionMode.Value != BodyMotionMode.Default)
            {
                return;
            }

            var actualTarget = hand == ReactedHand.Left
                ? _handIKIntegrator.LeftTargetType.Value
                : _handIKIntegrator.RightTargetType.Value;
            if (actualTarget != HandTargetType.Keyboard)
            {
                return;
            }

            // ENTERキーだけ教える & 他はキー名はヒミツ
            var eventKeyName =
                keyName.ToLower() == "enter" ? "enter" : "";
            _keyboardKeyDown.OnNext(eventKeyName);
        }

        private bool CanRaiseMouseClickMotionStartEvent(string eventName)
        {
            if (eventName is not 
                (MouseButtonEventNames.LDown or MouseButtonEventNames.RDown or MouseButtonEventNames.MDown)
               )
            {
                return false;
            }

            if (_bodyMotionModeController.MotionMode.Value != BodyMotionMode.Default)
            {
                return false;
            }
            return true;
        }

        private void OnGamepadButtonDownMotionStarted(ReactedHand hand, GamepadKey key)
        {
            if (_bodyMotionModeController.MotionMode.Value != BodyMotionMode.Default)
            {
                return;
            }

            if (!IsValidKey(key))
            {
                return;
            }

            _gamepadButtonDown.OnNext((hand, key));
        }
        
        private void OnArcadeStickButtonDownMotionStarted(GamepadKey key)
        {
            if (_bodyMotionModeController.MotionMode.Value != BodyMotionMode.Default)
            {
                return;
            }

            if (!IsValidKey(key))
            {
                return;
            }

            _arcadeStickButtonDown.OnNext(key);
        }

        private static bool IsValidKey(GamepadKey key) => key is not GamepadKey.Unknown;
        
        private static string ConvertHandTargetTypeToString(HandTargetType type)
        {
            return type switch
            {
                // 右手でのみ使う値がいくつかある
                HandTargetType.Mouse => "Mouse",
                HandTargetType.Presentation => "Presentation",
                HandTargetType.PenTablet => "PenTablet",
                // ここから下は両手で発生しうる
                HandTargetType.Keyboard => "Keyboard",
                HandTargetType.Gamepad => "Gamepad",
                HandTargetType.ArcadeStick => "ArcadeStick",
                HandTargetType.CarHandle => "CarHandle",
                HandTargetType.MidiController => "MidiController",
                HandTargetType.ImageBaseHand => "HandTracking",
                // NOTE: いちおう定義してるが、BodyMotionModeで弾いてるので通過しないはず…
                HandTargetType.AlwaysDown => "AlwaysDown",
                // HACK: clapはそもそも「遷移した」という事自体を無視してほしいので、空ではなくnullにしてRxの処理上で特別扱いする
                HandTargetType.ClapMotion => null,
                // NOTE: VMCPの適用中も「不明」くらいの扱いにする
                HandTargetType.VMCPReceiveResult => "",
                // 未知のも基本的には無視でOK
                _ => "",
            };
        }
    }
}
