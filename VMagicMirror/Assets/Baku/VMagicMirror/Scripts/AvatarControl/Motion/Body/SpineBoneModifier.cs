using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class SpineBoneModifier : PresenterBase, ITickable
    {
        private readonly IMessageReceiver _receiver;
        private readonly IVRMLoadable _vrmLoadable;

        private bool _hasModel;
        private static readonly float[] SpineBoneAngleWeights = { 0.4f, 0.4f, 0.2f };
        private readonly Transform[] _spineBones = new Transform[3];
        // neckがない場合はhead
        private Transform _neckBone;
        // shoulderがない場合はupperArm
        private Transform _leftShoulder;
        private Transform _rightShoulder;
        
        private readonly ReactiveProperty<int> _spineAngleOffset = new(0);
        
        public SpineBoneModifier(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
        }
        
        public override void Initialize()
        {
            _receiver.BindIntProperty(VmmCommands.SetSpineAngleOffset, _spineAngleOffset);

            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _spineBones[0] = info.AvatarBones.Spine;
            _spineBones[1] = info.AvatarBones.Chest;
            _spineBones[2] = info.AvatarBones.UpperChest;
            if (_spineBones[1] == null) _spineBones[1] = _spineBones[0];
            if (_spineBones[2] == null) _spineBones[2] = _spineBones[1];

            _neckBone = info.AvatarBones.Neck;
            if (_neckBone == null) _neckBone = info.AvatarBones.Head;
            
            _leftShoulder = info.AvatarBones.LeftShoulder;
            if (_leftShoulder == null) _leftShoulder = info.AvatarBones.LeftUpperArm;

            _rightShoulder = info.AvatarBones.RightShoulder;
            if (_rightShoulder == null) _rightShoulder = info.AvatarBones.RightUpperArm;
            
            _hasModel = true;
        }

        private void OnVrmUnloaded()
        {
            _hasModel = false;
            _spineBones[0] = null;
            _spineBones[1] = null;
            _spineBones[2] = null;
            _neckBone = null;
            _leftShoulder = null;
            _rightShoulder = null;
        }

        void ITickable.Tick() => Apply();
        
        public void Apply()
        {
            if (!_hasModel)
            {
                return;
            }

            var angle = (float) _spineAngleOffset.CurrentValue;
            for (var i = 0; i < _spineBones.Length; i++)
            {
                var spineBone = _spineBones[i];
                spineBone.localRotation *= Quaternion.Euler(angle * SpineBoneAngleWeights[i], 0, 0);
            }

            // 肩ボーンも逆回転させないと腕が後ろ向きになっちゃうので少し打ち消す。多少腰より後ろに行くようにする
            _neckBone.localRotation *= Quaternion.Euler(-angle, 0, 0);
            _leftShoulder.localRotation *= Quaternion.Euler(-angle * .4f, 0, 0);
            _rightShoulder.localRotation *= Quaternion.Euler(-angle * .4f, 0, 0);
        }
    }
}
