using System;
using Baku.VMagicMirror.GameInput;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class GamepadInputBodyMotionController : IInitializable, ITickable, IDisposable
    {
        private static readonly int Active = Animator.StringToHash("Active");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int MoveRight = Animator.StringToHash("MoveRight");
        private static readonly int MoveForward = Animator.StringToHash("MoveForward");
        private static readonly int Crouch = Animator.StringToHash("Crouch");

        private readonly IVRMLoadable _vrmLoadable;
        private readonly IGameInputSourceSwitcher _sourceSwitcher;
        private readonly IkWeightCrossFade _ikWeightCrossFade;
        private readonly ReactiveProperty<bool> _bodyMotionActive = new ReactiveProperty<bool>(false);
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        private bool _hasModel;
        private Animator _animator;

        public GamepadInputBodyMotionController(
            IVRMLoadable vrmLoadable,
            IGameInputSourceSwitcher sourceSwitcher,
            IkWeightCrossFade ikWeightCrossFade)
        {
            _vrmLoadable = vrmLoadable;
            _sourceSwitcher = sourceSwitcher;
            _ikWeightCrossFade = ikWeightCrossFade;
        }

        void IInitializable.Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;

            _sourceSwitcher.Source
                .Select(source => source.Jump)
                .Switch()
                .Subscribe(_ => TryJump())
                .AddTo(_disposable);

            _bodyMotionActive
                .Subscribe(v => RequestIkWeight(v ? 0f : 1f))
                .AddTo(_disposable);
        }

        private void OnVrmLoaded(VrmLoadedInfo obj)
        {
            _animator = obj.animator;
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
            _animator = null;
        }

        private void TryJump()
        {
            if (!_hasModel)
            {
                return;
            }
            _animator.SetTrigger(Jump);
        }

        private void RequestIkWeight(float targetWeight) 
            => _ikWeightCrossFade.SetBodyMotionBasedIkWeightRequest(targetWeight);

        //NOTE: イベント入力との順序とか踏まえてLateTickにしてもいい
        void ITickable.Tick()
        {
            if (!_hasModel)
            {
                return;
            }

            var source = _sourceSwitcher.Source.Value;
            _bodyMotionActive.Value = source.IsActive;
            if (!source.IsActive)
            {
                return;
            }


            var moveInput = source.MoveInput;
            _animator.SetBool(Active, source.IsActive);
            _animator.SetFloat(MoveRight, moveInput.x);
            _animator.SetFloat(MoveForward, moveInput.y);
            _animator.SetBool(Crouch, source.IsCrouching);
        }

        void IDisposable.Dispose() => _disposable.Dispose();
    }
}
