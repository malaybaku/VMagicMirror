using System;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// ホットキーを監視するすごいやつだよ
    /// </summary>
    class HotKeyObserver
    {
        readonly HotKeyWrapper _hotKeyWrapper;

        public HotKeyObserver()
        {
            _hotKeyWrapper = new HotKeyWrapper(Application.Current.MainWindow);
            _hotKeyWrapper.HotKeyActionRequested += OnActionRequested;
        }

        public event Action<HotKeyActionContent>? ActionRequested;

        //NOTE: settingの保存と関係ない部分だから別アイテムにするのもあり
        private void OnActionRequested(HotKeyActionContent content)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => ActionRequested?.Invoke(content))
            );
        }
    }
}
