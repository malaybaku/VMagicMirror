using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(PostProcessLayer))]
    public class EnvironmentInstaller : InstallerBase
    {
        public override void Install(DiContainer container)
        {
            container.Bind<Camera>().FromMethod(_ => GetComponent<Camera>()).AsCached();
            container.Bind<PostProcessLayer>().FromMethod(_ => GetComponent<PostProcessLayer>()).AsCached();
        }
    }
}
