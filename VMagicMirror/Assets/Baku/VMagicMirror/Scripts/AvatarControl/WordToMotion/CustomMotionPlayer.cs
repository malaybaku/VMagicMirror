using Baku.VMagicMirror.MotionExporter;
using Baku.VMagicMirror.WordToMotion;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// カスタムモーションをいい感じに実行するクラス。タスク指向で実装されている
    /// </summary>
    public class CustomMotionPlayer : MonoBehaviour, IWordToMotionPlayer
    {
        [SerializeField] private HumanoidAnimationSetter sourceFront = null;
        [SerializeField] private HumanoidAnimationSetter sourceBack = null;

        private readonly Subject<Unit> _lateUpdateRun = new Subject<Unit>();
        private CustomMotionRepository _repository;

        private bool _hasModel = false;
        private HumanPoseHandler _humanPoseHandler = null;

        private CustomMotionPlayRoutine _playRoutine = null;
        
        //アニメーション中の位置をどうにかせんといけないので…
        private Transform _hips;
        private Vector3 _originHipsPos;
        private Quaternion _originHipsRot;

        private CustomMotionItem CurrentItem => _playRoutine?.CurrentItem;
        private string CurrentMotionName => CurrentItem?.MotionLowerName ?? "";
        private bool IsPlayingPreview => _playRoutine?.IsRunningLoopMotion == true;
        
        //NOTE: Execution Order Sensitiveな処理なのでUniTask.DelayFrameが使えないんですね～
        private void LateUpdate()
        {
            _lateUpdateRun.OnNext(Unit.Default);
            if (_playRoutine?.HasUpdate == true)
            {
                //NOTE: hipsは固定しないとどんどんズレる事があるのを確認したため、安全のために固定してます
                _hips.localPosition = _originHipsPos;
                _hips.localRotation = _originHipsRot;
            }
            _playRoutine?.ResetUpdateFlag();
        }

        private void OnDestroy()
        {
            _playRoutine?.Dispose();
            _playRoutine = null;
        }

        [Inject]
        public void Initialize(CustomMotionRepository repository, IVRMLoadable vrmLoadable)
        {
            _repository = repository;

            vrmLoadable.VrmLoaded += info =>
            {
                _humanPoseHandler = new HumanPoseHandler(info.animator.avatar, info.vrmRoot);
                _hips = info.animator.GetBoneTransform(HumanBodyBones.Hips);
                _originHipsPos = _hips.localPosition;
                _originHipsRot = _hips.localRotation;
                _playRoutine = new CustomMotionPlayRoutine(
                    _humanPoseHandler, sourceFront, sourceBack, _lateUpdateRun
                );

                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;

                _playRoutine.Dispose();
                _playRoutine = null;
                _humanPoseHandler?.Dispose();
                _humanPoseHandler = null;

                _hips = null;
            };
        }

        bool IWordToMotionPlayer.CanPlay(MotionRequest request)
        {
            return
                request.MotionType == MotionRequest.MotionTypeCustom &&
                _repository.ContainsKey(request.CustomMotionClipName);
        }

        void IWordToMotionPlayer.Play(MotionRequest request, out float duration)
        {
            if (!_hasModel || IsPlayingPreview)
            {
                //いちおう非ゼロのdurationを返しておく
                duration = 1.0f;
                return;
            }

            var item = _repository.GetItem(request.CustomMotionClipName);
            duration = item.Motion.Duration - CustomMotionPlayState.FadeDuration;
            _playRoutine.Run(item);
        }

        void IWordToMotionPlayer.PlayPreview(MotionRequest request)
        {
            var motionName = request.CustomMotionClipName.ToLower();
            if (!_hasModel || (IsPlayingPreview && CurrentMotionName == motionName))
            {
                return;
            }

            var item = _repository.GetItem(motionName);
            _playRoutine.RunLoop(item);
        }

        void IWordToMotionPlayer.Stop() => _playRoutine?.Stop();
        void IWordToMotionPlayer.StopPreview() => _playRoutine?.StopImmediate();
        bool IWordToMotionPlayer.UseIkAndFingerFade => true;
    }
}
