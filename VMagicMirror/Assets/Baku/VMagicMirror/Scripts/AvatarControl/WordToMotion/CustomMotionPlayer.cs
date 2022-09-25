using System.Threading;
using Baku.VMagicMirror.MotionExporter;
using Baku.VMagicMirror.WordToMotion;
using Cysharp.Threading.Tasks;
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
        private const float FadeDuration = 0.5f;

        [SerializeField] private HumanoidAnimationSetter source = null;

        private CustomMotionRepository _repository;
        private readonly Subject<Unit> _lateUpdateRun = new Subject<Unit>();

        private bool _hasModel = false;
        private HumanPoseHandler _humanPoseHandler = null;
        private HumanPose _humanPose;

        //アニメーション中の位置をどうにかせんといけないので…
        private Transform _hips;
        private Vector3 _originHipsPos;
        private Quaternion _originHipsRot;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        private CustomMotionItem _currentItem = null;
        private string CurrentMotionName => _currentItem?.MotionLowerName ?? "";

        private bool _playingPreview = false;
        
        //NOTE: Execution Order Sensitiveな処理なのでUniTask.DelayFrameが使えないんですね～
        private void LateUpdate() => _lateUpdateRun.OnNext(Unit.Default);

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
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
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _humanPoseHandler = null;
            };
        }

        bool IWordToMotionPlayer.IsPlaying => !string.IsNullOrEmpty(CurrentMotionName);

        bool IWordToMotionPlayer.CanPlay(MotionRequest request)
        {
            return
                request.MotionType == MotionRequest.MotionTypeCustom &&
                _repository.ContainsKey(request.CustomMotionClipName);
        }

        void IWordToMotionPlayer.Play(MotionRequest request, out float duration)
        {
            if (!_hasModel || _playingPreview)
            {
                //いちおう非ゼロのdurationを返しておく
                duration = 1.0f;
                return;
            }

            var item = _repository.GetItem(request.CustomMotionClipName);
            duration = item.Motion.Duration;

            //NOTE: Stopせずにモーションどうしの補間するのを目指してもOK
            Stop();
            RunMotionAsync(item, _cts.Token).Forget();
        }

        void IWordToMotionPlayer.PlayPreview(MotionRequest request)
        {
            var motionName = request.CustomMotionClipName.ToLower();
            if (!_hasModel ||
                (_playingPreview && CurrentMotionName == motionName)
               )
            {
                return;
            }

            var item = _repository.GetItem(motionName);
            Stop();
            _playingPreview = true;
            RunMotionLoopAsync(item, _cts.Token).Forget();
        }

        void IWordToMotionPlayer.Abort() => Stop();

        void IWordToMotionPlayer.StopPreview() => Stop();

        bool IWordToMotionPlayer.UseIkAndFingerFade => true;

        private void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _playingPreview = false;
            _currentItem = null;
        }

        //NOTE: フェードインをしないのはIKFadeOutによっていい感じになる見込みだから
        async UniTaskVoid RunMotionAsync(CustomMotionItem item, CancellationToken cancellationToken)
        {
            PrepareItemRun(item);
            var count = 0f;

            var duration = _currentItem.Motion.Duration;
            while (count < duration)
            {
                await _lateUpdateRun.ToUniTask(true, cancellationToken);
                _currentItem.Motion.Evaluate(count);

                var useRate = (count < FadeDuration || count > duration - FadeDuration);
                //モーションの出入りは補間する: 急に動かないように
                var rate =
                    count < FadeDuration ? Mathf.Clamp01(count / FadeDuration) :
                    count > duration - FadeDuration ? Mathf.Clamp01((duration - count) / FadeDuration) :
                    1f;
                WriteCurrentPose(useRate, rate);
                count += Time.deltaTime;
            }

            cancellationToken.ThrowIfCancellationRequested();
            _currentItem = null;
        }

        async UniTaskVoid RunMotionLoopAsync(CustomMotionItem item, CancellationToken cancellationToken)
        {
            PrepareItemRun(item);
            while (!cancellationToken.IsCancellationRequested)
            {
                var count = 0f;
                while (count < _currentItem.Motion.Duration)
                {
                    await _lateUpdateRun.ToUniTask(true, cancellationToken);
                    _currentItem.Motion.Evaluate(count);
                    count += Time.deltaTime;
                    WriteCurrentPose();
                }
            }
        }

        void PrepareItemRun(CustomMotionItem item)
        {
            source.SetUsedFlags(item.UsedFlags);
            _currentItem = item;
            _currentItem.Motion.Target = source;
        }

        //クリップで再生されてセットされたはずの姿勢を当て込む
        void WriteCurrentPose(bool useRate = false, float rate = 1.0f)
        {
            _humanPoseHandler.GetHumanPose(ref _humanPose);
            if (useRate)
            {
                source.WriteToPose(ref _humanPose, rate);
            }
            else
            {
                source.WriteToPose(ref _humanPose);    
            }
 
            _humanPoseHandler.SetHumanPose(ref _humanPose);

            //NOTE: hipsは固定しないとどんどんズレる事があるのを確認したため、安全のために固定してます
            _hips.localPosition = _originHipsPos;
            _hips.localRotation = _originHipsRot;
        }
    }
}
