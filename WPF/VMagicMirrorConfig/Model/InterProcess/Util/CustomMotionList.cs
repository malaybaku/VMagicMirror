using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    class CustomMotionList
    {
        public CustomMotionList() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }
        public CustomMotionList(IMessageSender sender)
        {
            _sender = sender;
            CustomMotionClipNames = new ReadOnlyObservableCollection<string>(_customMotionClipNames);
            VrmaCustomMotionClipNames = new ReadOnlyObservableCollection<string>(_vrmaCustomMotionClipNames);
        }

        private readonly IMessageSender _sender;

        private readonly ObservableCollection<string> _customMotionClipNames = new();
        public ReadOnlyObservableCollection<string> CustomMotionClipNames { get; }

        private readonly ObservableCollection<string> _vrmaCustomMotionClipNames = new();
        public ReadOnlyObservableCollection<string> VrmaCustomMotionClipNames { get; }

        private readonly TaskCompletionSource<bool> _initializeTcs = new();

        public async Task WaitCustomMotionInitializeAsync() => await _initializeTcs.Task;

        private readonly object _isInitializedLock = new();
        private bool _isInitialized = false;
        public bool IsInitialized
        {
            get { lock (_isInitializedLock) return _isInitialized; }
            private set { lock (_isInitializedLock) _isInitialized = value; }
        }

        public async Task InitializeCustomMotionClipNamesAsync()
        {
            //NOTE: 2回取得するのは若干もっさりするが、アプリ起動後の1回だけなので許容しておく
            var clipNames = await GetCustomMotionClipNamesAsync(false);
            foreach (var name in clipNames)
            {
                _customMotionClipNames.Add(name);
            }

            var vrmaClipNames = await GetCustomMotionClipNamesAsync(true);
            foreach (var name in vrmaClipNames)
            {
                _vrmaCustomMotionClipNames.Add(name);
            }

            IsInitialized = true;
            _initializeTcs.SetResult(true);
        }

        private async Task<string[]> GetCustomMotionClipNamesAsync(bool vrmaOnly)
        {
            var rawClipNames = await _sender.QueryMessageAsync(
                MessageFactory.Instance.GetAvailableCustomMotionClipNames(vrmaOnly));
            return rawClipNames.Split('\t');
        }
    }
}
