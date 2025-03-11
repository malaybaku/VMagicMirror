using VMagicMirror.Buddy;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    // NOTE: 個々のエフェクトについて、値域などがInterface側のdocで決めてあるので、その値域への範囲制限等はこのファイルの中で効かせておく
    public class SpriteEffectApi : ISpriteEffect
    {
        public FloatingSpriteEffect InternalFloating { get; } = new();
        public BounceDeformSpriteEffect InternalBounceDeform { get; } = new();
        public JumpSpriteEffect InternalJump { get; } = new();

        IFloatingSpriteEffect ISpriteEffect.Floating => InternalFloating;
        IBounceDeformSpriteEffect ISpriteEffect.BounceDeform => InternalBounceDeform;
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

    public class BounceDeformSpriteEffect : IBounceDeformSpriteEffect
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

        public bool Loop
        {
            get => TimeController.Loop;
            set => TimeController.Loop = value;
        }

        private float _intensity = 24f;

        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Max(value, 0f);
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