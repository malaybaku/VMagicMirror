using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class SpineBoneModifier : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly IVRMLoadable _vrmLoadable;

        private bool _hasModel;
        private readonly List<Transform> _spineBones = new();
        // NOTE: neckがない場合はheadが入る
        private Transform _neckBone;
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
            _spineBones.Clear();
            if (info.AvatarBones.Spine != null) _spineBones.Add(info.AvatarBones.Spine);
            if (info.AvatarBones.Chest != null) _spineBones.Add(info.AvatarBones.Chest);
            if (info.AvatarBones.UpperChest != null) _spineBones.Add(info.AvatarBones.UpperChest);

            _neckBone = info.AvatarBones.Neck;
            if (_neckBone == null)
            {
                _neckBone = info.AvatarBones.Head;
            }
            
            _hasModel = true;
        }

        private void OnVrmUnloaded()
        {
            _hasModel = false;
            _spineBones.Clear();
            _neckBone = null;
        }

        public void Apply()
        {
            if (!_hasModel)
            {
                return;
            }

            // TODO: なんかする
        }
    }
}
