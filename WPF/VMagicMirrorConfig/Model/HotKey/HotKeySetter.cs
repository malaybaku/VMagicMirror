using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// HotKeyの設定に応じて実際にホットキーの登録/解除をするすごいやつだよ
    /// </summary>
    class HotKeySetter
    {
        public HotKeySetter() : this(
            ModelResolver.Instance.Resolve<HotKeyModel>(),
            ModelResolver.Instance.Resolve<HotKeySettingModel>()
            )
        {
        }

        internal HotKeySetter(HotKeyModel model, HotKeySettingModel setting)
        {
            _model = model;
            _setting = setting;
        }

        private readonly HotKeyModel _model;
        private readonly HotKeySettingModel _setting;
        private readonly List<HotKeyRegisterItem> _latestRegisterItems = new List<HotKeyRegisterItem>();

        public void Initialize()
        {
            //NOTE: _modelの初期化待ちをこのクラスでやるように追記してもよさそう
            _setting.Updated += (_, __) => OnSettingUpdated();
            _setting.SingleItemUpdated += OnSettingUpdated;
            _setting.EnableHotKey.PropertyChanged += OnEnableHotKeyChanged;

            //初期状態は明示的に同期する: Initializeの時点でホットキーの設定が読み込み済みの可能性があるので
            OnSettingUpdated();
        }

        private void OnEnableHotKeyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnSettingUpdated();
        }

        private void OnSettingUpdated()
        {
            //HotKeyを無効扱いする場合は明示的に設定を取っ払う。
            //そうしないと他アプリから使えないままになるので
            var items = _setting.EnableHotKey.Value 
                ? _setting.Items.ToArray()
                : Array.Empty<HotKeyRegisterItem>();            

            foreach(var itemToRemove in _latestRegisterItems.Where(i => !items.Contains(i)))
            {
                _model.Unregister(itemToRemove);
                _setting.RemoveInvalidItem(itemToRemove);
            }

            foreach(var itemToAdd in items.Where(i => !_latestRegisterItems.Contains(i)))
            {
                var succeed = _model.Register(itemToAdd);
                if (!succeed)
                {
                    _setting.AddInvalidItem(itemToAdd);
                }
            }

            _latestRegisterItems.Clear();
            _latestRegisterItems.AddRange(items);
        }
    }
}
