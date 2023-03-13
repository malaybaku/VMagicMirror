using System;
using System.Linq;
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
        private readonly GameInputSourceSet _sourceSet;
        private readonly ReactiveProperty<bool> _bodyMotionActive = new ReactiveProperty<bool>(false);
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        private bool _hasModel;
        private Animator _animator;

        private Vector2 _moveInput;
        private bool _isCrouching;
        
        public GamepadInputBodyMotionController(
            IVRMLoadable vrmLoadable,
            GameInputSourceSet sourceSet
            )
        {
            _vrmLoadable = vrmLoadable;
            _sourceSet = sourceSet;
        }

        void IInitializable.Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;

            Observable.Merge(_sourceSet.Sources.Select(s => s.Jump))
                .Subscribe(_ => TryJump())
                .AddTo(_disposable);

            //NOTE: 1フレームで2デバイスから入力が来たら片方は無視したいが…
            Observable.Merge(
                _sourceSet.Sources
                    .Select(s => s.MoveInput.DistinctUntilChanged()
                    )
                )
                .Subscribe(v => _moveInput = v)
                .AddTo(_disposable);

            Observable.Merge(
                _sourceSet.Sources
                    .Select(s => s.IsCrouching.DistinctUntilChanged()
                    )
                )
                .Subscribe(v => _isCrouching = v)
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
            if (_hasModel && _bodyMotionActive.Value)
            {
                _animator.SetTrigger(Jump);
            }
        }

        //NOTE: イベント入力との順序とか踏まえて必要な場合、LateTickにしてもいい
        void ITickable.Tick()
        {
            if (!_hasModel)
            {
                return;
            }

            _animator.SetBool(Active, _bodyMotionActive.Value);

            //NOTE: 使わない場合も一応入れておく、というスタイル
            _animator.SetFloat(MoveRight, _moveInput.x);
            _animator.SetFloat(MoveForward, _moveInput.y);
            _animator.SetBool(Crouch, _isCrouching);
        }

        void IDisposable.Dispose() => _disposable.Dispose();
    }
}
