using Baku.VMagicMirror.Buddy.Api;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary> BuddySpriteについてエフェクト + トランジションに関する処置を行うクラス </summary>
    public class BuddySprite2DUpdater
    {
        private readonly Camera _mainCamera;
        
        [Inject]
        public BuddySprite2DUpdater(Camera mainCamera)
        {
            _mainCamera = mainCamera;
        }
        
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
        
        public void UpdateSprite(BuddySprite2DInstance sprite)
        {
            var pose = EffectAppliedPose.Default();

            // NOTE: エフェクトの影響で位置がズレてからFlipすると見た目が悪そうなので、先にFlipの計算を入れてしまう
            if (!sprite.Transition.IsCompleted)
            {
                BuddySprite2DInstanceTransition transition;
                (pose, transition) = DoTransition(sprite, Time.deltaTime, pose, sprite.Transition);
                sprite.Transition = transition;
            }

            var effects = sprite.SpriteEffects;
            pose = Floating(pose, effects.InternalFloating);
            pose = Bounce(pose, effects.InternalBounceDeform);
            
            sprite.EffectorRectTransform.anchoredPosition = pose.Pos;
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

        private (EffectAppliedPose, BuddySprite2DInstanceTransition) DoTransition(
            BuddySprite2DInstance instance, float deltaTime, EffectAppliedPose pose, BuddySprite2DInstanceTransition transition)
        {
            if (transition.Style is Sprite2DTransitionStyle.None)
            {
                // 通らないはずだけど一応
                return (pose, transition);
            }
            
            if (transition.Style == Sprite2DTransitionStyle.Immediate)
            {
                instance.SetTexture(transition.UnAppliedTextureKey);
                return (pose, BuddySprite2DInstanceTransition.None);
            }
            
            // TEMP: LeftFlipだけ実装してある。、ホントはRightFlipとかy=0を軸に倒す実装も欲しいいったん常にLeftFlip
            transition.Time += deltaTime;

            if (transition.IsCompleted)
            {
                if (transition.HasUnAppliedTextureKey)
                {
                    instance.SetTexture(transition.UnAppliedTextureKey);
                }
                return (pose, BuddySprite2DInstanceTransition.None);
            }
            
            // NOTE: 計算が凝っているのは、world space UIでカメラに対してUIが垂直になって隠れる角度が90度であることが保証されないため
            // NOTE: 移動しながらこの計算して合わせに行くと見えがキモいかもなので注意
            var cameraToInstance =
                _mainCamera.transform.InverseTransformPoint(instance.transform.position).normalized;
            cameraToInstance.y = 0f;
            var dir = cameraToInstance.normalized;
            // NOTE: なぜか0.5倍するといい感じになるが、幾何的になぜコレでいいのかが分かってない…ちゃんと理解したい
            var additionalAngle = 0.5f * Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            if (transition.Rate < 0.5f)
            {
                var yaw = (90f + additionalAngle) * (transition.Rate / 0.5f);
                return (pose.WithRot(pose.Rot * Quaternion.Euler(0, yaw, 0)), transition);
            }
            else
            {
                if (transition.HasUnAppliedTextureKey)
                {
                    instance.SetTexture(transition.UnAppliedTextureKey);
                    transition.UnAppliedTextureKey = "";
                }

                // NOTE: 0 .. 90deg 付近から -90degにジャンプして-90 .. 0 に進める感じ
                var yaw = Mathf.Lerp(additionalAngle - 90f, 0, (transition.Rate - 0.5f) * 2f);
                return (pose.WithRot(pose.Rot * Quaternion.Euler(0, yaw, 0)), transition);
            }            
        }
    }
}
