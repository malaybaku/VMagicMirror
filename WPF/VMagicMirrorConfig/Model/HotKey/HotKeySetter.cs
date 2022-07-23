using System;
using System.Collections.Generic;
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

            //初期状態は明示的に同期する: Initializeの時点でホットキーの設定が読み込み済みの可能性があるので
            OnSettingUpdated();
        }

        private void OnSettingUpdated()
        {
            var items = _setting.Items.ToArray();
            if (!CheckItemsValidity(items))
            {
                return;
            }

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

        //TODO: validity checkここでやるのお？という感じはする。settingModel側にも何か合って良さそう
        private bool CheckItemsValidity(HotKeyRegisterItem[] items)
        {
            //ざっくり、被ってるのがあるかどうかだけ見る
            //HotKeyの重複禁止する、という考え方もある
            return items.Length == items.Distinct().Count();
        }
    }
}
