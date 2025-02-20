using Baku.VMagicMirror.Buddy.Api;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    // TODO: 呼び出し元を誰にするかとかは要整理
    // フレーム内タイミングもたいがい大事になるので、ScriptEventInvoker系のクラスから呼ぶのが良いかもしれない
    public class BuddySpriteUpdater
    {
        public void UpdateSprite(Sprite2DApi sprite)
        {
            var pos = sprite.InternalPosition;
            if (sprite.Effects.Floating.IsActive)
            {
                pos = GetAndUpdateFloatingPosition(pos, sprite.InternalEffects.InternalFloating);
            }
            // TODO: 不要になる or 必要だけどanchorじゃない方法で適用する…となりそうで、ややこしいので一旦ストップ
            //sprite.Instance.SetPosition(pos);

            var size = sprite.InternalSize;
            if (sprite.Effects.BounceDeform.IsActive)
            {
                size = GetAndUpdateBounceDeformedSize(size, sprite.InternalEffects.InternalBounceDeform);
            }
            sprite.Instance.SetSize(size);
            var isTransitionDone =
                sprite.Instance.DoTransition(Time.deltaTime, sprite.CurrentTexture, sprite.CurrentTransitionStyle);
            // sprite.Instance.SetTexture(sprite.CurrentTexture);
            if (isTransitionDone)
            {
                sprite.CurrentTransitionStyle = Sprite2DTransitionStyle.None;
            }
        }

        private Vector2 GetAndUpdateBounceDeformedSize(Vector2 size, BounceDeformSpriteEffect effect)
        {
            var t = effect.ElapsedTime + Time.deltaTime;
            if (t > effect.Duration)
            {
                if (effect.Loop)
                {
                    effect.ElapsedTime = t - effect.Duration;
                }
                else
                {
                    effect.IsActive = false;
                    return size;
                }
            }

            var rate = t / effect.Duration;
            // bounceRate > 0 のとき、横に平べったくなる。マイナスの場合は縦に伸びる
            var bounceRate = Mathf.Sin(rate * Mathf.PI * 2f);
            
            // - Intensityは「伸びる側の伸び率」を規定する
            // - 縮むほうはSizeの積が一定になるように決定される(=伸びたぶんの逆数で効かす)
            // TODO: bounceRateの正負切り替わりの瞬間がキモいかもしれないので様子を見ましょう
            if (bounceRate > 0)
            {
                var x = 1 + bounceRate * effect.Intensity;
                var y = 1 / x;
                return new Vector2(size.x * x, size.y * y);
            }
            else
            {
                var y = 1 + (-bounceRate) * effect.Intensity;
                var x = 1 / y;
                return new Vector2(size.x * x, size.y * y);
            }
        }

        private Vector2 GetAndUpdateFloatingPosition(Vector2 pos, FloatingSpriteEffect effect)
        {
            var t = effect.ElapsedTime + Time.deltaTime;
            if (t > effect.Duration)
            {
                effect.ElapsedTime = t - effect.Duration;
            }

            var rate = t / effect.Duration;
            var yRate = 0.5f * (1 - Mathf.Cos(rate * Mathf.PI * 2f));
            return pos + new Vector2(0, yRate * effect.Intensity);
        }
    }
}
