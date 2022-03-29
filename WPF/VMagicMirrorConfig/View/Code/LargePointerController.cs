namespace Baku.VMagicMirrorConfig.View
{
    /// <summary> ポインターを表示したり隠したりする、Windowのラッパーみたいなクラス。 </summary>
    class LargePointerController
    {
        private LargePointerController() { }
        private static LargePointerController? _instance = null;
        internal static LargePointerController Instance
            => _instance ??= new LargePointerController();

        public bool IsVisible { get; private set; } = false;

        private LargeMousePointerWindow? _window = null;

        public void UpdateVisibility(bool visible)
        {
            if (visible == IsVisible)
            {
                return;
            }

            IsVisible = visible;
            if (visible)
            {
                Show();
            }
            else
            {
                Close();
            }
        }

        public void Show()
        {
            if (_window == null)
            {
                _window = new LargeMousePointerWindow();
                _window.Show();
            }
            IsVisible = true;
        }

        public void Close()
        {
            if (_window != null)
            {
                var window = _window;
                _window = null;
                window.Close();
            }
            IsVisible = false;
        }
    }
}
