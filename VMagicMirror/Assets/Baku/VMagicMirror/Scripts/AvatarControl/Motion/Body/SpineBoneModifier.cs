using System.Collections.Generic;
using R3;
using RootMotion.FinalIK;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class SpineBoneModifier : PresenterBase
    {
        private const float PositiveAngleFactor = 0.3f;
        private const float NegativeAngleFactor = 0.2f;
        private readonly IMessageReceiver _receiver;
        private readonly IVRMLoadable _vrmLoadable;

        private bool _hasModel;
        private readonly List<Transform> _spineBones = new(3);
        // neckがない場合はhead
        private Transform _neckBone;
        private FullBodyBipedIK _fbbik;
        
        private readonly ReactiveProperty<int> _spineAngleOffset = new(0);
        
        public SpineBoneModifier(IMessageReceiver receiver, IVRMLoadable vrmLoadable)
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
            if (info.AvatarBones.Spine != null) _spineBones.Add(info.AvatarBones.Spine);
            if (info.AvatarBones.Chest != null) _spineBones.Add(info.AvatarBones.Chest);
            if (info.AvatarBones.UpperChest != null) _spineBones.Add(info.AvatarBones.UpperChest);

            _neckBone = info.AvatarBones.Neck;
            if (_neckBone == null) _neckBone = info.AvatarBones.Head;

            _fbbik = info.FbbIk;
            _fbbik.solver.OnPreRead += Apply;
            
            _hasModel = true;
        }

        private void OnVrmUnloaded()
        {
            _hasModel = false;
            
            if (_fbbik != null)
            {
                _fbbik.solver.OnPreRead -= Apply;
            }
            _fbbik = null;
            _spineBones.Clear();
            _neckBone = null;
        }

        private void Apply()
        {
            if (!_hasModel || _spineAngleOffset.CurrentValue == 0)
            {
                return;
            }

            var angleFactor = _spineAngleOffset.CurrentValue > 0 ? PositiveAngleFactor : NegativeAngleFactor;
            var angle = _spineAngleOffset.CurrentValue * angleFactor;
            var dividedAngle = angle / _spineBones.Count;
            foreach (var spineBone in _spineBones)
            {
                spineBone.localRotation *= Quaternion.Euler(dividedAngle, 0, 0);
            }

            // 腰を曲げたぶんが頭の向きに影響しないように打ち消す
            _neckBone.localRotation *= Quaternion.Euler(-angle, 0, 0);
        }
    }
}
