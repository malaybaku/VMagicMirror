using Baku.VMagicMirror.Buddy;
using Baku.VMagicMirror.MediaPipeTracker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    [CreateAssetMenu(menuName = "VMagicMirror/VMagicMirrorSettingsInstaller")]
    public class VMagicMirrorSettingsInstaller : ScriptableObjectInstaller
    {
        [SerializeField] private LocomotionSupportedAnimatorControllers locomotionSupportedAnimatorControllers;
        [SerializeField] private WebCamSettings webCamSettings;
        [SerializeField] private MediapipePoseSetterSettings poseSetterSettings;
        [SerializeField] private LandmarksVisualizer landmarksVisualizerPrefab;
        [SerializeField] private BuddyPresetResources buddyPresetResources;
        //NOTE: 本来ここでInstallするほうが良いものが他にもありそう

        public override void InstallBindings()
        {
            Container.BindInstance(locomotionSupportedAnimatorControllers);
            Container.BindInstance(webCamSettings);
            Container.BindInstance(poseSetterSettings);
            Container.BindInstance(buddyPresetResources);

            Container.Bind<LandmarksVisualizer>()
                .FromComponentInNewPrefab(landmarksVisualizerPrefab)
                .AsSingle();
        }
    }
}
