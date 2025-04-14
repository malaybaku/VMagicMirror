using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPFaceSwitchSetter : ITickable
    {
        private readonly VMCPBlendShape _blendShape;
        private readonly FaceSwitchExtractor _faceSwitch;

        [Inject]
        public VMCPFaceSwitchSetter(VMCPBlendShape blendShape, FaceSwitchExtractor faceSwitch)
        {
            _blendShape = blendShape;
            _faceSwitch = faceSwitch;
        }

        void ITickable.Tick()
        {
            // NOTE: 単にブレンドシェイプを受信していることだけでなく、
            // 「パーフェクトシンクっぽいデータを受け取っている」という条件もつけていることに注意
            if (_blendShape.IsActive.Value && _blendShape.SeemsToHavePerfectSyncData.Value)
            {
                _faceSwitch.Update(_blendShape.FaceTrackBlendShapes);
            }
        }
    }    
}
