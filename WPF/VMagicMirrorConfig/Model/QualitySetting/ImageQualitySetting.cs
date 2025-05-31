using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    //VMMの設定ファイルには保存されずUnity側の設定として永続化される、描画品質の設定に関するモデル
    class ImageQualitySetting
    {
        public ImageQualitySetting() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public ImageQualitySetting(IMessageSender sender)
        {
            _sender = sender;
            ImageQualityNames = new ReadOnlyObservableCollection<string>(_imageQualityNames);
            ImageQuality = new RProperty<string>("", s => _sender.SendMessage(MessageFactory.SetImageQuality(s)));
        }

        private readonly IMessageSender _sender;

        public RProperty<string> ImageQuality { get; }

        private readonly ObservableCollection<string> _imageQualityNames = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> ImageQualityNames { get; }

        public async Task InitializeQualitySelectionsAsync()
        {
            string res = await _sender.QueryMessageAsync(MessageFactory.GetQualitySettingsInfo());
            var info = ImageQualityInfo.ParseFromJson(res);
            if (info.ImageQualityNames != null &&
                info.CurrentQualityIndex >= 0 &&
                info.CurrentQualityIndex < info.ImageQualityNames.Length
                )
            {
                foreach (var name in info.ImageQualityNames)
                {
                    _imageQualityNames.Add(name);
                }
                ImageQuality.Value = info.ImageQualityNames[info.CurrentQualityIndex];
            }
        }

        public async Task ResetAsync()
        {
            var qualityName = await _sender.QueryMessageAsync(MessageFactory.ApplyDefaultImageQuality());
            if (ImageQualityNames.Contains(qualityName))
            {
                ImageQuality.Value = qualityName;
            }
            else
            {
                LogOutput.Instance.Write($"Invalid image quality `{qualityName}` applied");
            }
        }

    }
}
