using System.IO;
using Cysharp.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using UniVRM10;
using VRMShaders;

namespace Baku.VMagicMirror.Buddy
{
    // NOTE: Lengthの取得方法はVrmaRepository/VrmaInstance (Word to Motion機能のほうでVRMAを読んでるやつ) と同じ方法になっている
    
    // NOTE: MonoBehaviour化するかどうかはリソース破棄の都合とかにも依存する
    public class BuddyVrmAnimationInstance
    {
        private RuntimeGltfInstance _gltfInstance;
        public Vrm10AnimationInstance Instance { get; private set; }
        public Animation Animation { get; private set; }
        
        public bool IsLoaded => _gltfInstance != null;
        
        // NOTE: setterはコンポーネントを生成するメソッドのみから用いる。InjectするほどでもないのでDIは使ってない
        public BuddyFolder BuddyFolder { get; set; }

        public async UniTask LoadAsync(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                Debug.LogError("VRM Animation not found");
                return;
            }

            if (Path.GetExtension(fullPath).ToLower() != ".vrma")
            {
                // NOTE: 拡張子が違っても通す…という手もあるが、話がややこしくなるので禁止
                Debug.LogError("VRM Animation extension is invalid");
                return;
            }

            RuntimeGltfInstance gltfInstance = null;
            try
            {
                using var data = new AutoGltfFileParser(fullPath).Parse();
                using var loader = new VrmAnimationImporter(data);
                gltfInstance = await loader.LoadAsync(new ImmediateCaller());
                var instance = gltfInstance.GetComponent<Vrm10AnimationInstance>();
                var animation = gltfInstance.GetComponent<Animation>();
                instance.ShowBoxMan(false);

                _gltfInstance = gltfInstance;
                Instance = instance;
                Animation = animation;
            }
            catch
            {
                if (gltfInstance != null)
                {
                    gltfInstance.Dispose();
                }

                _gltfInstance = null;
                Instance = null;
                Animation = null;
            }
        }

        public void Dispose()
        {
            if (IsLoaded)
            {
                _gltfInstance.Dispose();
            }

            _gltfInstance = null;
            Instance = null;
            Animation = null;
        }
        
        public float GetLength()
        {
            // 1とかでも良いのかもしれない
            if (!IsLoaded) return -1;
            
            return Animation.clip.length;
        }
    }
}

