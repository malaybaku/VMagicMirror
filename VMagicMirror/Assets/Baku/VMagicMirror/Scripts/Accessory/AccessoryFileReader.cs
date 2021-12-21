using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// アクセサリーのファイルバイナリからUnityで使える実態に変換してくれるやつ
    /// </summary>
    public static class AccessoryFileReader
    {
        // NOTE: ぜんぶ同じフォルダに入ってる事は保証されてない事に注意。
        // gltfや、(今は無いけど想定される例として)連番画像とかはフォルダを区切った中に入る。
        
        public static Texture2D LoadPngImage(byte[] bytes)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            tex.Apply();
            return tex;
        }

        //NOTE: なんとなく関数を分けた方が治安が良いので分けておく
        public static GameObject LoadGltf(string path, byte[] bytes)
        {
            return LoadGlb(path, bytes);
        }

        public static GameObject LoadGlb(string path, byte[] bytes)
        {
            var context = new UniGLTF.ImporterContext();
            context.Load(path, bytes);
            return context.Root;
        }
    }
}
