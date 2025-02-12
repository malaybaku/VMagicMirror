using System;
using Baku.VMagicMirror.IK;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    public class AvatarMotionEventApiImplement : PresenterBase
    {
        private readonly Subject<string> _keyboardKeyDown = new();
        /// <summary>
        /// アバターがキーボードのタイピング動作を開始すると発火する。
        /// - ユーザーがキーボードを押した場合でも、アバターが実際に動いてなければ発火しない。
        /// - キー名はごく一部(ENTERとか)だけが取得でき、大体のキーは空文字列になる (主にプライバシー観点がモチベ)
        /// </summary>
        public IObservable<string> KeyboardKeyDown => _keyboardKeyDown;

        private readonly Subject<Unit> _touchPadMouseButtonDown = new();
        /// <summary>
        /// タッチパッドが表示sあれた状態でアバターがクリック動作をしたとき、押し込みに対して発火する。どのボタンをクリックしたかは公開されない
        /// </summary>
        public IObservable<Unit> TouchPadMouseButtonDown => _touchPadMouseButtonDown;
        
        // 基本的なイベント監視のアプローチ
        // - 個々のIKGeneratorの自己申告を見る
        // - 体の動作モード + 手IKのモードの実態を見ることで、そのモーションが本当に適用されてそうかを見に行く
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly HandIKIntegrator _handIKIntegrator;
        // NOTE: 監視対象が多すぎてコードのスケールがしんどい説ありそう～
        private readonly TypingHandIKGenerator _typingHandIKGenerator;
        private readonly MouseMoveHandIKGenerator _mouseMoveHandIKGenerator;
        private readonly PenTabletHandIKGenerator _penTabletHandIKGenerator;

        public AvatarMotionEventApiImplement(
            BodyMotionModeController bodyMotionModeController,
            HandIKIntegrator handIKIntegrator,
            TypingHandIKGenerator typingHandIKGenerator,
            MouseMoveHandIKGenerator mouseMoveHandIKGenerator,
            PenTabletHandIKGenerator penTabletHandIKGenerator
            )
        {
            _bodyMotionModeController = bodyMotionModeController;
            _handIKIntegrator = handIKIntegrator;
            _typingHandIKGenerator = typingHandIKGenerator;
            _mouseMoveHandIKGenerator = mouseMoveHandIKGenerator;
            _penTabletHandIKGenerator = penTabletHandIKGenerator;
        }

        public override void Initialize()
        {
            _typingHandIKGenerator.KeyDownMotionStarted
                .Subscribe(value => OnKeyboardKeyDownMotionStarted(value.hand, value.key))
                .AddTo(this);

            _mouseMoveHandIKGenerator.MouseClickMotionStarted
                .Subscribe(OnMouseClickMotionStarted)
                .AddTo(this);
            
            _penTabletHandIKGenerator.MouseClickMotionStarted
                .Subscribe(OnMouseClickMotionStarted)
                .AddTo(this);
        }


        public string GetRightHandTarget()
        {
            if (_bodyMotionModeController.MotionMode.Value is not BodyMotionMode.Default)
            {
                return "";
            }

            return ConvertHandTargetTypeToString(_handIKIntegrator.RightTargetType.Value);
        }

        public string GetLeftHandTarget()
        {
            if (_bodyMotionModeController.MotionMode.Value is not BodyMotionMode.Default)
            {
                return "";
            }

            return ConvertHandTargetTypeToString(_handIKIntegrator.LeftTargetType.Value);
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

        private void OnMouseClickMotionStarted(string eventName)
        {
            if (eventName is not 
                (MouseButtonEventNames.LDown or MouseButtonEventNames.RDown or MouseButtonEventNames.MDown)
                )
            {
                return;
            }

            if (_bodyMotionModeController.MotionMode.Value != BodyMotionMode.Default)
            {
                return;
            }

            _touchPadMouseButtonDown.OnNext(Unit.Default);
        }

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
                // NOTE: ClapMotionはWord to Motionの一種として発動するものであり、かなり特殊なので不明値扱い
                HandTargetType.ClapMotion => "",
                // NOTE: VMCPの適用中も「不明」くらいの扱いにする
                HandTargetType.VMCPReceiveResult => "",
                // 未知のも基本的には無視でOK
                _ => "",
            };
        }
    }
}
