using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(BlendShapeStore))]
    [RequireComponent(typeof(FaceDetector))]
    [RequireComponent(typeof(FaceBlendShapeController))]
    public class BlendShapeAssignController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler;

        [SerializeField]
        private GrpcSender sender;

        private BlendShapeStore _blendShapeStore = null;

        public EyebrowBlendShapeSet EyebrowBlendShape { get; } = new EyebrowBlendShapeSet();

        void Start()
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
                    default:
                        break;
                }
            });
            handler.QueryRequested += OnQueryReceived;
        }

        public void InitializeModel(Transform vrmRoot)
        {
            _blendShapeStore.Initialize(vrmRoot);
            EyebrowBlendShape.RefreshTarget(_blendShapeStore);
            //StartCoroutine(SendBlendShapeNamesDelay(0.5f, _blendShapeStore.GetBlendShapeNames()));
        }

        public void DisposeModel()
        {
            _blendShapeStore.Dispose();
            EyebrowBlendShape.Reset();
        }

        public string[] TryGetBlendShapeNames() => _blendShapeStore.GetBlendShapeNames();

        public void SendBlendShapeNames()
            => sender.SendCommand(MessageFactory.Instance.SetBlendShapeNames(
                string.Join("\t", TryGetBlendShapeNames())
                ));

        private void OnQueryReceived(object sender, ReceivedMessageHandler.QueryEventArgs e)
        {
            switch(e.Query.Command)
            {
                case MessageQueryNames.GetBlendShapeNames:
                    e.Query.Result = string.Join("\t", _blendShapeStore.GetBlendShapeNames());
                    break;
                default:
                    break;
            }
        }

    }
}
