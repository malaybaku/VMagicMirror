using Baku.VMagicMirror.GameInput;
using Baku.VMagicMirror.MotionExporter;
using Baku.VMagicMirror.WordToMotion;
using R3;
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
        private BodyMotionModeController _bodyMotionModeController;

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
            var posBefore = Vector3.zero;
            var rotBefore = Quaternion.identity;
            if (_hasModel)
            {
                posBefore = _hips.localPosition;
                rotBefore = _hips.localRotation;
            }
            
            _lateUpdateRun.OnNext(Unit.Default);
            if (_playRoutine?.HasUpdate == true)
            {
                if (_bodyMotionModeController.MotionMode.Value == BodyMotionMode.GameInputLocomotion)
                {
                    //ゲーム入力中はAnimatorで有効な姿勢が入ってるはずなのを信じて、HumanPoseHandlerが値を更新した分は巻き戻す
                    _hips.localPosition = posBefore;
                    _hips.localRotation = rotBefore;
                }
                else
                {
                    //NOTE: こっちでもゲーム入力時と同じ方式で処理しうるかもしれないが、誤差が蓄積されるのが怖いので固定値で。
                    _hips.localPosition = _originHipsPos;
                    _hips.localRotation = _originHipsRot;
                }
            }
            _playRoutine?.ResetUpdateFlag();
        }

        private void OnDestroy()
        {
            _playRoutine?.Dispose();
            _playRoutine = null;
        }

        [Inject]
        public void Initialize(
            CustomMotionRepository repository, 
            BodyMotionModeController bodyMotionModeController,
            IVRMLoadable vrmLoadable)
        {
            _repository = repository;
            _bodyMotionModeController = bodyMotionModeController;

            vrmLoadable.VrmLoaded += info =>
            {
                //TODO: これControlRigと相性が悪すぎるので何か考えて下さい
                //ビルトインモーションもヤバそう
                _humanPoseHandler = new HumanPoseHandler(info.animator.avatar, info.vrmRoot);
                _hips = info.controlRig.GetBoneTransform(HumanBodyBones.Hips);
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
