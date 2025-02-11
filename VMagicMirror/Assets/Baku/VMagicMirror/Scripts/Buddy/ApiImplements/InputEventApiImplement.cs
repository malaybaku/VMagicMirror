using System;
using Baku.VMagicMirror.IK;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    public class InputEventApiImplement : PresenterBase
    {
        private readonly Subject<string> _keyboardKeyDown = new();
        /// <summary>
        /// アバターがキーボードのタイピング動作を開始すると発火する。
        /// - ユーザーがキーボードを押した場合でも、アバターが実際に動いてなければ発火しない。
        /// - キー名はごく一部(ENTERとか)だけが取得でき、大体のキーは空文字列になる (主にプライバシー観点がモチベ)
        /// </summary>
        public IObservable<string> KeyboardKeyDown => _keyboardKeyDown;

        // 基本的なイベント監視のアプローチ
        // - 個々のIKGeneratorの自己申告を見る
        // - 体の動作モード + 手IKのモードの実態を見ることで、そのモーションが本当に適用されてそうかを見に行く
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly HandIKIntegrator _handIKIntegrator;
        // NOTE: 監視対象が多すぎてコードのスケールがしんどい説ありそう～
        private readonly TypingHandIKGenerator _typingHandIKGenerator;

        public InputEventApiImplement(
            BodyMotionModeController bodyMotionModeController,
            HandIKIntegrator handIKIntegrator,
            TypingHandIKGenerator typingHandIKGenerator
            )
        {
            _bodyMotionModeController = bodyMotionModeController;
            _handIKIntegrator = handIKIntegrator;
            _typingHandIKGenerator = typingHandIKGenerator;
        }

        public override void Initialize()
        {
            _typingHandIKGenerator.KeyDownMotionStarted
                .Subscribe(value => OnKeyboardKeyDownMotionStarted(value.hand, value.key))
                .AddTo(this);
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
    }
}
