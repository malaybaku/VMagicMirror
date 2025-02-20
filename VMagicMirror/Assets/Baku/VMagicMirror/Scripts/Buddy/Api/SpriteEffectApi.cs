using Baku.VMagicMirror.Buddy.Api.Interface;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    // NOTE: 個々のエフェクトについて、値域などがInterface側のdocで決めてあるので、その値域への範囲制限等はこのファイルの中で効かせておく
    
    public class SpriteEffectApi : ISpriteEffectApi
    {
        public FloatingSpriteEffect InternalFloating { get; } = new();
        public BounceDeformSpriteEffect InternalBounceDeform { get; } = new();

        IFloatingSpriteEffect ISpriteEffectApi.Floating => InternalFloating;
        IBounceDeformSpriteEffect ISpriteEffectApi.BounceDeform => InternalBounceDeform;
    }

    public class BounceDeformSpriteEffect : IBounceDeformSpriteEffect
    {
        internal float ElapsedTime { get; set; }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (!value)
                {
                    ElapsedTime = 0;
                }
            }
        }

        private float _intensity = 0.5f;
        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Max(value, 0f);
        }

        private float _duration = 0.5f;
        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Clamp(value, 0.01f, 5f);
        }

        public bool Loop { get; set; } = true;
    }

    public class FloatingSpriteEffect : IFloatingSpriteEffect
    {
        internal float ElapsedTime { get; set; }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (!value)
                {
                    ElapsedTime = 0;
                }
            }
        }

        private float _intensity = 0.5f;
        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Clamp01(value);
        }

        private float _duration = 2f;
        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Clamp(value, 0.1f, 10f);
        }

        // Floatはループしかせんやろ…ということでLoopオプションは省いている
    }
}
