using Baku.VMagicMirror.MediaPipeTracker;
using UniRx;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 表情制御の一番か二番目くらいに偉いやつ。VRMBlendShapeProxy.Applyをする権利を保有する。
    /// </summary>
    public class FaceControlManager : MonoBehaviour
    {
        //NOTE: まばたき自体は3種類どれかが排他で適用される。複数走っている場合、external > image > autoの優先度で適用する。
        [SerializeField] private ExternalTrackerBlink externalTrackerBlink = null;
        [SerializeField] private ImageBasedBlinkController imageBasedBlinkController = null;
        [SerializeField] private VRMAutoBlink autoBlink = null;
        
        [SerializeField] private EyeJitter randomEyeJitter = null;
        [SerializeField] private ExternalTrackerEyeJitter externalTrackEyeJitter = null;
        
        private bool _hasModel = false;
        private FaceControlConfiguration _config;
        private MediaPipeBlink _mediaPipeBlink;
        private MediaPipeEyeJitter _mediaPipeEyeJitter;

        // WebCam (低負荷) でのトラッキング中に自動瞬きを使う場合はtrue
        private readonly ReactiveProperty<bool> AutoBlinkOnWebCamLowPower = new(true);
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, IMessageReceiver receiver, IMessageSender sender, 
            FaceControlConfiguration config,
            MediaPipeBlink mediaPipeBlink,
            MediaPipeEyeJitter mediaPipeEyeJitter)
        {
            _config = config;
            _mediaPipeBlink = mediaPipeBlink;
            _mediaPipeEyeJitter = mediaPipeEyeJitter;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
            
            receiver.BindBoolProperty(VmmCommands.AutoBlinkDuringFaceTracking, AutoBlinkOnWebCamLowPower);
            receiver.AssignCommandHandler(
                VmmCommands.FaceDefaultFun,
                message => DefaultBlendShape.FaceDefaultFunValue = message.ParseAsPercentage()
            );
        }
        
        public DefaultFunBlendShapeModifier DefaultBlendShape { get; } = new();

        public void Accumulate(ExpressionAccumulator accumulator, float weight = 1f)
        {
            if (!_hasModel)
            {
                return;
            }
            
            //NOTE: ここのデフォルトfunだが
            //「パーフェクトシンク使用中」「FaceSwitch適用中」「Word to Motion適用中」
            //の3ケースでは適用されると困る。
            //で、ここに書いておくと上記3ケースではそもそもAccumulateが呼ばれないため、うまく動く。
            DefaultBlendShape.Apply(accumulator);

            var blinkSource = _config.ControlMode switch
            {
                FaceControlModes.ExternalTracker => externalTrackerBlink.BlinkSource,
                // NOTE: ここでIsTrackedも検証しておくパターンもアリ
                FaceControlModes.WebCamHighPower => _mediaPipeBlink.BlinkSource,
                FaceControlModes.WebCamLowPower when !AutoBlinkOnWebCamLowPower.Value
                    => imageBasedBlinkController.BlinkSource,
                _ => autoBlink.BlinkSource
            };

            accumulator.Accumulate(ExpressionKey.BlinkLeft, blinkSource.Left * weight);
            accumulator.Accumulate(ExpressionKey.BlinkRight, blinkSource.Right * weight);
        }

        private void Update()
        {
            // TODO: VMCPで目ボーンの上書きしたいケースがカバーできてないかも…？

            // 眼球運動はモード別で切り替える。
            // 外部トラッキングや高負荷カメラでは検出結果にLookAtが入ってるので、それをそのまま使う…という話
            switch (_config.ControlMode)
            {
                // NOTE: Trackedではない場合にも各々のEyeJitterに帰着するようにするのもアリ
                // (「自動のとトラッキングのが頻繁に切り替わると見た目が悪い」みたいな問題が起こったら特に改変すべき)
                case FaceControlModes.ExternalTracker when externalTrackEyeJitter.IsTracked:
                    externalTrackEyeJitter.IsActive = true;
                    _mediaPipeEyeJitter.IsActive = false;
                    randomEyeJitter.IsActive = false;
                    break;
                case FaceControlModes.WebCamHighPower when _mediaPipeEyeJitter.IsEnabledAndTracked:
                    externalTrackEyeJitter.IsActive = false;
                    _mediaPipeEyeJitter.IsActive = true;
                    randomEyeJitter.IsActive = false;
                    break;
                default:
                    externalTrackEyeJitter.IsActive = false;
                    _mediaPipeEyeJitter.IsActive = false;
                    randomEyeJitter.IsActive = true;
                    break;
            }
        }
                
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
        }
    }
    
    
    /// <summary> まばたき状態の値を提供します。 </summary>
    public interface IBlinkSource
    {
        float Left { get; }
        float Right { get; }
    }

    /// <summary> 単なるプロパティで<see cref="IBlinkSource"/>を実装します。 </summary>
    public class RecordBlinkSource : IBlinkSource
    {
        public float Left { get; set; }
        public float Right { get; set; }
    }
}
