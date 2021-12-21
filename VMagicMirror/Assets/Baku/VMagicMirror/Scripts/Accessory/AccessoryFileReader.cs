using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class AccessoryFileContext<T>
    {
        public AccessoryFileContext(T obj, Action disposeAction)
        {
            Object = obj;
            Disposer = new AccessoryFileDisposer(disposeAction);
        }
        
        public AccessoryFileDisposer Disposer { get; }
        public T Object { get; }
    }

    public class AccessoryFileDisposer
    {
        public AccessoryFileDisposer(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }
        private readonly Action _disposeAction;
        public void Dispose() => _disposeAction();
    }
    
    /// <summary>
    /// アクセサリーのファイルバイナリからUnityで使える実態に変換してくれるやつ
    /// </summary>
    public static class AccessoryFileReader
    {
        // NOTE: ぜんぶ同じフォルダに入ってる事は保証されてない事に注意。
        // gltfや、(今は無いけど想定される例として)連番画像とかはフォルダを区切った中に入る。
        
        public static AccessoryFileContext<Texture2D> LoadPngImage(byte[] bytes)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            tex.Apply();
            return new AccessoryFileContext<Texture2D>(tex, () =>
            {
                UnityEngine.Object.Destroy(tex);
            });
        }

        //NOTE: なんとなく関数を分けた方が治安が良いので分けておく
        public static AccessoryFileContext<GameObject> LoadGltf(string path, byte[] bytes)
        {
            return LoadGlb(path, bytes);
        }

        public static AccessoryFileContext<GameObject> LoadGlb(string path, byte[] bytes)
        {
            var context = new UniGLTF.ImporterContext();
            context.Load(path, bytes);
            return new AccessoryFileContext<GameObject>(
                context.Root,
                () => context.Dispose()
            );
        }
    }
}
