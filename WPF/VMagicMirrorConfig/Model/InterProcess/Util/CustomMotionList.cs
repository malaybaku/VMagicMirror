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

        private readonly ObservableCollection<string> _customMotionClipNames = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> CustomMotionClipNames { get; }

        private readonly ObservableCollection<string> _vrmaCustomMotionClipNames = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> VrmaCustomMotionClipNames { get; }

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
        }

        private async Task<string[]> GetCustomMotionClipNamesAsync(bool vrmaOnly)
        {
            var rawClipNames = await _sender.QueryMessageAsync(
                MessageFactory.Instance.GetAvailableCustomMotionClipNames(vrmaOnly));
            return rawClipNames.Split('\t');
        }
    }
}
