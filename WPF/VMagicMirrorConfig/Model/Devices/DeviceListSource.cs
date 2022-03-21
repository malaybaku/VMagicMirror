using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// 接続しているマイクやカメラなどのデバイス情報を保持するクラス。
    /// 設定ファイルには残らない情報を扱っているのがポイント
    /// </summary>
    class DeviceListSource
    {
        public DeviceListSource() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public DeviceListSource(IMessageSender sender)
        {
            _sender = sender;
            MicrophoneNames = new ReadOnlyObservableCollection<string>(_microphoneNames);
            CameraNames = new ReadOnlyObservableCollection<string>(_cameraNames);
        }

        private readonly IMessageSender _sender;

        private readonly ObservableCollection<string> _microphoneNames
            = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> MicrophoneNames { get; }

        private readonly ObservableCollection<string> _cameraNames
            = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> CameraNames { get; }

        //NOTE: 何回呼び出してもOKなことに注意アプリケーション起動後に呼び直してもOK
        public async Task InitializeDeviceNamesAsync()
        {
            await InitializeMicrophoneNamesAsync();
            await InitializeCameraNamesAsync();
        }

        private async Task InitializeMicrophoneNamesAsync()
        {
            string rawNames = await _sender.QueryMessageAsync(MessageFactory.Instance.CameraDeviceNames());
            var names = DeviceNames.FromJson(rawNames, "Camera").Names;
            Application.Current.MainWindow.Dispatcher.Invoke(() =>
            {
                _microphoneNames.Clear();
                foreach (var name in names)
                {
                    _microphoneNames.Add(name);
                }
            });
        }

        private async Task InitializeCameraNamesAsync()
        {
            var rawNames = await _sender.QueryMessageAsync(MessageFactory.Instance.MicrophoneDeviceNames());
            var names = DeviceNames.FromJson(rawNames, "Microphone").Names;
            Application.Current.MainWindow.Dispatcher.Invoke(() =>
            {
                _cameraNames.Clear();
                foreach (var name in names)
                {
                    _cameraNames.Add(name);
                }
            });
        }
    }
}
