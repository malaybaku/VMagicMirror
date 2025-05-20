using VMagicMirror.Buddy;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    // NOTE: 個々のエフェクトについて、値域などがInterface側のdocで決めてあるので、その値域への範囲制限等はこのファイルの中で効かせておく
    public class SpriteEffectApi : ISpriteEffect
    {
        public FloatingSpriteEffect InternalFloating { get; } = new();
        public PuniSpriteEffect InternalPuni { get; } = new();
        public VibrateSpriteEffect InternalVibrate { get; } = new();
        public JumpSpriteEffect InternalJump { get; } = new();

        IFloatingSpriteEffect ISpriteEffect.Floating => InternalFloating;
        IPuniSpriteEffect ISpriteEffect.Puni => InternalPuni;
        IVibrateSpriteEffect ISpriteEffect.Vibrate => InternalVibrate;
        IJumpSpriteEffect ISpriteEffect.Jump => InternalJump;
    }

    // NOTE: SpriteEffectの中で時刻をインクリメントする部分に使う
    public class SpriteEffectTimeController
    {
        public float ElapsedTime { get; private set; }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (!value)
                {
                    ElapsedTime = 0f;
                }
            }
        }

        public bool Loop { get; set; }

        // NOTE: どのようなエフェクトも無限小の時間では実行できない…とする
        private float _duration = 0.01f;

        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Max(value, 0.01f);
        }

        // NOTE: ラフに0除算避けをしてる
        public float Rate => ElapsedTime / Duration;

        public bool ShouldStop => !IsActive || (!Loop && Rate >= 1f);

        public void ResetElapsedTime() => ElapsedTime = 0f;
        
        public void Reset()
        {
            IsActive = false;
            ElapsedTime = 0f;
            Duration = 0f;
        }

        public void Update(float deltaTime)
        {
            ElapsedTime = Loop 
                ? Mathf.Repeat(ElapsedTime + deltaTime, Duration) 
                : Mathf.Min(ElapsedTime + deltaTime, Duration);
        }
    }

    public class PuniSpriteEffect : IPuniSpriteEffect
    {
        internal SpriteEffectTimeController TimeController { get; } = new()
        {
            Loop = true,
            Duration = 1f,
        };

        public bool IsActive
        {
            get => TimeController.IsActive;
            set => TimeController.IsActive = value;
        }

        public float Duration
        {
            get => TimeController.Duration;
            set => TimeController.Duration = Mathf.Max(value, 0.01f);
        }

        private float _intensity = 0.1f;
        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Max(value, 0f);
        }
    }

    public class VibrateSpriteEffect : IVibrateSpriteEffect
    {
        internal SpriteEffectTimeController TimeController { get; } = new()
        {
            Loop = true,
            // NOTE: 精度上問題なさそうな範囲で長周期にしておく。Durationをまたぐ瞬間だけ不自然になるが、そこはまあ許容で…
            Duration = 1800f,
        };

        public bool IsActive
        {
            get => TimeController.IsActive;
            set => TimeController.IsActive = value;
        }

        private float _intensityX = 5f;
        public float IntensityX
        {
            get => _intensityX;
            set => _intensityX = Mathf.Max(value, 0f);
        }
        private float _frequencyX = 20f;
        public float FrequencyX
        {
            get => _frequencyX;
            set => _frequencyX = Mathf.Max(value, 0f);
        }

        private float _intensityY = 5f;
        public float IntensityY
        {
            get => _intensityY;
            set => _intensityY = Mathf.Max(value, 0f);
        }
        private float _frequencyY = 20f;
        public float FrequencyY
        {
            get => _frequencyY;
            set => _frequencyY = Mathf.Max(value, 0f);
        }
        
        private float _phaseOffsetX;
        public float PhaseOffsetX
        {
            get => _phaseOffsetX;
            set => _phaseOffsetX = Mathf.Clamp01(value);
        }

        private float _phaseOffsetY;
        public float PhaseOffsetY
        {
            get => _phaseOffsetY;
            set => _phaseOffsetY = Mathf.Clamp01(value);
        }
    }
    
    public class FloatingSpriteEffect : IFloatingSpriteEffect
    {
        // NOTE: Floating では常時ループのみをサポートし、単発実行はサポートしていない
        internal SpriteEffectTimeController TimeController { get; } = new()
        {
            Loop = true,
            Duration = 2f,
        };

        public bool IsActive
        {
            get => TimeController.IsActive;
            set => TimeController.IsActive = value;
        }

        public float Duration
        {
            get => TimeController.Duration;
            set => TimeController.Duration = value;
        }

        public float Intensity { get; set; } = 24f;
    }

    public class JumpSpriteEffect : IJumpSpriteEffect
    {
        internal SpriteEffectTimeController TimeController { get; } = new();

        internal float Intensity { get; private set; }
        internal int Count { get; private set; }

        public void Jump(float duration, float intensity, int count)
        {
            if (duration <= 0f || count <= 0)
            {
                Stop();
                return;
            }

            TimeController.Duration = duration;
            TimeController.IsActive = true;
            TimeController.ResetElapsedTime();

            Intensity = intensity;
            Count = count;
        }

        public void Stop() => TimeController.Reset();
    }
}