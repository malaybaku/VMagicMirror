using System;
using System.Linq;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    //TODO: GODじゃなくなってほしすぎる
    //NOTE: このクラスが(半分神になっちゃうのが気に入らんが)やること
    // - プレビューのon/off : プレビューがオンの場合、プレビューが全てに優先する
    // - プレビューではないワードベースモーションのon/off :
    //   - プレビューがオフでワードベースモーションが有効な間は
    //     コレを使ってモデルの体や表情を操る
    
    /// <summary>
    /// Word to Motion機能によってモーションとか表情にアクセスするやつ
    /// </summary>
    public class WordToMotionManager : MonoBehaviour
    {
        [Tooltip("Word To Motionの発動イベントを管理しているトリガークラス")]
        [SerializeField] private WordToMotionTriggers triggers = null;

        [Tooltip("ワード由来のモーションに入る時にIKを無効化するときの所要時間")]
        [SerializeField] private float ikFadeDuration = 0.5f;
        //棒立ちポーズが指定されたアニメーション。
        //コレが必要なのは、デフォルトアニメーションが無いと下半身を動かさないアニメーションで脚が骨折するため
        [SerializeField] private AnimationClip defaultAnimation = null;
        [SerializeField] private FingerController fingerController = null;
        [SerializeField] private CustomMotionPlayer customMotionPlayer = null;
        
        /// <summary>
        /// モーション実行後、単にIKを切るのではなくデフォルト(=立ち)状態に戻すべきかどうか判断するフラグを指定します。
        /// note: これは実際には、タイピング動作が無効化されているときに使いたい
        /// </summary>
        public bool ShouldSetDefaultClipAfterMotion { get; set; } = false;

        /// <summary> ゲームパッド入力をWord to Motionに用いるかどうかを取得、設定します。 </summary>
        public bool UseGamepadForWordToMotion
        {
            get => triggers.UseGamepadInput;
            set => triggers.UseGamepadInput = value;
        }

        /// <summary> キーボード入力をWord to Motionに用いるかどうかを取得、設定します。 </summary>
        public bool UseKeyboardForWordToMotion
        {
            get => triggers.UseKeyboardInput;
            set => triggers.UseKeyboardInput = value;
        }

        /// <summary> MIDI入力をWord to Motionに用いるかどうかを取得、設定します。 </summary>
        public bool UseMidiForWordToMotion
        {
            get => triggers.UseMidiInput;
            set => triggers.UseMidiInput = value;
        }

        /// <summary> 単語ベースの入力をWord to Motionに用いるかどうかを取得、設定します。 </summary>
        public bool UseKeyboardWordTypingForWordToMotion
        {
            get => triggers.UseKeyboardWordTypingForWordToMotion;
            set => triggers.UseKeyboardWordTypingForWordToMotion = value;
        } 

        private bool _enablePreview = false;
        /// <summary>
        /// プレビュー動作を有効化するか否か。
        /// falseからtrueにトグルした時点で<see cref="PreviewRequest"/>がnullに初期化されます。
        /// </summary>
        public bool EnablePreview
        {
            get => _enablePreview; 
            set
            {
                if (_enablePreview == value)
                {
                    return;
                }

                _enablePreview = value;
                _blendShape.IsPreview = value;
                if (value)
                {
                    PreviewRequest = null;
                }
                else
                {
                    IsPlayingBlendShape = false;
                    _blendShape.ResetBlendShape();
                    StopPreviewBuiltInMotion();
                    StopPreviewCustomMotion();
                    _accessoryVisibilityRequest.Value = "";
                }
            }
        }

        /// <summary>モーションを実行中かどうかを取得します。</summary>
        public bool IsPlayingMotion { get; private set; }

        /// <summary>表情を切り替え中かどうかを取得します。</summary>
        public bool IsPlayingBlendShape { get; private set; }

        /// <summary>プレビュー動作の内容。</summary>
        public MotionRequest PreviewRequest { get; set; }
        
        private readonly ReactiveProperty<string> _accessoryVisibilityRequest 
            = new ReactiveProperty<string>("");
        /// <summary> 表示してほしいアクセサリーのFileIdか、または空文字 </summary>
        public IReadOnlyReactiveProperty<string> AccessoryVisibilityRequest => _accessoryVisibilityRequest;

        private HeadMotionClipPlayer _headMotionClipPlayer = null;
        private WordToMotionMapper _mapper = null;
        private IkWeightCrossFade _ikWeightCrossFade = null;
        private WordToMotionBlendShape _blendShape = null;

        private SimpleAnimation _simpleAnimation = null;

        private MotionRequest _currentMotionRequest = null;
        
        //いまの動作の種類: MotionRequest.MotionTypeXXXのどれかの値になる
        private int _currentMotionType = MotionRequest.MotionTypeNone;
        //ビルトインモーションの実行中だと意味のある文字列になる
        private string _currentBuiltInMotionName = "";

        private bool _currentMotionIsHeadMotion = false;
        private float _ikFadeInCountDown = 0f;
        private float _blendShapeResetCountDown = 0f;
        private float _customMotionStopCountDown = 0f;

        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable,
            BuiltInMotionClipData builtInClips,
            HeadMotionClipPlayer headMotionClipPlayer
            )
        {
            var _ = new WordToMotionManagerReceiver(receiver, this);
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
            _mapper = new WordToMotionMapper(builtInClips);
            _headMotionClipPlayer = headMotionClipPlayer;
        }

        public void LoadItems(MotionRequestCollection motionRequests)
        {
            _mapper.Requests = motionRequests.Requests;
            triggers.LoadItems(motionRequests);
        }

        /// <summary>
        /// 通常のワード検出をした場合の判断基準でモーションを再生します。
        /// </summary>
        /// <param name="request"></param>
        public void PlayItem(MotionRequest request)
        {
            if (EnablePreview) { return; }

            _currentMotionRequest = request;

            float dynamicDuration = 0f;

            //モーションを適用
            switch (request.MotionType)
            {
                case MotionRequest.MotionTypeBuiltInClip:
                    StopCurrentMotion();
                    _currentMotionIsHeadMotion = _headMotionClipPlayer.CanPlay(request.BuiltInAnimationClipName);
                    if (_currentMotionIsHeadMotion)
                    {
                        _headMotionClipPlayer.Play(request.BuiltInAnimationClipName, out float duration);
                        dynamicDuration = duration;
                        IsPlayingMotion = true;
                    }
                    else
                    {
                        StartBuiltInMotion(request.BuiltInAnimationClipName);
                    }
                    break;
                case MotionRequest.MotionTypeCustom:
                    StopCurrentMotion();
                    StartCustomMotion(request.CustomMotionClipName);
                    break;
                case MotionRequest.MotionTypeNone:
                default:
                    break;
            }

            //表情を適用
            if (request.UseBlendShape)
            {
                IsPlayingBlendShape = true;
                if (dynamicDuration > 0f)
                {
                    StartApplyBlendShape(request, dynamicDuration);
                }
                else
                {
                    StartApplyBlendShape(request);
                }
            }
        }
        
        private void Start()
        {
            _blendShape = GetComponent<WordToMotionBlendShape>();
            _ikWeightCrossFade = GetComponent<IkWeightCrossFade>();
            
            triggers.RequestExecuteWord += word =>
            {
                var request = _mapper.FindMotionRequest(word);
                if (request != null)
                {
                    PlayItem(request);
                }
            };

            triggers.RequestExecuteWordToMotionItem += i =>
            {
                var request = _mapper.FindMotionByIndex(i);
                if (request != null)
                {
                    PlayItem(request);
                }
            };
        }

        private void Update()
        {
            if (!EnablePreview && _ikFadeInCountDown > 0)
            {
                _ikFadeInCountDown -= Time.deltaTime;
                if (_ikFadeInCountDown <= 0)
                {
                    //フェードさせ終わる前に完了扱いにする: やや荒っぽいが、高精度に使うフラグではないのでOK
                    IsPlayingMotion = false;
                    _ikWeightCrossFade.FadeInArmIkWeights(ikFadeDuration);
                    fingerController.FadeInWeight(ikFadeDuration);
                    if (_currentMotionType == MotionRequest.MotionTypeCustom)
                    {
                        //NOTE: モデルとCustomMotionPlayerの両方がデフォルトのポーズに向かうことで、
                        //アニメーション終了時の破綻を防ぐのが狙い
                        _simpleAnimation.CrossFade("Default", ikFadeDuration);
                        customMotionPlayer.FadeToDefaultPose(ikFadeDuration);
                    }
                    else if (ShouldSetDefaultClipAfterMotion)
                    {
                        Debug.Log("End animation, return to default");
                       _simpleAnimation.CrossFade("Default", ikFadeDuration);
                    }
                }
            }
            
            if (!EnablePreview && _currentMotionIsHeadMotion && !_headMotionClipPlayer.IsPlaying)
            {
                //頭部のみのモーションはIKウェイトとかはいじらないため、フラグだけ折れば十分
                _currentMotionIsHeadMotion = false;
                IsPlayingMotion = false;
            }

            if (EnablePreview && PreviewRequest != null)
            {
                if (PreviewRequest.MotionType == MotionRequest.MotionTypeBuiltInClip && 
                    !string.IsNullOrEmpty(PreviewRequest.BuiltInAnimationClipName))
                {
                    if (_headMotionClipPlayer.CanPlay(PreviewRequest.BuiltInAnimationClipName))
                    {
                        _headMotionClipPlayer.PlayPreview(PreviewRequest.BuiltInAnimationClipName);
                    }
                    else
                    {
                        StartPreviewBuiltInMotion(PreviewRequest.BuiltInAnimationClipName);
                    }
                }
                else
                {
                    StopPreviewBuiltInMotion();
                    _headMotionClipPlayer.StopPreview();
                }

                if (PreviewRequest.MotionType == MotionRequest.MotionTypeCustom &&
                    !string.IsNullOrEmpty(PreviewRequest.CustomMotionClipName))
                {
                    StartPreviewCustomMotion(PreviewRequest.CustomMotionClipName);
                }
                else
                {
                    StopPreviewCustomMotion();
                }
            }

            if (!EnablePreview && _customMotionStopCountDown > 0)
            {
                //NOTE: _ikFadeDurationによる終了よりもちょっと遅れて止まる。はず。
                _customMotionStopCountDown -= Time.deltaTime;
                if (_customMotionStopCountDown <= 0)
                {
                    customMotionPlayer.StopCurrentMotion();
                }
            }

            if (!EnablePreview && _blendShapeResetCountDown > 0)
            {
                _blendShapeResetCountDown -= Time.deltaTime;
                //HoldBlendShapeフラグが立ってる場合、ここでブレンドシェイプを変えっぱなしにすることに注意
                if (_blendShapeResetCountDown <= 0 &&
                    _currentMotionRequest != null && 
                    !_currentMotionRequest.HoldBlendShape)
                {
                    IsPlayingBlendShape = false;
                    _blendShape.ResetBlendShape();
                }
            }

            if (EnablePreview && PreviewRequest != null)
            {
                _accessoryVisibilityRequest.Value = PreviewRequest.AccessoryName;
                ApplyPreviewBlendShape();
            }

            if (!EnablePreview)
            {
                _accessoryVisibilityRequest.Value = (IsPlayingMotion || IsPlayingBlendShape) && _currentMotionRequest != null 
                    ? _currentMotionRequest.AccessoryName
                    : "";
            }
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _simpleAnimation = info.vrmRoot.gameObject.AddComponent<SimpleAnimation>();
            _simpleAnimation.playAutomatically = false;
            _simpleAnimation.AddState(defaultAnimation, "Default");
            _simpleAnimation.Play("Default");

            _blendShape.Initialize(info.blendShape);
            _ikWeightCrossFade.OnVrmLoaded(info);
        }

        private void OnVrmDisposing()
        {
            _ikWeightCrossFade.OnVrmDisposing();

            _blendShape.DisposeProxy();
            _simpleAnimation = null;
        }

        private void ApplyPreviewBlendShape()
        {
            if (PreviewRequest.UseBlendShape)
            {
                _blendShape.SetForPreview(
                    PreviewRequest.BlendShapeValuesDic
                        .Select(pair => (BlendShapeKeyFactory.CreateFrom(pair.Key), pair.Value)
                        ),
                    PreviewRequest.PreferLipSync
                );
            }
            else
            {
                _blendShape.ResetBlendShape();
            }
        }

        private void StartBuiltInMotion(string clipName)
        {
            //キャラのロード前に数字キーとか叩いたケースをガードしています
            if (_simpleAnimation == null)
            {
                return;
            }
            
            var clip = _mapper.FindBuiltInAnimationClipOrDefault(clipName);
            if (clip == null) { return; }

            //NOTE: Removeがうまく動いてないように見えるのでRemoveしない設計になってます
            if (_simpleAnimation.GetState(clipName) == null)
            {
                _simpleAnimation.AddState(clip, clipName);
            }
            else
            {
                //2回目がきちんと動くために。
                _simpleAnimation.Rewind(clipName);
            }

            IsPlayingMotion = true;

            if (ShouldSetDefaultClipAfterMotion)
            {
                _simpleAnimation.CrossFade(clipName, ikFadeDuration);
            }
            else
            {
                _simpleAnimation.Play(clipName);
            }
            _currentBuiltInMotionName = clipName;
            _currentMotionType = MotionRequest.MotionTypeBuiltInClip;

            //いったんIKからアニメーションにブレンディングし、後で元に戻す
            _ikWeightCrossFade.FadeOutArmIkWeights(ikFadeDuration);
            fingerController.FadeOutWeight(ikFadeDuration);
            _ikFadeInCountDown = clip.length - ikFadeDuration;
            //ここは短すぎるモーションを指定されたときの対策
            if (_ikFadeInCountDown <= 0)
            {
                _ikFadeInCountDown = 0.01f;
            }
        }

        //note: このメソッドは実行中に何度呼び出してもOKな設計です
        private void StartPreviewBuiltInMotion(string clipName)
        {
            //もうやってる場合: そのまま放置
            if (IsPlayingMotion && _currentBuiltInMotionName == clipName)
            {
                return;
            }

            //プレビュー動作Aからプレビュー動作Bに変える、みたいな処理をやってるときの対応
            if (!string.IsNullOrEmpty(_currentBuiltInMotionName) &&
                _currentBuiltInMotionName != clipName
                )
            {
                _simpleAnimation.Stop(_currentBuiltInMotionName);
            }

            var clip = _mapper.FindBuiltInAnimationClipOrDefault(clipName);
            if (clip == null) { return; }

            if (_simpleAnimation.GetState(clipName) == null)
            {
                _simpleAnimation.AddState(clip, clipName);
            }
            else
            {
                //いちおう直す方が心臓に優しいので
                _simpleAnimation.Rewind(clipName);
            }

            IsPlayingMotion = true;
            _simpleAnimation.Play(clipName);
            _currentBuiltInMotionName = clipName;
            //プレビュー用なので一気にやる: コレでいいかはちょっと検討すべき
            _ikWeightCrossFade.FadeOutArmIkWeightsImmediately();
            fingerController.FadeOutWeight(0);
        }

        private void StopPreviewBuiltInMotion()
        {
            if (!IsPlayingMotion || string.IsNullOrEmpty(_currentBuiltInMotionName))
            {
                return;
            }

            IsPlayingMotion = false;
            _simpleAnimation.Stop(_currentBuiltInMotionName);
            _currentBuiltInMotionName = "";
            //プレビュー用なので一気にやる: コレでいいかはちょっと検討すべき
            _ikWeightCrossFade.FadeInArmIkWeightsImmediately();
            fingerController.FadeInWeight(0);
        }
        
        private void StartCustomMotion(string clipName)
        {
            var started = customMotionPlayer.PlayClip(clipName);
            if (!started)
            {
                LogOutput.Instance.Write("モーションが正常にスタートしませんでした: " + clipName);
            }
            _currentMotionType = MotionRequest.MotionTypeCustom;

            try
            {
                //いったんIKからアニメーションにブレンディングし、後で元に戻す
                _ikWeightCrossFade.FadeOutArmIkWeights(ikFadeDuration);
                fingerController.FadeOutWeight(0);

                float duration = customMotionPlayer.GetMotionDuration(clipName);
                _ikFadeInCountDown = duration - ikFadeDuration;
                _customMotionStopCountDown = duration;
                //ここは短すぎるモーションを指定されたときの対策
                if (_ikFadeInCountDown <= 0)
                {
                    _ikFadeInCountDown = 0.01f;
                    _customMotionStopCountDown = 0.01f;
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void StartPreviewCustomMotion(string clipName)
        {
            var started = customMotionPlayer.PlayClipForPreview(clipName);
            //もうやってた場合: そのまま放置
            if (!started)
            {
                return;
            }

            IsPlayingMotion = true;
            //プレビュー用なので一気にやる: コレでいいかはちょっと検討すべき
            _ikWeightCrossFade.FadeOutArmIkWeightsImmediately();
            fingerController.FadeOutWeight(0);
        }

        private void StopPreviewCustomMotion()
        {
            if (!IsPlayingMotion)
            {
                return;
            }

            IsPlayingMotion = false;
            customMotionPlayer.StopPreviewMotion();
            //プレビュー用なので一気にやる: コレでいいかはちょっと検討すべき
            _ikWeightCrossFade.FadeInArmIkWeightsImmediately();
            fingerController.FadeInWeight(0);            
        }

        private void StopCurrentMotion()
        {
            //プレビュー中は通常動作は開始も停止もしないので、何もしないでOK
            if (EnablePreview) { return; }

            _headMotionClipPlayer.Stop();
            _currentMotionIsHeadMotion = false;
            switch (_currentMotionType)
            {
                case MotionRequest.MotionTypeBuiltInClip:
                    if (!string.IsNullOrEmpty(_currentBuiltInMotionName) && _simpleAnimation.IsPlaying(_currentBuiltInMotionName))
                    {
                        _simpleAnimation?.Stop(_currentBuiltInMotionName);
                    }
                    _currentBuiltInMotionName = "";
                    break;
                case MotionRequest.MotionTypeCustom:
                    customMotionPlayer.StopCurrentMotion();
                    _customMotionStopCountDown = 0f;
                    break;
                case MotionRequest.MotionTypeNone:
                default:
                    break;
            }

            _currentMotionType = MotionRequest.MotionTypeNone;
        }

        private void StartApplyBlendShape(MotionRequest request, float dynamicDuration = -1f)
        {
            if (!request.UseBlendShape ||
                request.BlendShapeValuesDic == null ||
                request.BlendShapeValuesDic.Count == 0)
            {
                return;
            }

            _blendShape.SetBlendShapes(
                request.BlendShapeValuesDic.Select(
                    pair => (BlendShapeKeyFactory.CreateFrom(pair.Key), pair.Value)
                ),
                request.PreferLipSync
            );
            
            _blendShapeResetCountDown = dynamicDuration > 0f
                ? dynamicDuration
                : CalculateDuration(request);
        }

        private float CalculateDuration(MotionRequest request)
        {
            switch (request.MotionType)
            {
                case MotionRequest.MotionTypeBuiltInClip:
                    try
                    {
                        return _mapper
                            .FindBuiltInAnimationClipOrDefault(request.BuiltInAnimationClipName)
                            .length;
                    }
                    catch
                    {
                        return 5.0f;
                    }
                case MotionRequest.MotionTypeCustom:
                    return customMotionPlayer.GetMotionDuration(request.CustomMotionClipName);
                case MotionRequest.MotionTypeNone:
                    return request.DurationWhenOnlyBlendShape;
                default:
                    //来ないハズ
                    return 5.0f;
            }
        }

        public void RunCustomMotionDoctor()
        {
            //とりあえず今は診断事項がないのでOK扱いで返します。
            return;
        }

        public string[] LoadAvailableCustomMotionClipNames() 
            => customMotionPlayer.LoadAvailableCustomMotionNames();
    }
}
