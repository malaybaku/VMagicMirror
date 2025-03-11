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

            // NOTE: エフェクトを上乗せする場合、普通はAdd関数を使う。Withを使うと他のエフェクトの計算結果をかき消すので、通常は使わない
            public EffectAppliedPose AddPos(Vector2 pos) => new(Pos + pos, Rot, Scale);
            public EffectAppliedPose AddRot(Quaternion rot) => new(Pos, rot * Rot, Scale);
            public EffectAppliedPose AddScale(Vector2 scale) => new(Pos, Rot, Vector2.Scale(scale, Scale));
            
            public EffectAppliedPose WithPos(Vector2 pos) => new(pos, Rot, Scale);
            public EffectAppliedPose WithRot(Quaternion rot) => new(Pos, rot, Scale);
            public EffectAppliedPose WithScale(Vector2 scale) => new(Pos, Rot, scale);
            
            public static EffectAppliedPose Default() => new(Vector2.zero, Quaternion.identity, Vector2.one);
        }
        
        public void UpdateSprite(BuddySprite2DInstance sprite)
        {
            var pose = EffectAppliedPose.Default();

            var effects = sprite.SpriteEffects;
            pose = Floating(pose, effects.InternalFloating);
            pose = Bounce(pose, effects.InternalBounceDeform);
            pose = Jump(pose, effects.InternalJump);

            // NOTE: Transitionのポーズ変形はz軸以外の回転を含むため、これを最後にやる (先にやると回転の合成がヘンになる)
            if (!sprite.Transition.IsCompleted)
            {
                BuddySprite2DInstanceTransition transition;
                (pose, transition) = DoTransition(sprite, Time.deltaTime, pose, sprite.Transition);
                sprite.Transition = transition;
            }

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
            
            effect.TimeController.Update(Time.deltaTime);
            if (effect.TimeController.ShouldStop)
            {
                effect.TimeController.Reset();
                return pose;
            }

            // bounceRate > 0 のとき、横に平べったくなる。マイナスの場合は縦に伸びる
            var bounceRate = Mathf.Sin(effect.TimeController.Rate * Mathf.PI * 2f);
            
            // - Intensityは「伸びる側の伸び率」を規定する
            // - 縮むほうはSizeの積が一定になるように決定される(=伸びたぶんの逆数で効かす)
            // TODO: bounceRateの正負切り替わりの瞬間がキモいかもしれないので様子を見ましょう
            if (bounceRate > 0)
            {
                var x = 1 + bounceRate * effect.Intensity;
                var y = 1 / x;
                return pose.AddScale(new Vector2(pose.Scale.x * x, pose.Scale.y * y));
            }
            else
            {
                var y = 1 + (-bounceRate) * effect.Intensity;
                var x = 1 / y;
                return pose.AddScale(new Vector2(pose.Scale.x * x, pose.Scale.y * y));
            }
        }

        private EffectAppliedPose Floating(EffectAppliedPose pose, FloatingSpriteEffect effect)
        {
            if (!effect.IsActive)
            {
                return pose;
            }

            effect.TimeController.Update(Time.deltaTime);
            var yRate = 0.5f * (1 - Mathf.Cos(effect.TimeController.Rate * Mathf.PI * 2f));
            return pose.AddPos(pose.Pos + new Vector2(0, yRate * effect.Intensity));
        }

        private EffectAppliedPose Jump(EffectAppliedPose pose, JumpSpriteEffect effect)
        {
            if (!effect.TimeController.IsActive)
            {
                return pose;
            }

            effect.TimeController.Update(Time.deltaTime);
            if (effect.TimeController.ShouldStop)
            {
                effect.TimeController.Reset();
                return pose;
            }

            // 例: 3回まとめてジャンプする場合、0 -> 1 になるのを3度繰り返す 
            var jumpRate = Mathf.Repeat(effect.TimeController.Rate * effect.Count, 1f);

            // 以下の3点を通過する二次関数
            // - jumpRate = 0 or 1 で 0
            // - jumpRate = 0.5　で Intensity
            var height = 4f * effect.Intensity * jumpRate * (1 - jumpRate);
            
            return pose.AddPos(pose.Pos + Vector2.up * height);
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
                return (pose.AddRot(pose.Rot * Quaternion.Euler(0, yaw, 0)), transition);
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
                return (pose.AddRot(pose.Rot * Quaternion.Euler(0, yaw, 0)), transition);
            }            
        }
    }
}
