using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    //NOTE: もし必要ならゲームパッドとかも追加してOK
    /// <summary> 接続しているマイクやカメラなどのデバイス情報を保持する  </summary>
    class DeviceListSource
    {
        //あまり頻繁にポーリングしないでも良いと思うが、これ以上遅いとしんどそう
        private const int DeviceNamesPollingIntervalMillisec = 5000;
        private const string MicrophoneDisconnectedFormatKey = "Snackbar_MicrophoneDisconnected_Format";
        private const string MicrophoneReconnectedFormatKey = "Snackbar_MicrophoneReconnected_Format";
        private const string CameraReconnectedFormatKey = "Snackbar_CameraReconnected_Format";

        public DeviceListSource() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>()
            )
        {
        }

        public DeviceListSource(IMessageSender sender, MotionSettingModel setting)
        {
            _sender = sender;
            _setting = setting;
            MicrophoneNames = new ReadOnlyObservableCollection<string>(_microphoneNames);
            CameraNames = new ReadOnlyObservableCollection<string>(_cameraNames);
        }

        private readonly IMessageSender _sender;
        private readonly MotionSettingModel _setting;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly ObservableCollection<string> _microphoneNames = new();
        public ReadOnlyObservableCollection<string> MicrophoneNames { get; }

        private readonly ObservableCollection<string> _cameraNames = new();
        public ReadOnlyObservableCollection<string> CameraNames { get; }

        //NOTE: 何回呼び出してもOKなことに注意アプリケーション起動後に呼び直してもOK
        public async Task InitializeDeviceNamesAsync()
        {
            await InitializeMicrophoneNamesAsync(true);
            await InitializeCameraNamesAsync(true);
            new Thread(() => PollingDeviceNames(_cts.Token)).Start();
        }

        public void Dispose() => _cts.Cancel();

        public async Task ReloadDevicesAsync()
        {
            var micIsInDeviceNames = _microphoneNames.Contains(_setting.LipSyncMicrophoneDeviceName.Value);
            var webcamIsInDeviceNames = _cameraNames.Contains(_setting.CameraDeviceName.Value);

            await InitializeMicrophoneNamesAsync(false);
            await InitializeCameraNamesAsync(false);

            //NOTE:
            // - 選択中のデバイスが未接続→接続に切り替わったと考えられる場合、明示的なメッセージによって(必要なら)リップシンクなどが始まる
            // - マイクに関しては接続→未接続の切り替えも検出するが、特に突っ込んでできることは無いので、トーストだけ表示しておく

            var microphoneName = _setting.LipSyncMicrophoneDeviceName.Value;
            var micIsInDeviceNamesNew = _microphoneNames.Contains(_setting.LipSyncMicrophoneDeviceName.Value);
            if (!string.IsNullOrEmpty(microphoneName))
            {
                if (!micIsInDeviceNames && micIsInDeviceNamesNew)
                {
                    _sender.SendMessage(MessageFactory.Instance.SetMicrophoneDeviceName(microphoneName));
                    SnackbarWrapper.Enqueue(string.Format(
                        LocalizedString.GetString(MicrophoneReconnectedFormatKey),
                        microphoneName
                    ));
                }
                else if (micIsInDeviceNames && !micIsInDeviceNamesNew)
                {
                    SnackbarWrapper.Enqueue(string.Format(
                        LocalizedString.GetString(MicrophoneDisconnectedFormatKey),
                        microphoneName
                    ));
                }
            }

            var cameraName = _setting.CameraDeviceName.Value;
            if (!string.IsNullOrEmpty(cameraName) && 
                !webcamIsInDeviceNames &&
                _cameraNames.Contains(_setting.CameraDeviceName.Value))
            {
                _sender.SendMessage(MessageFactory.Instance.SetCameraDeviceName(cameraName));
                SnackbarWrapper.Enqueue(string.Format(
                    LocalizedString.GetString(CameraReconnectedFormatKey),
                    cameraName
                ));
            }
        }

        private async Task InitializeMicrophoneNamesAsync(bool refresh)
        {
            string rawNames = await _sender.QueryMessageAsync(MessageFactory.Instance.CameraDeviceNames());
            var names = DeviceNames.FromJson(rawNames, "Camera").Names;
            Application.Current.MainWindow.Dispatcher.Invoke(() =>
            {
                if (refresh)
                {
                    _cameraNames.Clear();
                    foreach (var name in names)
                    {
                        _cameraNames.Add(name);
                    }
                }
                else
                {
                    AlignWithFewestRemove(names, _cameraNames);
                }
            });
        }

        private async Task InitializeCameraNamesAsync(bool refresh)
        {
            var rawNames = await _sender.QueryMessageAsync(MessageFactory.Instance.MicrophoneDeviceNames());
            var names = DeviceNames.FromJson(rawNames, "Microphone").Names;
            Application.Current.MainWindow.Dispatcher.Invoke(() =>
            {
                if (refresh)
                {
                    _microphoneNames.Clear();
                    foreach (var name in names)
                    {
                        _microphoneNames.Add(name);
                    }
                }
                else
                {
                    AlignWithFewestRemove(names, _microphoneNames);
                }
            });
        }

        private async void PollingDeviceNames(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(DeviceNamesPollingIntervalMillisec, cancellationToken);
                await ReloadDevicesAsync();
            }
        }

        //OC<T>の要素の削除処理を最小限にしつつ(※ComboBoxにバインドしたときの挙動を安全にするため)、
        //destの中身がsrcと同じになるように更新する
        private static void AlignWithFewestRemove(string[] src, ObservableCollection<string> dest)
        {
            for (int i = 0; i < src.Length; i++)
            {
                var name = src[i];
                if (i < dest.Count && dest[i] == name)
                {
                    //正しい
                }
                else if (dest.Contains(name))
                {
                    var index = dest.IndexOf(name);
                    dest.Move(index, i);
                }
                else
                {
                    dest.Insert(i, name);
                }
            }

            while (dest.Count > src.Length)
            {
                dest.RemoveAt(src.Length);
            }
        }
    }
}
