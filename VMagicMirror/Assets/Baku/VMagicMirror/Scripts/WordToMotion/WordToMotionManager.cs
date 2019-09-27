using System;
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
//    [RequireComponent(typeof(LateMotionTransfer))]



    /// <summary>
    /// <see cref="WordToMotionController"/>と同じ目的でWord To Motionを動かすが、
    /// そのとき手段として他の制御(Hand+Head IK, Body, BlendShape)のオンオフの権限を持つ。
    /// </summary>
    public class WordToMotionManager : MonoBehaviour
    {

        [SerializeField]
        [Tooltip("この時間だけキー入力が無かったらワードが途切れたものとして入力履歴をクリアする。")]
        private float forgetTime = 1.0f;

        [SerializeField]
        [Tooltip("ワード由来のモーションに入る時にIKを無効化するときの所要時間")]
        private float ikFadeDuration = 0.5f;

        //棒立ちポーズが指定されたアニメーション。
        //コレが必要なのは、デフォルトアニメーションが無いと下半身を動かさないアニメーションで脚が骨折するため
        [SerializeField] private AnimationClip defaultAnimation = null;

        [Inject] private IVRMLoadable _vrmLoadable = null;

        /// <summary>
        /// モーション実行後、単にIKを切るのではなくデフォルト(=立ち)状態に戻すべきかどうか判断するフラグを指定します。
        /// note: これは実際には、タイピング動作が無効化されているときに使いたい
        /// </summary>
        public bool ShouldSetDefaultClipAfterMotion { get; set; } = false;

        /// <summary>キー押下イベントをちゃんと読み込むか否か</summary>
        public bool EnableReadKey { get; set; } = true;

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

        private readonly WordAnalyzer _analyzer = new WordAnalyzer();
        private float _count = 0f;
        private float _ikFadeInCountDown = 0f;
        private float _blendShapeResetCountDown = 0f;
        private float _bvhStopCountDown = 0f;
        
        public void LoadItems(MotionRequestCollection motionRequests)
        {
            _mapper.Requests = motionRequests.Requests;
            _analyzer.LoadWordSet(
                motionRequests.Requests.Select(r => r.Word).ToArray()
                );
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

        /// <summary>
        /// キー押下の処理: 実際はパーサー的なクラスに素通しするだけ
        /// </summary>
        /// <param name="keyName"></param>
        public void ReceiveKeyDown(string keyName)
        {
            if (!EnableReadKey)
            {
                return;
            }

            _count = forgetTime;
            _analyzer.Add(KeyName2Char(keyName));
        }


        private void Start()
        {
            _count = forgetTime;

            _mapper = GetComponent<WordToMotionMapper>();
            _blendShape = GetComponent<WordToMotionBlendShape>();
            _motionTransfer = GetComponent<LateMotionTransfer>();
            _ikWeightCrossFade = GetComponent<IkWeightCrossFade>();
            
            _analyzer.WordDetected.Subscribe(word =>
            {
                if (_simpleAnimation == null)
                {
                    return;
                }

                var request = _mapper.FindMotionRequest(word);
                if (request != null)
                {
                    PlayItem(request);
                }
            });

            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private void Update()
        {
            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _count = forgetTime;
                _analyzer.Clear();
            }

            if (!EnablePreview && _ikFadeInCountDown > 0)
            {
                _ikFadeInCountDown -= Time.deltaTime;
                if (_ikFadeInCountDown <= 0)
                {
                    //フェードさせ終わる前に完了扱いにする: やや荒っぽいが、高精度に使うフラグではないのでOK
                    IsPlayingMotion = false;
                    _ikWeightCrossFade.FadeInArmIkWeights(ikFadeDuration);
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
        }

        
        private void ApplyPreviewBlendShape()
        {
            if (PreviewRequest.UseBlendShape)
            {
                foreach (var pair in PreviewRequest.BlendShapeValuesDic)
                {
                    _blendShape.Add(new BlendShapeKey(pair.Key), pair.Value);
                }
            }
            else
            {
                _blendShape.ResetBlendShape();
            }
        }

        private void StartBuiltInMotion(string clipName)
        {
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
                _blendShape.Add(new BlendShapeKey(pair.Key), pair.Value);
            }
            _blendShapeResetCountDown = CalculateDuration(request);
        }


        private static char KeyName2Char(string keyName)
        {
            if (keyName.Length == 1)
            {
                //a-z
                return keyName.ToLower()[0];
            }
            else if (keyName.Length == 2 && keyName[0] == 'D' && char.IsDigit(keyName[1]))
            {
                //D0 ~ D9 (テンキーじゃないほうの0~9)
                return keyName[1];
            }
            else if (keyName.Length == 7 && keyName.StartsWith("NumPad") && char.IsDigit(keyName[6]))
            {
                //NumPad0 ~ NumPad9 (テンキーの0~9)
                return keyName[6];
            }
            else
            {
                //TEMP: 「ヘンな文字でワードが途切れた」という情報だけ残す
                return ' ';
            }
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
