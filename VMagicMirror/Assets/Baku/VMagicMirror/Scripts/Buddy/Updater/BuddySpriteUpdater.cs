using Baku.VMagicMirror.Buddy.Api;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary> BuddySpriteについてエフェクト + トランジションに関する処置を行うクラス </summary>
    public class BuddySprite2DUpdater
    {
        readonly struct EffectAppliedPose
        {
            public EffectAppliedPose(Vector2 pos, Quaternion rot, Vector2 scale)
            {
                Pos = pos;
                Rot = rot;
                Scale = scale;
            }
            
            public Vector2 Pos { get; }
            public Quaternion Rot { get; }
            public Vector2 Scale { get; }

            public EffectAppliedPose WithPos(Vector2 pos) => new(pos, Rot, Scale);
            public EffectAppliedPose WithRot(Quaternion rot) => new(Pos, rot, Scale);
            public EffectAppliedPose WithScale(Vector2 scale) => new(Pos, Rot, scale);
            
            public static EffectAppliedPose Default() => new(Vector2.zero, Quaternion.identity, Vector2.one);
        }
        
        public void UpdateSprite(BuddySpriteInstance sprite)
        {
            var pose = EffectAppliedPose.Default();

            var effects = sprite.SpriteEffects;
            pose = Floating(pose, effects.InternalFloating);
            pose = Bounce(pose, effects.InternalBounceDeform);

            // TODO: ここもエフェクトと似たように書きたいが、テクスチャの差し替えタイミングの管理が必要なことには注意する
            var (addRot, isTransitionDone) = sprite.DoTransition(Time.deltaTime, sprite.CurrentTexture, sprite.CurrentTransitionStyle);
            if (isTransitionDone)
            {
                sprite.CurrentTransitionStyle = Sprite2DTransitionStyle.None;
            }

            pose = pose.WithRot(addRot * pose.Rot);

            // NOTE: Posは実際には画面サイズに配慮したスケールにしたい。ので、実は親になってるTransform2Dとかの影響も受ける
            sprite.EffectorRectTransform.anchoredPosition = pose.Pos * 720f;
            sprite.EffectorRectTransform.localRotation = pose.Rot;
            sprite.EffectorRectTransform.localScale = new Vector3(pose.Scale.x, pose.Scale.y, 1f);
        }

        private EffectAppliedPose Bounce(EffectAppliedPose pose, BounceDeformSpriteEffect effect)
        {
            if (!effect.IsActive)
            {
                return pose;
            }
            
            var t = effect.ElapsedTime + Time.deltaTime;
            if (t > effect.Duration)
            {
                if (effect.Loop)
                {
                    effect.ElapsedTime = t - effect.Duration;
                }
                else
                {
                    effect.ElapsedTime = 0f;
                    effect.IsActive = false;
                    return pose;
                }
            }
            else
            {
                effect.ElapsedTime = t;
            }
            
            var rate = effect.ElapsedTime / effect.Duration;
            // bounceRate > 0 のとき、横に平べったくなる。マイナスの場合は縦に伸びる
            var bounceRate = Mathf.Sin(rate * Mathf.PI * 2f);
            
            // - Intensityは「伸びる側の伸び率」を規定する
            // - 縮むほうはSizeの積が一定になるように決定される(=伸びたぶんの逆数で効かす)
            // TODO: bounceRateの正負切り替わりの瞬間がキモいかもしれないので様子を見ましょう
            if (bounceRate > 0)
            {
                var x = 1 + bounceRate * effect.Intensity;
                var y = 1 / x;
                return pose.WithScale(new Vector2(pose.Scale.x * x, pose.Scale.y * y));
            }
            else
            {
                var y = 1 + (-bounceRate) * effect.Intensity;
                var x = 1 / y;
                return pose.WithScale(new Vector2(pose.Scale.x * x, pose.Scale.y * y));
            }
        }

        private EffectAppliedPose Floating(EffectAppliedPose pose, FloatingSpriteEffect effect)
        {
            if (!effect.IsActive)
            {
                return pose;
            }

            var t = effect.ElapsedTime + Time.deltaTime;
            effect.ElapsedTime = (t > effect.Duration)
                ? t - effect.Duration
                : t;

            var rate = effect.ElapsedTime / effect.Duration;
            var yRate = 0.5f * (1 - Mathf.Cos(rate * Mathf.PI * 2f));
            return pose.WithPos(pose.Pos + new Vector2(0, yRate * effect.Intensity));
        }
    }
}
