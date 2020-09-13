using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UniRx;
using VRM;
using UniHumanoid;
using Zenject;

namespace Baku.VMagicMirror
{
    //NOTE: このクラスが(半分神になっちゃうのが気に入らんが)やること
    // - プレビューのon/off : プレビューがオンの場合、プレビューが全てに優先する
    // - プレビューではないワードベースモーションのon/off :
    //   - プレビューがオフでワードベースモーションが有効な間は
    //     コレを使ってモデルの体や表情を操る

    //NOTE2: Bvhによる動作については「ほんとにBvhでいいのか」問題が浮上しているため、いったんGUI側で選択不可にしている。そのため実装が凄くいい加減。

    /// <summary>
    /// <see cref="WordToMotionController"/>と同じ目的でWord To Motionを動かすが、
    /// そのとき手段として他の制御(Hand+Head IK, Body, BlendShape)のオンオフの権限を持つ。
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
                if (value)
                {
                    PreviewRequest = null;
                }
                else
                {
                    IsPlayingBlendShape = false;
                    _blendShape.ResetBlendShape();
                    _blendShape.KeepLipSync = false;
                    StopPreviewBuiltInMotion();
                }
            }
        }

        /// <summary>モーションを実行中かどうかを取得します。</summary>
        public bool IsPlayingMotion { get; private set; }

        /// <summary>表情を切り替え中かどうかを取得します。</summary>
        public bool IsPlayingBlendShape { get; private set; }

        /// <summary>プレビュー動作の内容。</summary>
        public MotionRequest PreviewRequest { get; set; }

        private LateMotionTransfer _motionTransfer = null;

        private WordToMotionMapper _mapper = null;
        private IkWeightCrossFade _ikWeightCrossFade = null;
        private WordToMotionBlendShape _blendShape = null;

        private SimpleAnimation _simpleAnimation = null;

        private MotionRequest _currentMotionRequest = null;
        
        //いまの動作の種類: MotionRequest.MotionTypeXXXのどれかの値になる
        private int _currentMotionType = MotionRequest.MotionTypeNone;
        //ビルトインモーションの実行中だと意味のある文字列になる
        private string _currentBuiltInMotionName = "";

        private float _ikFadeInCountDown = 0f;
        private float _blendShapeResetCountDown = 0f;
        private float _bvhStopCountDown = 0f;

        [Inject]
        public void Initialize(IMessageReceiver receiver, IVRMLoadable vrmLoadable, BuiltInMotionClipData builtInClips)
        {
            var _ = new WordToMotionManagerReceiver(receiver, this);
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
            _mapper = new WordToMotionMapper(builtInClips);
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

            //モーションを適用
            switch (request.MotionType)
            {
                case MotionRequest.MotionTypeBuiltInClip:
                    StopCurrentMotion();
                    StartBuiltInMotion(request.BuiltInAnimationClipName);
                    break;
                case MotionRequest.MotionTypeBvhFile:
                    StopCurrentMotion();
                    StartBvhFileMotion(request.ExternalBvhFilePath);
                    break;
                case MotionRequest.MotionTypeNone:
                default:
                    break;
            }

            //表情を適用
            if (request.UseBlendShape)
            {
                IsPlayingBlendShape = true;
                StartApplyBlendShape(request);
            }
        }
        
        private void Start()
        {
            _blendShape = GetComponent<WordToMotionBlendShape>();
            _motionTransfer = GetComponent<LateMotionTransfer>();
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
                    if (ShouldSetDefaultClipAfterMotion)
                    {
                        Debug.Log("End animation, return to default");
                       _simpleAnimation.CrossFade("Default", ikFadeDuration);
                    }
                }
            }

            if (EnablePreview && PreviewRequest != null)
            {
                if (PreviewRequest.MotionType == MotionRequest.MotionTypeBuiltInClip && 
                    !string.IsNullOrEmpty(PreviewRequest.BuiltInAnimationClipName))
                {
                    StartPreviewBuiltInMotion(PreviewRequest.BuiltInAnimationClipName);
                }
                else
                {
                    StopPreviewBuiltInMotion();
                }
            }

            //note: BVHは一旦ないことにしてるのでここは来ません
            if (_bvhStopCountDown > 0)
            {
                _bvhStopCountDown -= Time.deltaTime;
                if (_bvhStopCountDown <= 0)
                {
                    if (_motionTransfer.Target != null)
                    {
                        _motionTransfer.Target.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.None;
                        _motionTransfer.Fade(false);
                    }

                    if (_motionTransfer.Source != null)
                    {

                        Destroy(_motionTransfer.Source.gameObject);
                        _motionTransfer.Source = null;
                    }
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
                    _blendShape.KeepLipSync = false;
                    _blendShape.ResetBlendShape();
                }
            }

            if (EnablePreview && PreviewRequest != null)
            {
                ApplyPreviewBlendShape();
            }
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _simpleAnimation = info.vrmRoot.gameObject.AddComponent<SimpleAnimation>();
            _simpleAnimation.playAutomatically = false;
            _simpleAnimation.AddState(defaultAnimation, "Default");
            _simpleAnimation.Play("Default");

            _blendShape.Initialize(info.blendShape);
            _motionTransfer.Target = info.vrmRoot.GetComponent<HumanPoseTransfer>();
            
            _ikWeightCrossFade.OnVrmLoaded(info);
        }

        private void OnVrmDisposing()
        {
            _ikWeightCrossFade.OnVrmDisposing();

            _blendShape.DisposeProxy();
            _motionTransfer.Target = null;
            _simpleAnimation = null;
        }

        private void ApplyPreviewBlendShape()
        {
            if (PreviewRequest.UseBlendShape)
            {
                foreach (var pair in PreviewRequest.BlendShapeValuesDic)
                {
                    _blendShape.Add(BlendShapeKeyFactory.CreateFrom(pair.Key), pair.Value);
                }
                _blendShape.KeepLipSync = PreviewRequest.PreferLipSync;
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
        
        private void StartBvhFileMotion(string bvhFilePath)
        {
            if (_motionTransfer.Target == null)
            {
                return;
            }

            try
            {
                //contextのdisposeしないとダメなやつじゃないかなコレ
                var context = new BvhImporterContext();
                context.Parse(bvhFilePath);
                context.Load();
                if (_motionTransfer.Source  != null)
                {
                    Destroy(_motionTransfer.Source.gameObject);
                }
                _motionTransfer.Source = context.Root.GetComponent<HumanPoseTransfer>();
                //box-manというのが出てくるけど出したくないので隠します。
                _motionTransfer.Source.GetComponent<SkinnedMeshRenderer>().enabled = false;
                _motionTransfer.Target.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
                _motionTransfer.Fade(true);

                //いったんIKからアニメーションにブレンディングし、後で元に戻す
                _ikWeightCrossFade.FadeOutArmIkWeights(ikFadeDuration);
                fingerController.FadeOutWeight(0);

                float duration = context.Bvh.FrameCount * Time.deltaTime;
                Debug.Log("duration = " + duration.ToString("00.000"));
                _ikFadeInCountDown = duration - ikFadeDuration;
                _bvhStopCountDown = duration;
                //ここは短すぎるモーションを指定されたときの対策
                if (_ikFadeInCountDown <= 0)
                {
                    _ikFadeInCountDown = 0.01f;
                    _bvhStopCountDown = 0.01f;
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void StopCurrentMotion()
        {
            //プレビュー中は通常動作は開始も停止もしないので、何もしないでOK
            if (EnablePreview) { return; }

            switch (_currentMotionType)
            {
                case MotionRequest.MotionTypeBuiltInClip:
                    if (!string.IsNullOrEmpty(_currentBuiltInMotionName) && _simpleAnimation.IsPlaying(_currentBuiltInMotionName))
                    {
                        _simpleAnimation?.Stop(_currentBuiltInMotionName);
                    }
                    _currentBuiltInMotionName = "";
                    break;
                case MotionRequest.MotionTypeBvhFile:
                    if (_motionTransfer.Target != null)
                    {
                        _motionTransfer.Target.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.None;
                        _motionTransfer.Fade(false);
                    }

                    if (_motionTransfer.Source != null)
                    {

                        Destroy(_motionTransfer.Source.gameObject);
                        _motionTransfer.Source = null;
                    }
                    _bvhStopCountDown = 0f;
                    break;
                case MotionRequest.MotionTypeNone:
                default:
                    break;
            }

            _currentMotionType = MotionRequest.MotionTypeNone;
        }

        private void StartApplyBlendShape(MotionRequest request)
        {
            if (!request.UseBlendShape ||
                request.BlendShapeValuesDic == null ||
                request.BlendShapeValuesDic.Count == 0)
            {
                return;
            }

            //Clearが要るのは前回のブレンドシェイプと混ざるのを防ぐため
            _blendShape.Clear();
            foreach (var pair in request.BlendShapeValuesDic)
            {
                _blendShape.Add(BlendShapeKeyFactory.CreateFrom(pair.Key), pair.Value);
            }
            _blendShape.KeepLipSync = request.PreferLipSync;
            _blendShapeResetCountDown = CalculateDuration(request);
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
                case MotionRequest.MotionTypeBvhFile:
                    //TODO: Bvhファイルベースで計算する
                    return 5.0f;
                case MotionRequest.MotionTypeNone:
                    return request.DurationWhenOnlyBlendShape;
                default:
                    //来ないハズ
                    return 5.0f;
            }
        }
    }
}
