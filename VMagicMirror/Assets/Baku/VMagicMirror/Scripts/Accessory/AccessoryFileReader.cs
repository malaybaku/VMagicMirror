using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// アクセサリーのファイルバイナリからUnityで使える実態に変換してくれるやつ
    /// </summary>
    public static class AccessoryFileReader
    {
        public static Texture2D LoadPngImage(byte[] bytes)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            tex.Apply();
            return tex;
        }

        //NOTE: いちおう分けといた方が健全な気がするので冗長に書いてある
        public static GameObject LoadGltf(byte[] bytes)
        {
            return LoadGlb(bytes);
        }

        public static GameObject LoadGlb(byte[] bytes)
        {
            var context = new UniGLTF.ImporterContext();
            context.Load("", bytes);
            return context.Root;
        }
    }
}
