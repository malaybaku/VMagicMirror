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
        }

        private readonly IMessageSender _sender;

        private readonly ObservableCollection<string> _customMotionClipNames = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> CustomMotionClipNames { get; }

        public async Task InitializeCustomMotionClipNamesAsync()
        {
            var clipNames = await GetAvailableCustomMotionClipNamesAsync();
            foreach (var name in clipNames)
            {
                _customMotionClipNames.Add(name);
            }
        }

        private async Task<string[]> GetAvailableCustomMotionClipNamesAsync()
        {
            var rawClipNames = await _sender.QueryMessageAsync(MessageFactory.Instance.GetAvailableCustomMotionClipNames());
            return rawClipNames.Split('\t');
        }
    }
}
