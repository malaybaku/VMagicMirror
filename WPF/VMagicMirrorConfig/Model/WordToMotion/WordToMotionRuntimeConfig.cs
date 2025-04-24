using System;
using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    class WordToMotionRuntimeConfig
    {
        public WordToMotionRuntimeConfig() : this(
            ModelResolver.Instance.Resolve<WordToMotionSettingModel>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>()
            )
        {
        }

        public WordToMotionRuntimeConfig(WordToMotionSettingModel setting, IMessageReceiver receiver)
        {
            _setting = setting;
            receiver.ReceivedCommand += OnReceiveCommand;

            //書いてる通りだが、このマシン上でVMagicMirrorが初めて実行されたと推定できるとき、
            //デフォルトのWord To Motion一覧を生成して初期化する
            if (!SpecialFilePath.IsAutoSaveFileExist())
            {
                LoadDefaultItems();
            }
        }

        private readonly WordToMotionSettingModel _setting;


        private string[] _latestAvaterExtraClipNames = new string[0];
        //NOTE: 型変換がめんどくさいのでArrayで公開してます
        public string[] LatestAvaterExtraClipNames => _latestAvaterExtraClipNames;

        public List<string> ExtraBlendShapeClipNames => _setting.ExtraBlendShapeClipNames;

        public event EventHandler<BlendShapeCheckedEventArgs>? DetectNewExtraBlendShapeName;

        public void LoadDefaultItems()
        {
            ExtraBlendShapeClipNames.Clear();
            //NOTE: 現在ロードされてるアバターがいたら、そのアバターのブレンドシェイプをただちに当て直す
            ExtraBlendShapeClipNames.AddRange(_latestAvaterExtraClipNames);
            
            _setting.LoadDefaultMotionRequests(ExtraBlendShapeClipNames);
        }

        private void OnReceiveCommand(CommandReceivedData e)
        {
            if (e.Command is not VMagicMirror.VmmServerCommands.ExtraBlendShapeClipNames)
            {
                return;
            }

            //やることは2つ: 
            // - 知らない名前のブレンドシェイプが飛んできたら記憶する
            // - アバターが持ってるExtraなクリップ名はコレですよ、というのを明示的に与える
            _latestAvaterExtraClipNames = e.GetStringValue()
                .Split(',')
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray();

            bool hasNewBlendShape = false;
            foreach (var name in _latestAvaterExtraClipNames
                .Where(n => !ExtraBlendShapeClipNames.Contains(n))
                )
            {
                hasNewBlendShape = true;
                ExtraBlendShapeClipNames.Add(name);
            }

            DetectNewExtraBlendShapeName?.Invoke(this, new BlendShapeCheckedEventArgs(hasNewBlendShape));
        }
    }

    public class BlendShapeCheckedEventArgs : EventArgs
    {
        public BlendShapeCheckedEventArgs(bool hasNewBlendShape)
        {
            HasNewBlendShape = hasNewBlendShape;
        }

        public bool HasNewBlendShape { get; }
    }
}
