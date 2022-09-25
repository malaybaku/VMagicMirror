using Zenject;

namespace Baku.VMagicMirror
{
    public class EyeLookAtUpdater : ILateTickable
    {
        private readonly EyeLookAt _lookAt;
        public EyeLookAtUpdater(EyeLookAt lookAt) => _lookAt = lookAt;
        public void LateTick() => _lookAt.Calculate();
    }
}
