using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using UniVRM10;
using VRMShaders;

namespace Baku.VMagicMirror
{
    //NOTE: このクラスがやることは2つ
    //- 既定のフォルダから利用可能なモーションを持ってきて保持しておく
    //- 指定したアニメーションを再生する。このとき、アクティブなアニメーションをただ一つだけ再生する
    
    //TODO寄りの補足:
    //- vrmaどうしの補間したいときの対応: 最大2つまでアニメーション動いててもOKにするetc.
    //- ロードをケチりたい場合の何か
    public class VrmaRepository
    {
        private const string VrmaFileExtension = ".vrma";
        
        private readonly List<VrmaFileItem> _fileItems = new();
        private readonly List<VrmaInstance> _instances = new();

        // _fileItems覧の確認が終わってればtrue
        private bool _fileItemsInitialized;
        // アニメーションデータ自体のロードが始まる/完了済みだとtrue;
        private bool _instanceInitializeStarted;
        private bool _instanceInitialized;

        public IReadOnlyList<string> GetAvailableMotionNames()
            => GetAvailableFileItems().Select(item => item.FileName).ToArray();
        
        public IReadOnlyList<VrmaFileItem> GetAvailableFileItems()
        {
            if (!_fileItemsInitialized)
            {
                InitializeFileItems();
                _fileItemsInitialized = true;
            }
            return _fileItems;
        }

        public void Initialize()
        {
            if (_instanceInitializeStarted)
            {
                return;
            }

            _instanceInitializeStarted = true;
            InitializeInstancesAsync().Forget();
        }

        public void Run(VrmaFileItem file, bool loop, out float duration)
        {
            //NOTE: ロード前にめっちゃ急いで呼び出されたら対応しない。わかりやすさのため
            if (!_instanceInitialized)
            {
                duration = 1f;
                return;
            }

            var index = _instances.FindIndex(i => i.File.Equals(file));
            if (index < 0)
            {
                duration = 1f;
                return;
            }

            var item =_instances[index];
            _instances.RemoveAt(index);
            _instances.Insert(0, item);
            _instances[0].PlayFromStart(loop);
            duration = item.Animation.clip.length;

            foreach (var instance in _instances.Skip(1))
            {
                instance.Stop();
            }
        }

        public void Stop()
        {
            if (_instances.Count > 0)
            {
                _instances[0].Stop();
            }
        }
        
        private void InitializeFileItems()
        {
            _fileItems.Clear();
            var folder = SpecialFiles.MotionsDirectory;
            foreach (var filePath in Directory
                .GetFiles(folder)
                .Where(file => Path.GetExtension(file) == VrmaFileExtension)
                .Select(Path.GetFullPath)
                )
            {
                _fileItems.Add(new VrmaFileItem(filePath));
            }
        }
        
        private async UniTaskVoid InitializeInstancesAsync()
        {
            try
            {
                foreach (var fileItem in GetAvailableFileItems())
                {
                    using var data = new AutoGltfFileParser(fileItem.FilePath).Parse();
                    using var loader = new VrmAnimationImporter(data);
                    var instance = await loader.LoadAsync(new ImmediateCaller());
                    _instances.Add(new VrmaInstance(
                        fileItem,
                        instance.GetComponent<Vrm10AnimationInstance>(),
                        instance.GetComponent<Animation>()
                    ));
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
            finally
            {
                _instanceInitialized = true;
            }
        }
    }
}
