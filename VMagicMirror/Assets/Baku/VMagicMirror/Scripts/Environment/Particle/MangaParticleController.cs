using System;
using System.IO;
using R3;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// マンガ風パーティクルをなんか頑張って制御するやつ
    /// </summary>
    /// <remarks>
    /// このクラスが面倒を見るとこ(色々ある)
    /// - エフェクトの実行の最短間隔の保証
    /// - エフェクトの発生頻度とか出現位置のランダム化
    /// - いろんな種類のエフェクトが同時に出すぎることの防止
    /// - app起動時に画像があれば差し替えるやつ
    /// </remarks>
    public class MangaParticleController : PresenterBase
    {
        private const float MouseClickProbability = 0.4f;
        private const float KeyDownProbability = 0.5f;
        private const float EnterKeyDownProbability = 0.7f;

        private const float GamepadButtonDownProbability = 0.5f;
        // NOTE: ゲームパッドだけ発火回数がだいぶ違うはずなので下げている。あんまり意味ない可能性もあるが…
        private const float GamepadButtonStickProbability = 0.2f;
        
        
        private readonly MangaParticleView _view;
        private readonly ParticleModeController _particleModeController;
        private readonly BodyMotionModeController _motionModeController;
        private readonly IKeyMouseEventSource _keyMouse;
        private readonly XInputGamePad _gamePad;

        private readonly Subject<Unit> _runKeyDownParticle = new();
        private readonly Subject<Unit> _runEnterKeyDownParticle = new();

        private readonly Subject<Unit> _runMouseClickParticle = new();
        private readonly Subject<Unit> _runMouseMoveParticle = new();

        private readonly Subject<Unit> _runGamepadButtonParticle = new();
        private readonly Subject<Unit> _runGamepadStickParticle = new();
        
        [Inject]
        public MangaParticleController(
            MangaParticleView view,
            ParticleModeController particleModeController,
            BodyMotionModeController motionModeController,
            IKeyMouseEventSource keyMouse,
            XInputGamePad gamePad)
        {
            _view = view;
            _particleModeController = particleModeController;
            _motionModeController = motionModeController;
            _keyMouse = keyMouse;
            _gamePad = gamePad;
        }

        private bool _isActive;
        
        public override void Initialize()
        {
            ReplaceTextureIfExists();
            
            _particleModeController.MangaEffectActive
                .Subscribe(active =>
                {
                    _isActive = active;
                    _view.SetActive(active);
                })
                .AddTo(this);

            // 入力 to パーティクルの実行
            _keyMouse.KeyDown
                .Subscribe(OnKeyDown)
                .AddTo(this);

            _keyMouse.MouseButton
                .Subscribe(OnMouseClick)
                .AddTo(this);

            _gamePad.ButtonUpDown
                .Subscribe(OnGamepadButton)
                .AddTo(this);

            //左右スティックどっちも見るべきかもしれない
            _gamePad.LeftStickPosition
                .Subscribe(OnGamepadStick)
                .AddTo(this);
            
            // パーティクルの実行に対してThrottleとか併発防止のフィルタリングがかかるやつ。
            // - とりあえず併発防止はせずにThrottleだけやっている
            // - Throttleの長さはパーティクルごとに違ってよいことに注意

            _runKeyDownParticle
                .Where(_ => _motionModeController.MotionMode.CurrentValue == BodyMotionMode.Default)
                .ThrottleFirst(TimeSpan.FromSeconds(0.25f))
                .Subscribe(_ => _view.RunNormalKeyDownEffect())
                .AddTo(this);

            _runEnterKeyDownParticle
                .Where(_ => _motionModeController.MotionMode.CurrentValue == BodyMotionMode.Default)
                .ThrottleFirst(TimeSpan.FromSeconds(1.5f))
                .Subscribe(_ => _view.RunEnterKeyDownEffect())
                .AddTo(this);

            _runMouseClickParticle
                .Where(_ => _motionModeController.MotionMode.CurrentValue == BodyMotionMode.Default)
                .ThrottleFirst(TimeSpan.FromSeconds(1.2f))
                .Subscribe(_ => _view.RunMouseKeyDownEffect())
                .AddTo(this);

            _runGamepadButtonParticle
                .Where(_ => _motionModeController.MotionMode.CurrentValue == BodyMotionMode.Default)
                .ThrottleFirst(TimeSpan.FromSeconds(0.52f))
                .Subscribe(_ => _view.RunGamepadButtonDownEffect())
                .AddTo(this);

            _runGamepadStickParticle
                .Where(_ => _motionModeController.MotionMode.CurrentValue == BodyMotionMode.Default)
                .ThrottleFirst(TimeSpan.FromSeconds(0.87f))
                .Subscribe(_ => _view.RunGamepadStickMoveEffect())
                .AddTo(this);
        }

        private void OnKeyDown(string key)
        {
            if (!_isActive) return;

            if (key == nameof(System.Windows.Forms.Keys.Enter))
            {
                if (DoRandom(EnterKeyDownProbability))
                {
                    _runEnterKeyDownParticle.OnNext(Unit.Default);
                }
            }
            else
            {
                // NOTE: がちゃがちゃしてたら必ず反応してほしいが、確率上そうなるはずなので特段のケアはしない
                if (DoRandom(KeyDownProbability))
                {
                    return;
                }
                _runKeyDownParticle.OnNext(Unit.Default);
            }
        }

        private void OnGamepadButton(GamepadKeyData data)
        {
            if (!_isActive) return;

            if (!data.IsPressed || data.IsArrowKey) return;
            
            //NOTE: 確率以外で「短時間でたくさん打鍵したら100%」みたいなジャッジもほしい (低頻度の打鍵でも出てほしいけど)
            if (DoRandom(GamepadButtonDownProbability))
            {
                _runGamepadButtonParticle.OnNext(Unit.Default);
            }
        }

        private void OnMouseClick(string buttonName)
        {
            if (!_isActive) return;

            if (DoRandom(MouseClickProbability))
            {
                _runMouseClickParticle.OnNext(Unit.Default);
            }

        }

        private void OnGamepadStick(Vector2Int input)
        {
            if (!_isActive) return;

            // ほぼスティックを倒し終わっている状態で動かすと一定確率で発火
            var sqrMagnitude = new Vector2(input.x / 32767f, input.y / 32767f).sqrMagnitude;
            if (sqrMagnitude < 0.95f)
            {
                return;
            }

            if (DoRandom(GamepadButtonStickProbability))
            {
                _runGamepadStickParticle.OnNext(Unit.Default);
            }
        }

        private void ReplaceTextureIfExists()
        {
            if (LoadTexture("manga_keydown.png") is { } keyDownTexture)
            {
                _view.SetKeyDownTexture(keyDownTexture);
            }

            if (LoadTexture("manga_enter_keydown.png") is { } enterKeyDownTexture)
            {
                _view.SetEnterKeyDownTexture(enterKeyDownTexture);
            }

            if (LoadTexture("manga_click.png") is { } clickTexture)
            {
                _view.SetMouseClickTexture(clickTexture);
            }
            
            if (LoadTexture("manga_gamepad_button.png") is { } gamepadButtonTexture)
            {
                _view.SetGamepadButtonDownTexture(gamepadButtonTexture);
            }

            if (LoadTexture("manga_gamepad_stick.png") is { } gamepadStickTexture)
            {
                _view.SetGamepadStickTexture(gamepadStickTexture);
            }
        }

        //NOTE: 破棄についてはケアしない (アプリ起動中に1回だけ読み込んでずっと使うため)
        private static Texture2D LoadTexture(string fileName)
        {
            var filePath = SpecialFiles.GetTextureReplacementPath(fileName);
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var texture = new Texture2D(32, 32);
                texture.LoadImage(File.ReadAllBytes(filePath), true);
                return texture;
            }
            catch (Exception e)
            {
                LogOutput.Instance.Write(e);
                return null;
            }
        }

        private static bool DoRandom(float probability) => Random.value < probability;
    }
}
