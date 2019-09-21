using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(BlendShapeStore))]
    public class BlendShapeAssignController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler;

        [SerializeField]
        private GrpcSender sender;

        private BlendShapeStore _blendShapeStore;

        public EyebrowBlendShapeSet EyebrowBlendShape { get; } = new EyebrowBlendShapeSet();

        private void Start()
        {
            _blendShapeStore = GetComponent<BlendShapeStore>();
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.EyebrowLeftUpKey:
                        EyebrowBlendShape.LeftUpKey = message.Content;
                        EyebrowBlendShape.RefreshTarget(_blendShapeStore);
                        break;
                    case MessageCommandNames.EyebrowLeftDownKey:
                        EyebrowBlendShape.LeftDownKey = message.Content;
                        EyebrowBlendShape.RefreshTarget(_blendShapeStore);
                        break;
                    case MessageCommandNames.UseSeparatedKeyForEyebrow:
                        EyebrowBlendShape.UseSeparatedTarget = message.ToBoolean();
                        EyebrowBlendShape.RefreshTarget(_blendShapeStore);
                        break;
                    case MessageCommandNames.EyebrowRightUpKey:
                        EyebrowBlendShape.RightUpKey = message.Content;
                        EyebrowBlendShape.RefreshTarget(_blendShapeStore);
                        break;
                    case MessageCommandNames.EyebrowRightDownKey:
                        EyebrowBlendShape.RightDownKey = message.Content;
                        EyebrowBlendShape.RefreshTarget(_blendShapeStore);
                        break;
                    case MessageCommandNames.EyebrowUpScale:
                        EyebrowBlendShape.UpScale = message.ParseAsPercentage();
                        break;
                    case MessageCommandNames.EyebrowDownScale:
                        EyebrowBlendShape.DownScale = message.ParseAsPercentage();
                        break;
                }
            });
            handler.QueryRequested += OnQueryReceived;
        }

        public void OnVrmLoaded(VrmLoadedInfo info)
        {
            _blendShapeStore.Initialize(info.vrmRoot);
            EyebrowBlendShape.RefreshTarget(_blendShapeStore);
            SendBlendShapeNames();
        }

        public void OnVrmDisposing()
        {
            _blendShapeStore.Dispose();
            EyebrowBlendShape.Reset();
        }

        public string[] TryGetBlendShapeNames() => _blendShapeStore.GetBlendShapeNames();

        public void SendBlendShapeNames()
            => sender.SendCommand(MessageFactory.Instance.SetBlendShapeNames(
                string.Join("\t", TryGetBlendShapeNames())
                ));

        private void OnQueryReceived(ReceivedQuery query)
        {
            if (query.Command == MessageQueryNames.GetBlendShapeNames)
            {
                query.Result = string.Join("\t", _blendShapeStore.GetBlendShapeNames());
            }
        }

    }
}
