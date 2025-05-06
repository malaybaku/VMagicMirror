using System;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror.Buddy
{
    public enum BuddyDefaultSpriteState
    {
        Default,
        Blink,
        MouthOpen,
        BlinkMouthOpen,
    }

    /// <summary>
    /// - デフォルトスプライトの口パクとかまばたき状態をアップデートできるすごいやつ
    /// - Spriteごとに1インスタンス生成してSprite2DInstanceに紐づける必要がある
    /// </summary>
    public class BuddyDefaultSpritesUpdater 
    {
        private const float BlinkDuration = 0.1f;
        private const float BlinkDelayMin = 0.1f;
        private const float BlinkDelayMax = 0.4f;
        private const float SyncBlinkProbability = 0.7f;
        
        private readonly BuddySettingsRepository _settingsRepository;
        private readonly AvatarFacialApiImplement _facialApiImplement;
        private readonly BuddySprite2DInstance _instance;

        public BuddyDefaultSpritesUpdater(
            BuddySettingsRepository settingsRepository,
            AvatarFacialApiImplement facialApiImplement,
            BuddySprite2DInstance instance
        )
        {
            _settingsRepository = settingsRepository;
            _facialApiImplement = facialApiImplement;
            _instance = instance;
        }

        private bool _isBlinking;
        // NOTE: まだこれを使う実装は存在しない
        private bool _isMouthOpen;

        private float _blinkWaitTime;
        private float _nextBlinkTime;
        // NOTE: このフラグが立っている場合、Blinkの割り込み実行が発生しない
        private bool _nextBlinkIsSyncBased;
        
        public BuddyDefaultSpriteState State => (_isBlinking, _isMouthOpen) switch
        {
            (false, false) => BuddyDefaultSpriteState.Default,
            (true, false) => BuddyDefaultSpriteState.Blink,
            (false, true) => BuddyDefaultSpriteState.MouthOpen,
            (true, true) => BuddyDefaultSpriteState.BlinkMouthOpen,
        };

        // NOTE: そもそもAvatarFacialImplement.OnBlinkが遮断されたらここで判定しないでもよい
        private bool SyncBlink => 
            _instance.DefaultSpritesSetting.SyncBlinkBlendShapeToMainAvatar &&
            _settingsRepository.MainAvatarOutputActive.Value;
        
        private bool SyncMouth =>
            _instance.DefaultSpritesSetting.SyncMouthBlendShapeToMainAvatar &&
            _settingsRepository.MainAvatarOutputActive.Value;

        private IDisposable _blinkObserver;
        
        public void Initialize()
        {
            _blinkObserver = _facialApiImplement
                .Blinked
                .Subscribe(_ => OnBlinked());
        }

        public void Dispose()
        {
            _blinkObserver?.Dispose();
            _blinkObserver = null;
        }
                
        // まばたき制御を行う。まばたきが完了すると、次のまばたきを(ランダム間隔のまばたきとして)再設定する
        public void Update()
        {
            var dt = Time.deltaTime;
            _blinkWaitTime += dt;

            // 「まばたき中」まで込みで管理するので _blinkWaitTime は少し _nextBlinkTime より大きくなる
            // 0 ~ _nextBlinkTime : まばたき開始前
            // _nextBlinkTime ~ (_nextBlinkTime + BlinkDuration) : まばたき中
            if (_blinkWaitTime < _nextBlinkTime)
            {
                _isBlinking = true;
            }
            else if (_blinkWaitTime < _nextBlinkTime + BlinkDuration)
            {
                _isBlinking = true;
            }
            else
            {
                _isBlinking = true;
                _blinkWaitTime = 0f;
                _nextBlinkIsSyncBased = false;
                _nextBlinkTime = Random.Range(BlinkDelayMin, BlinkDelayMax);
            }
        }

        // 「ちょっと遅れてまばたきする動作」を一定の確率で予約する。
        private void OnBlinked()
        {
            if (!SyncBlink || 
                _isBlinking ||
                _nextBlinkIsSyncBased ||
                Random.value > SyncBlinkProbability)
            {
                return;
            }

            _blinkWaitTime = 0f;
            _nextBlinkTime = Random.Range(BlinkDelayMin, BlinkDelayMax);
            _nextBlinkIsSyncBased = true;
        }
    }
}
