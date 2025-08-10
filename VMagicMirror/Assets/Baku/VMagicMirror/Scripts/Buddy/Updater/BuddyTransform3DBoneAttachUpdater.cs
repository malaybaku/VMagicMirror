using System.Collections.Generic;
using R3;
using UniRx.Triggers;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// <see cref="BuddyTransform3DInstance"/> のアバターボーンに対する着脱を管理するクラス。それ以外は管理しない
    /// </summary>
    public class BuddyTransform3DBoneAttachUpdater : PresenterBase
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly Buddy3DInstanceCreator _instanceCreator;

        private readonly List<BuddyTransform3DInstance> _instances = new();

        private bool _hasModel;
        private Animator _animator;
        
        [Inject]
        public BuddyTransform3DBoneAttachUpdater(
            IVRMLoadable vrmLoadable,
            Buddy3DInstanceCreator instanceCreator)
        {
            _vrmLoadable = vrmLoadable;
            _instanceCreator = instanceCreator;
        }

        public override void Initialize()
        {
            _vrmLoadable.PostVrmLoaded += OnModelLoaded;
            _vrmLoadable.VrmDisposing += OnModelUnloaded;
            
            _instanceCreator.Transform3DCreated
                .Subscribe(instance =>
                {
                    _instances.Add(instance);

                    instance.ParentBone
                        .Subscribe(_ => UpdateParentBone(instance))
                        .AddTo(instance);

                    instance.OnDestroyAsObservable()
                        .Subscribe(_ => _instances.Remove(instance))
                        .AddTo(instance);
                    
                })
                .AddTo(this);
        }

        private void OnModelLoaded(VrmLoadedInfo info)
        {
            _animator = info.animator;
            _hasModel = true;
            UpdateAllParentBone();
        }

        private void OnModelUnloaded()
        {
            _hasModel = false;
            _animator = null;
            UpdateAllParentBone();
        }

        private void UpdateAllParentBone()
        {
            foreach (var instance in _instances)
            {
                if (instance.ParentBone.Value == HumanBodyBones.LastBone)
                {
                    continue;
                }

                if (_hasModel)
                {
                    instance.SetParentAvatarBone(
                        _animator.GetBoneTransformAscending(instance.ParentBone.Value)
                        );
                }
                else
                {
                    instance.RemoveParentAvatarBone();
                }
            }
        }

        private void UpdateParentBone(BuddyTransform3DInstance instance)
        {
            if (!_hasModel)
            {
                return;
            }

            if (instance.ParentBone.Value == HumanBodyBones.LastBone)
            {
                instance.RemoveParentAvatarBone();
            }
            else
            {
                instance.SetParentAvatarBone(
                    _animator.GetBoneTransformAscending(instance.ParentBone.Value)
                );
            }
        }
    }
}
