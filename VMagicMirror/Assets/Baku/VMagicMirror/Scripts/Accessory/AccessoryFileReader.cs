using System;
using System.IO;
using UniGLTF;
using UnityEngine;
using UnityEngine.Rendering;

namespace Baku.VMagicMirror
{ 
    public class AccessoryFileContext<T>
    {
        public AccessoryFileContext(T obj, IAccessoryFileActions actions)
        {
            Object = obj;
            Actions = actions;
        }
        
        public IAccessoryFileActions Actions { get; }
        public T Object { get; }
    }

    /// <summary>
    /// アクセサリーのファイルバイナリからUnityで使える実態に変換してくれるやつ
    /// </summary>
    public static class AccessoryFileReader
    {
        // NOTE: ぜんぶ同じフォルダに入ってる事は保証されてない事に注意。
        // gltfや、(今は無いけど想定される例として)連番画像とかはフォルダを区切った中に入る。

        public static AccessoryFileContext<Texture2D> LoadPngImage(AccessoryFile file, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return new AccessoryFileContext<Texture2D>(null, new ImageAccessoryActions(file, null));
            }

            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            tex.Apply();
            return new AccessoryFileContext<Texture2D>(tex, new ImageAccessoryActions(file, tex));
        }

        public static AccessoryFileContext<GameObject> LoadGltf(string path, byte[] bytes)
        {
            if (!File.Exists(path) || bytes == null || bytes.Length == 0)
            {
                return new AccessoryFileContext<GameObject>(new GameObject("(empty)"), new EmptyFileActions());
            }

            var parser = new AutoGltfFileParser(path);
            using var data = parser.Parse();
            return LoadGlbOrGltf(data);
        }

        public static AccessoryFileContext<GameObject> LoadGlb(string path, byte[] bytes)
        {
            if (!File.Exists(path) || bytes == null || bytes.Length == 0)
            {
                return new AccessoryFileContext<GameObject>(new GameObject("(empty)"), new EmptyFileActions());
            }

            var parser = new GlbLowLevelParser("", bytes);
            using var data = parser.Parse();
            return LoadGlbOrGltf(data);
        }

        public static AccessoryFileContext<AnimatableImage> LoadNumberedPngImage(byte[][] binaries)
        {
            if (binaries == null)
            {
                binaries = Array.Empty<byte[]>();
            }
            var res = new AnimatableImage(binaries);
            //他と違い、AnimatableImage自体がFileActionを実装済み
            return new AccessoryFileContext<AnimatableImage>(res, res);
        }

        private static AccessoryFileContext<GameObject> LoadGlbOrGltf(GltfData data)
        {
            var context = new ImporterContext(data);
            var instance = context.Load();
            instance.ShowMeshes();
            instance.EnableUpdateWhenOffscreen();

            foreach (var renderer in instance.Root.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return new AccessoryFileContext<GameObject>(instance.Root, new GlbFileAccessoryActions(context, instance));
        }
    }
}
