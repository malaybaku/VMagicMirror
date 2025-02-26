using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniVRM10;
using VRM;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyVrmInstance : BuddyObject3DInstanceBase
    {
        private bool _hasModel = false;
        private Vrm10Instance _vrm = null;

        public void UpdateInstance()
        {
            if (_hasModel)
            {
                _vrm.Runtime.Process();
            }
        }
        
        public async UniTask LoadAsync(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (Path.GetExtension(path).ToLower() != ".vrm")
            {
                // NOTE: メインのVRM10LoadControllerと違って「拡張子がわざと変えてある」程度なら通しても良い可能性もある
                return;
            }

            try
            {
                var bytes = await File.ReadAllBytesAsync(path);
                var instance = await Vrm10.LoadBytesAsync(bytes,
                    true,
                    ControlRigGenerationOption.Generate,
                    true,
                    ct: this.GetCancellationTokenOnDestroy()
                );

                //NOTE: VRM10LoadControllerでもやってる処理なのでこっちでもやっている
                foreach(var sr in instance.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    sr.updateWhenOffscreen = true;
                }

                _vrm = instance;
                //NOTE: Script Execution OrderをVMM側で制御したいので。
                instance.UpdateType = Vrm10Instance.UpdateTypes.None;
                
                var renderers = instance.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    //セルフシャドウは明示的に切る: ちょっとでも軽量化したい
                    r.receiveShadows = false;
                }
            }
            catch (Exception ex)
            {
                //TODO: ここでバイナリを捨てる必要がありそう
            }
        }

        // NOTE: Vrmについては「インスタンス生成時にパスとバイナリ確定します、破棄は最後のDestroyのときだけです！」もアリかも
        private void ReleaseCurrentVrm()
        {
            
        }
        
        // TODO: Glbに定義したのと似たようなAPIを生やしていく or Apiクラス起点でいろいろ決める
        public void Hide() => gameObject.SetActive(false);
    }
}
