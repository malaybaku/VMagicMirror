using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 首の動きだけからなるようなモーションの再生機構。
    /// 全身用のアニメーションクリップを使わず、頭の角度リクエストのみを送出する
    /// </summary>
    public class HeadMotionClipPlayer : MonoBehaviour, IWordToMotionPlayer
    {
        public const string NodClipName = "Nod";
        public const string ShakeClipName = "Shake";
        
        /// <summary> 再生中のモーションのパターン一覧 </summary>
        public enum ClipPlayState
        {
            None,
            Nod,
            Shake,
        }

        [SerializeField] private HeadMotionClipSetting setting;

        private VRMAutoBlink _autoBlink;

        //Playするたびに値が変わることに注意
        private HeadMotionClipSetting.HeadMotionParams _currentMotionParams;

        private ClipPlayState _playState = ClipPlayState.None;
        private string _previewClipName = "";
        private bool PreviewIsActive => !string.IsNullOrEmpty(_previewClipName);
        
        public bool IsPlaying => _playState != ClipPlayState.None && _playCount < _currentMotionParams.Duration;

        bool IWordToMotionPlayer.UseIkAndFingerFade => false;

        bool IWordToMotionPlayer.CanPlay(MotionRequest request)
        {
            return request.MotionType == MotionRequest.MotionTypeBuiltInClip && (
                request.BuiltInAnimationClipName == NodClipName ||
                request.BuiltInAnimationClipName == ShakeClipName
                );
        }

        void IWordToMotionPlayer.Play(MotionRequest request, out float duration)
        {
            Play(request.BuiltInAnimationClipName, out duration);
        }

        void IWordToMotionPlayer.Abort()
        {
            //停止指示に対しては「Preview動作があったら消す」とし、プレビューじゃない動作の場合は止められない
            if (PreviewIsActive)
            {
                _previewClipName = "";
                Stop();
            }
        }

        void IWordToMotionPlayer.PlayPreview(MotionRequest request)
        {
            PlayPreview(request.BuiltInAnimationClipName);
        }

        /// <summary> 頭に対して適用してほしい回転値を取得します。 </summary>
        public Quaternion RotationRequest { get; private set; }

        /// <summary> 頭に対して適用してほしい回転値の半分を取得します。Neckのあるモデルに対して使います。 </summary>
        public Quaternion HalfRotationRequest { get; private set; }

        private float _playCount = 0f;
        
        private bool _hasModel;
        private bool _hasNeck;
        private Transform _neck;
        private Transform _head;

        public bool CanPlay(string clipName)
        {
            return clipName == NodClipName || clipName == ShakeClipName;
        }
        
        //durationは通常は副作用の一貫として拾って欲しいのでoutで渡す
        public void Play(string clipName, out float duration)
        {
            if (clipName == NodClipName)
            {
                PlayNoddingMotion();
            }
            else if (clipName == ShakeClipName)
            {
                PlayShakingMotion();
            }
            duration = _currentMotionParams.Duration;
        }
        
        private void PlayNoddingMotion()
        {
            //既にモーション実行中だった場合は巻き戻して再生し直すが、連続実行するとこのせいでガクつくのが多少まずい
            _playCount = 0f;
            _currentMotionParams = setting.GetNoddingMotionParams();
            _playState = ClipPlayState.Nod;
        }

        private void PlayShakingMotion()
        {
            _playCount = 0f;
            _currentMotionParams = setting.GetShakingMotionParams();
            _playState = ClipPlayState.Shake;
        }

        /// <summary> モーションが再生中だった場合、それを直ちにストップします。 </summary>
        public void Stop()
        {
            _playState = ClipPlayState.None;
        }

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, VRMAutoBlink autoBlink)
        {
            _autoBlink = autoBlink;
            vrmLoadable.VrmLoaded += info =>
            {
                _neck = info.animator.GetBoneTransform(HumanBodyBones.Neck);
                _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
                _hasNeck = (_neck != null);
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hasNeck = false;
                _neck = null;
                _head = null;
            };
        }

        public void PlayPreview(string clipName)
        {
            if (!CanPlay(clipName))
            {
                return;
            }

            _previewClipName = clipName;
            if (_playState == ClipPlayState.None)
            {
                Play(clipName, out _);    
            }
        }

        public void StopPreview()
        {
            _previewClipName = "";
            Stop();
        }
        
        private void Update()
        {
            if (PreviewIsActive && _playCount >= _currentMotionParams.Duration)
            {
                Play(_previewClipName, out _);
                //プレビュー関数はプレビューが有効なあいだは毎フレーム呼ばれること、および
                //プレビューの停止タイミングが読めづらいのを踏まえて毎回初期化してしまう
                _previewClipName = "";
            }

            if (_playState == ClipPlayState.None || _playCount >= _currentMotionParams.Duration)
            {
                //ここは通常はガクつかない(カーブの設計がそういうふうになってるので)
                HalfRotationRequest = Quaternion.identity;
                RotationRequest = Quaternion.identity;
                return;
            }

            var count = _playCount + Time.deltaTime;
            if (_currentMotionParams.ShouldBlink && 
                _playCount < _currentMotionParams.BlinkTime &&
                count >= _currentMotionParams.BlinkTime)
            {
                _autoBlink.ForceStartBlink();
            }
            
            _playCount = count;
            HalfRotationRequest = GetHalfRotationByCurve(_playCount / _currentMotionParams.Duration);
            RotationRequest = HalfRotationRequest * HalfRotationRequest;
        }

        private void LateUpdate()
        {
            if (!_hasModel)
            {
                return;
            }

            if (_hasNeck)
            {
                _neck.localRotation *= HalfRotationRequest;
                _head.localRotation *= HalfRotationRequest;
            }
            else
            {
                _head.localRotation *= RotationRequest;
            }
        }

        private Quaternion GetHalfRotationByCurve(float rate)
        {
            float factor = _currentMotionParams.AngleFactor * 0.5f;
            switch (_playState)
            {
                case ClipPlayState.None:
                    return Quaternion.identity;
                case ClipPlayState.Nod:
                    return Quaternion.AngleAxis(factor * _currentMotionParams.Evaluate(rate), Vector3.right);
                case ClipPlayState.Shake:
                    return Quaternion.AngleAxis(factor * _currentMotionParams.Evaluate(rate), Vector3.up);
                default:
                    return Quaternion.identity;
            }
        }
    }
}

