using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror
{
    public static class AccessoryTextureResizer
    {
        //戻り値: 画像が生データのと同じ解像度ならtrue, 縮まっていればfalse
        public static bool ResizeImage(
            Texture2D texture, int maxSize, byte[] rawBytes, int rawSize)
        {
            if (rawBytes == null || rawBytes.Length == 0)
            {
                return false;
            }

            //分岐が微妙にムズい
            // - 指定より小さい生画像を持っている -> そのままでOK
            // - 生画像を持っており、縮小が必要 -> 手持ちテクスチャを縮める
            // - 縮小した画像を使用中
            //   - それよりmaxSizeが小さい -> さらに縮める
            //   - 縮小画像を2倍以上に拡大してもOK -> デカくするために生画像を再度持ってくる

            var size = Math.Max(texture.width, texture.height);
            
            if (size == rawSize)
            {
                if (rawSize <= maxSize)
                {
                    return true;
                }
                else
                {
                    TextureSizeUtil.GetSizeLimitedTexture(texture, maxSize);
                    return false;
                }
            }

            if (size > maxSize)
            {
                TextureSizeUtil.GetSizeLimitedTexture(texture, maxSize);
                return false;
            }
            else if (size * 2 <= maxSize)
            {
                var rawTexture = new Texture2D(16, 16, TextureFormat.ARGB32, false);
                rawTexture.LoadImage(rawBytes);
                rawTexture.Apply();
                TextureSizeUtil.GetSizeLimitedTexture(rawTexture, texture, maxSize);
                var result = Math.Max(rawTexture.width, rawTexture.height) == rawSize;
                Object.Destroy(rawTexture);
                return result;
            }
            else
            {
                //今の圧縮状態がちょうどいい
                return false;
            }
        }
    }
}
