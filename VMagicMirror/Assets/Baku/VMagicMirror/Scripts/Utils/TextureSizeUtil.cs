using UnityEngine;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror
{
    public static class TextureSizeUtil
    {
        /// <summary>
        /// テクスチャの幅と高さの上限を抑えます。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static void GetSizeLimitedTexture(Texture2D source, int maxSize)
        {
            if (source.width <= maxSize && source.height <= maxSize)
            {
                return;
            }

            var width = source.width;
            var height = source.height;
            if (width > height)
            {
                height = Mathf.FloorToInt(height * maxSize * 1.0f / width);
                width = maxSize;
            }
            else
            {
                width = Mathf.FloorToInt(width * maxSize * 1.0f / height);
                height = maxSize;
            }

            //NOTE: アクセサリ画像用のはずなので、ARGB32で読んでるはず
            var resized = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Graphics.ConvertTexture(source, resized);
            source.Reinitialize(width, height);
            source.Apply();
            Graphics.ConvertTexture(resized, source);
            Object.Destroy(resized);
        }

        public static void GetSizeLimitedTexture(Texture2D source, Texture2D dest, int maxSize)
        {
            dest.Reinitialize(source.width, source.height);
            dest.Apply();
            Graphics.ConvertTexture(source, dest);
            Object.Destroy(source);
            GetSizeLimitedTexture(dest, maxSize);
        }
    }
}
