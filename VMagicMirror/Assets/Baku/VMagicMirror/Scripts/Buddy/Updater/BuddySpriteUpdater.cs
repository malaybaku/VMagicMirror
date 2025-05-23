using System;
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
            if (sprite.DefaultSpritesInstance.HasValidSetup)
            {
                sprite.UpdateDefaultSpritesTexture();
            }

            // デフォルト立ち絵 + 各種Effectによるポーズの更新
            var pose = EffectAppliedPose.Default();
            var effects = sprite.SpriteEffects;

            pose = DefaultSpritesBlink(pose, sprite);
            pose = Floating(pose, effects.InternalFloating);
            pose = Puni(pose, effects.InternalPuni);
            pose = Vibrate(pose, effects.InternalVibrate);
            pose = Jump(pose, effects.InternalJump);

            // NOTE: Transitionのポーズ変形はz軸以外の回転を含むため、これを最後にやる (先にやると回転の合成がヘンになる)
            if (!sprite.Transition.IsCompleted)
            {
                // NOTE: 遷移先がデフォルト立ち絵の場合、その遷移先が変わっているのも後追いしておく(ここまでせんでも良いかもだが)
                if (sprite.Transition.IsDefaultSprites && sprite.Transition.HasUnAppliedTexture)
                {
                    var t = sprite.Transition;
                    t.UnAppliedTexture = sprite.DefaultSpritesInstance.CurrentTexture;
                    sprite.Transition = t;
                }
                
                BuddySprite2DInstanceTransition transition;
                (pose, transition) = DoTransition(sprite, Time.deltaTime, pose, sprite.Transition);
                sprite.Transition = transition;
            }

            sprite.EffectorRectTransform.anchoredPosition = pose.Pos;
            sprite.EffectorRectTransform.localRotation = pose.Rot;
            sprite.EffectorRectTransform.localScale = new Vector3(pose.Scale.x, pose.Scale.y, 1f);
        }

        private EffectAppliedPose DefaultSpritesBlink(EffectAppliedPose pose, BuddySprite2DInstance sprite)
        {
            if (sprite.IsDefaultSpritesActive && 
                sprite.DefaultSpritesUpdater.State is BuddyDefaultSpriteState.Blink or BuddyDefaultSpriteState.BlinkMouthOpen)
            {
                return pose.AddPos(sprite.DefaultSpritesSetting.LocalPositionOffsetOnBlink);
            }
            else
            {
                return pose;
            }
        }
        
        private EffectAppliedPose Puni(EffectAppliedPose pose, PuniSpriteEffect effect)
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

            // puniRate > 0 のとき、横に平べったくなる。マイナスの場合は縦に伸びる
            var puniRate = Mathf.Sin(effect.TimeController.Rate * Mathf.PI * 2f);
            
            // - Intensityは「伸びる側の伸び率」を規定する
            // - 縮むほうはSizeの積が一定になるように決定される(=伸びたぶんの逆数で効かす)
            // NOTE: puniRateの正負切り替わりの瞬間がキモいかもなので、気になったら調整してもよい
            if (puniRate > 0)
            {
                var x = 1 + puniRate * effect.Intensity;
                var y = 1 / x;
                return pose.AddScale(new Vector2(pose.Scale.x * x, pose.Scale.y * y));
            }
            else
            {
                var y = 1 + (-puniRate) * effect.Intensity;
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

        private EffectAppliedPose Vibrate(EffectAppliedPose pose, VibrateSpriteEffect effect)
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

            // 縦横それぞれを別々に評価する。このとき、
            var x = effect.IntensityX * Mathf.Sin(
                (effect.TimeController.ElapsedTime * effect.FrequencyX + effect.PhaseOffsetX) * Mathf.PI * 2f
                );
            var y = effect.IntensityY * Mathf.Sin(
                (effect.TimeController.ElapsedTime * effect.FrequencyY + effect.PhaseOffsetY) * Mathf.PI * 2f
                );
            return pose.AddPos(new Vector2(x, y));
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

            if (transition.IsImmediate)
            {
                instance.SetTexture(transition.UnAppliedTexture, transition.IsDefaultSprites);
                return (pose, BuddySprite2DInstanceTransition.None);
            }

            // 時刻経過の考え方 + 終端まで行ったらNoneになること等は共通なのでここで処理しとく
            transition.Time += deltaTime;
            if (transition.IsCompleted)
            {
                if (transition.HasUnAppliedTexture)
                {
                    instance.SetTexture(transition.UnAppliedTexture, transition.IsDefaultSprites);
                }
                return (pose, BuddySprite2DInstanceTransition.None);
            }
            
            switch (transition.Style)
            {
                case Sprite2DTransitionStyle.LeftFlip:
                    return DoHorizontalFlipTransition(instance, pose, transition, true);
                case Sprite2DTransitionStyle.RightFlip:
                    return DoHorizontalFlipTransition(instance, pose, transition, false);
                case Sprite2DTransitionStyle.BottomFlip:
                    return DoBottomFlipTransition(instance, pose, transition);
                case Sprite2DTransitionStyle.None:
                case Sprite2DTransitionStyle.Immediate:
                    // 実装ミスでのみ通過する(すでにガードしてあるので)
                    throw new InvalidOperationException();
                default:
                    throw new NotImplementedException();
            }
        }

        private (EffectAppliedPose, BuddySprite2DInstanceTransition) DoHorizontalFlipTransition(
            BuddySprite2DInstance instance,
            EffectAppliedPose pose,
            BuddySprite2DInstanceTransition transition,
            bool isLeft)
        {
            // NOTE: 計算が凝っているのは、world space UIでカメラに対してUIが垂直になって隠れる角度が90度であることが保証されないため
            // NOTE: 移動しながらこの計算して合わせに行くと見えがキモいかもなので注意
            var cameraToInstance =
                _mainCamera.transform.InverseTransformPoint(instance.transform.position).normalized;
            cameraToInstance.y = 0f;
            var dir = cameraToInstance.normalized;
            // NOTE: なぜか0.5倍するといい感じになるが、幾何的になぜコレでいいのかが分かってない…ちゃんと理解したい
            var additionalAngle = 0.5f * Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            var yawFactor = isLeft ? 1f : -1f;
            
            if (transition.Rate < 0.5f)
            {
                var yaw = (90f * yawFactor + additionalAngle) * (transition.Rate / 0.5f);
                return (pose.AddRot(pose.Rot * Quaternion.Euler(0, yaw, 0)), transition);
            }
            else
            {
                if (transition.HasUnAppliedTexture)
                {
                    instance.SetTexture(transition.UnAppliedTexture, transition.IsDefaultSprites);
                    transition.UnAppliedTexture = null;
                }

                // NOTE: 0 .. 90deg 付近から -90degにジャンプして-90 .. 0 に進める感じ
                var yaw = Mathf.Lerp(-90f * yawFactor + additionalAngle, 0, (transition.Rate - 0.5f) * 2f);
                return (pose.AddRot(pose.Rot * Quaternion.Euler(0, yaw, 0)), transition);
            }
        }

        private (EffectAppliedPose, BuddySprite2DInstanceTransition) DoBottomFlipTransition(
            BuddySprite2DInstance instance,
            EffectAppliedPose pose,
            BuddySprite2DInstanceTransition transition)
        {
            // NOTE: BottomFlipもHorizontalと同じく「カメラから見えなくなる角度に倒す」という計算をする
            var cameraToInstance =
                _mainCamera.transform.InverseTransformPoint(instance.transform.position).normalized;
            cameraToInstance.x = 0f;
            var dir = cameraToInstance.normalized;
            var additionalAngle = -Mathf.Atan2(dir.y, dir.z) * Mathf.Rad2Deg;

            var rotAngle = 90f + additionalAngle;
            if (transition.Rate < 0.45f)
            {
                var easeRate = transition.Rate / 0.45f;
                //var pitch = rotAngle * easeRate;
                var pitch = rotAngle * EaseInBack(easeRate);
                return (pose.AddRot(pose.Rot * Quaternion.Euler(pitch, 0, 0)), transition);
            }
            else if (transition.Rate < 0.55f)
            {
                //倒れきった状態でキープ
                return (pose.AddRot(pose.Rot * Quaternion.Euler(rotAngle, 0, 0)), transition);
            }
            else
            {
                if (transition.HasUnAppliedTexture)
                {
                    instance.SetTexture(transition.UnAppliedTexture, transition.IsDefaultSprites);
                    transition.UnAppliedTexture = null;
                }

                // 冒頭の動きをひっくり返したやつ
                var easeRate = (1 - transition.Rate) / 0.45f;
                //var pitch = rotAngle * easeRate;
                var pitch = rotAngle * EaseInBack(easeRate);
                return (pose.AddRot(pose.Rot * Quaternion.Euler(pitch, 0, 0)), transition);
            }
        }

        private static float EaseInBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return x * x * (c3 * x - c1);
        }
    }
}
