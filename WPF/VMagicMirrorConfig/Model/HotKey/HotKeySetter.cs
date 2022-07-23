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

            //NOTE: _modelの初期化待ちをこのクラスでやるように追記してもよさそう
            _setting.Updated += (_, __) => OnSettingUpdated();
            _setting.SingleItemUpdated += OnSettingUpdated;
        }

        private readonly HotKeyModel _model;
        private readonly HotKeySettingModel _setting;
        private readonly List<HotKeyRegisterItem> _latestRegisterItems = new List<HotKeyRegisterItem>();

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
            }

            foreach(var itemToAdd in items.Where(i => !_latestRegisterItems.Contains(i)))
            {
                _model.Register(itemToAdd);
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
