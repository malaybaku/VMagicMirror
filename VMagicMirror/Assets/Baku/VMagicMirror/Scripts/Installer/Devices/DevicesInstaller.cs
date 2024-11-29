using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary> デバイスの座標周りの処理をインジェクトしてくれるすごいやつだよ </summary>
    public class DevicesInstaller : InstallerBase, IDevicesRoot
    {
        [SerializeField] private DeviceTransformController controller = null;
        [SerializeField] private GamepadProvider gamepadProvider = null;
        [SerializeField] private ArcadeStickProvider arcadeStickProvider = null;
        [SerializeField] private CarHandleProvider carHandleProvider;
        [SerializeField] private KeyboardProvider keyboardProvider = null;
        [SerializeField] private TouchPadProvider touchPadProvider = null;
        [SerializeField] private PenTabletProvider penTabletProvider = null;
        [SerializeField] private PenController penController = null;
        [SerializeField] private MidiControllerProvider midiControllerProvider = null;
        [SerializeField] private ParticleStore particleStore = null;
        [SerializeField] private MangaParticleView mangaParticleView = null;

        public Transform Transform => transform;
        
        public override void Install(DiContainer container)
        {
            //prefabの各要素がワールド直下じゃなくてこのオブジェクト以下にぶら下がれるように、親っぽい雰囲気を出しておく
            container.Bind<IDevicesRoot>()
                .FromInstance(this)
                .AsCached();

            container.BindInstance(controller);

            container.BindInterfacesAndSelfTo<DeviceVisibilityRepository>().AsSingle();
            container.BindInterfacesAndSelfTo<ParticleModeController>().AsSingle();
            
            // NOTE: マンガ風パーティクルは構造上はほかのパーティクルと違ってデバイス類との関連が薄いが、似てはいるのでここで管理してる
            container.BindInstance(mangaParticleView);
            container.BindInterfacesTo<MangaParticleController>().AsSingle();

            //NOTE: ペンタブより先にバインドしといたほうが無難(PenTabletProvider側で必要)
            container.Bind<PenController>()
                .FromComponentInNewPrefab(penController)
                .AsCached();

            container.Bind<GamepadProvider>()
                .FromComponentInNewPrefab(gamepadProvider)
                .AsCached();
            container.Bind<KeyboardProvider>()
                .FromComponentInNewPrefab(keyboardProvider)
                .AsCached();
            container.Bind<TouchPadProvider>()
                .FromComponentInNewPrefab(touchPadProvider)
                .AsCached();
            container.Bind<MidiControllerProvider>()
                .FromComponentInNewPrefab(midiControllerProvider)
                .AsCached();
            container.Bind<ParticleStore>()
                .FromComponentInNewPrefab(particleStore)
                .AsCached();

            container.Bind<ArcadeStickProvider>()
                .FromComponentInNewPrefab(arcadeStickProvider)
                .AsCached();
            container.Bind<PenTabletProvider>()
                .FromComponentInNewPrefab(penTabletProvider)
                .AsCached();

            container.Bind<CarHandleProvider>()
                .FromComponentInNewPrefab(carHandleProvider)
                .AsCached();
        }
    }

    public interface IDevicesRoot
    {
        Transform Transform { get; }
    }
}
