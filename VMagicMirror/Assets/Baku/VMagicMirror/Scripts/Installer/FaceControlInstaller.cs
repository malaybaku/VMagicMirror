using Baku.VMagicMirror.Buddy;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary> 顔周りでなんかインストールするやつ </summary>
    public class FaceControlInstaller : InstallerBase
    {
        [SerializeField] private VmmLipSyncContextBase lipSyncContext = null;
        [SerializeField] private LipSyncIntegrator lipSyncIntegrator = null;
        [SerializeField] private VRMAutoBlink autoBlink = null;
        [SerializeField] private EyeBoneAngleSetter eyeBoneAngleSetter = null;
        [SerializeField] private ExternalTrackerBlink externalTrackerBlink;

        public override void Install(DiContainer container)
        {
            container.Bind<VmmLipSyncContextBase>().FromInstance(lipSyncContext).AsCached();
            container.BindInstances(
                lipSyncIntegrator,
                autoBlink,
                eyeBoneAngleSetter,
                externalTrackerBlink
            );

            // NOTE: VoiceOnOffがFaceの一種かというとかなりアヤシイが、LipSyncとの関連が強いのでここでBindしてる
            container.BindInterfacesAndSelfTo<VoiceOnOffParser>().AsSingle();
            container.BindInterfacesAndSelfTo<ExpressionAccumulator>().AsSingle();
            container.Bind<EyeLookAt>().AsSingle();
            container.BindInterfacesTo<EyeLookAtUpdater>().AsSingle();
            // TODO: BlinkDetectorに乗り換えたい
            container.BindInterfacesAndSelfTo<BlinkTriggerDetector>().AsSingle();
            // TODO: BlinkDetectorはフォルダをFaceControlっぽいとこに移動したほうが良さそう
            container.BindInterfacesAndSelfTo<BlinkDetector>().AsSingle();
            container.BindInterfacesTo<FaceControlConfigurationReceiver>().AsSingle();

            container.Bind<CameraDeviceListQueryHandler>().AsSingle();
            
            //ブレンドシェイプの内訳の確認処理で、意味のある処理ではないけど一応つねに入れておく
            container.Bind<BlendShapeExclusivenessChecker>().AsSingle();
            container.Bind<UserOperationBlendShapeResultRepository>().AsSingle();
            
            
        }
    }
}
