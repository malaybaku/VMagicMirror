using System;
using System.Linq;
using UnityEngine;
using UniRx;
using VRM;
using UniHumanoid;

namespace Baku.VMagicMirror
{
    //NOTE: このクラスが(半分神になっちゃうのが気に入らんが)やること
    // - プレビューのon/off : プレビューがオンの場合、プレビューが全てに優先する
    // - プレビューではないワードベースモーションのon/off :
    //   - プレビューがオフでワードベースモーションが有効な間は
    //     コレを使ってモデルの体や表情を操る
    public class WordToMotionController : MonoBehaviour
    {

        [SerializeField]
        [Tooltip("この時間だけキー入力が無かったらワードが途切れたものとして入力履歴をクリアする。")]
        private float _forgetTime = 1.0f;

        [SerializeField]
        [Tooltip("ワード由来のモーションに入る時にIKを無効化するときの所要時間")]
        private float _ikFadeDuration = 0.5f;

        // Start is called before the first frame update
        //立ち状態が入ってればOK。
        //コレが必要なのは、デフォルトアニメーションが無いと下半身を動かさないアニメーションで脚が骨折するため
        [SerializeField]
        private AnimationClip _defaultAnimation = null;

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
                    _blendShape.ResetBlendShape();
                }
            }
        }

        /// <summary>モーションを実行中かどうかを取得します。</summary>
        public bool IsPlayingMotion { get; private set; }

        /// <summary>表情を切り替え中かどうかを取得します。</summary>
        public bool IsPlayingBlendShape { get; private set; }

        /// <summary>プレビュー動作の内容。</summary>
        public MotionRequest PreviewRequest { get; set; }

        private WordToMotionMapper _mapper = null;
        private IkWeightCrossFade _ikWeightCrossFade = null;
        private WordToMotionBlendShape _blendShape = null;

        private SimpleAnimation _simpleAnimation = null;
        private HumanPoseTransfer _humanPoseTransferTarget = null;
        private Animator _animator = null;

        //いまの動作の種類: MotionRequest.MotionTypeXXXのどれかの値になる
        private int _currentMotionType = MotionRequest.MotionTypeNone;

        //ビルトインモーションの実行中だと意味のある文字列になる
        private string _currentBuiltInMotionName = "";
        //BVHモーションの実行中だと非nullになる
        private HumanPoseTransfer _humanPoseTransferSource = null;

        private readonly WordAnalyzer _analyzer = new WordAnalyzer();
        private float _count = 0f;
        private float _ikFadeInCountDown = 0f;
        private float _blendShapeResetCountDown = 0f;
        private float _bvhStopCountDown = 0f;


        public void Initialize(
            SimpleAnimation simpleAnimation,
            VRMBlendShapeProxy proxy, 
            HumanPoseTransfer humanPoseTransfer, 
            Animator animator
            )
        {
            _simpleAnimation = simpleAnimation;
            _simpleAnimation.AddState(_defaultAnimation, "Default");
            _blendShape.Initialize(proxy);
            _humanPoseTransferTarget = humanPoseTransfer;
            _animator = animator;
        }

        public void Dispose()
        {
            _simpleAnimation = null;
            _blendShape.DisposeProxy();
            _humanPoseTransferTarget = null;
            _animator = null;
        }

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
        /// キー押下イベントを処理する
        /// </summary>
        /// <param name="keyName"></param>
        public void ReceiveKeyDown(string keyName)
        {
            if (!EnableReadKey)
            {
                return;
            }

            _count = _forgetTime;
            _analyzer.Add(KeyName2Char(keyName));
        }


        private void Start()
        {
            _count = _forgetTime;
            _mapper = GetComponent<WordToMotionMapper>();
            _ikWeightCrossFade = GetComponent<IkWeightCrossFade>();
            _blendShape = GetComponent<WordToMotionBlendShape>();
            
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
        }

        private void Update()
        {
            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _count = _forgetTime;
                _analyzer.Clear();
            }

            if (_ikFadeInCountDown > 0)
            {
                _ikFadeInCountDown -= Time.deltaTime;
                if (_ikFadeInCountDown <= 0)
                {
                    //フェードさせ終わる前に完了扱いにする: やや荒っぽいが、高精度に使うフラグではないのでOK
                    IsPlayingMotion = false;
                    _ikWeightCrossFade.FadeInArmIkWeights(_ikFadeDuration);
                }
            }

            if (_bvhStopCountDown > 0)
            {
                _bvhStopCountDown -= Time.deltaTime;
                if (_bvhStopCountDown <= 0)
                {
                    if (_humanPoseTransferTarget != null)
                    {
                        _humanPoseTransferTarget.Source = null;
                        _humanPoseTransferTarget.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.None;
                    }

                    if (_humanPoseTransferSource != null)
                    {
                        Destroy(_humanPoseTransferSource.gameObject);
                        _humanPoseTransferSource = null;
                    }
                }
            }

            if (!EnablePreview && _blendShapeResetCountDown > 0)
            {
                _blendShapeResetCountDown -= Time.deltaTime;
                if (_blendShapeResetCountDown <= 0)
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
            _simpleAnimation.Play(clipName);
            _currentBuiltInMotionName = clipName;

            //いったんIKからアニメーションにブレンディングし、後で元に戻す
            _ikWeightCrossFade.FadeOutArmIkWeights(_ikFadeDuration);
            _ikFadeInCountDown = clip.length - _ikFadeDuration;
            //ここは短すぎるモーションを指定されたときの対策
            if (_ikFadeInCountDown <= 0)
            {
                _ikFadeInCountDown = 0.01f;
            }
        }

        private void StartBvhFileMotion(string bvhFilePath)
        {
            if (_humanPoseTransferTarget == null || _simpleAnimation == null || _animator == null)
            {
                return;
            }

            try
            {
                //contextのdisposeしないとダメなやつじゃないかなコレ
                var context = new BvhImporterContext();
                context.Parse(bvhFilePath);
                context.Load();
                if (_humanPoseTransferSource != null)
                {
                    Destroy(_humanPoseTransferSource.gameObject);
                }
                _humanPoseTransferSource = context.Root.GetComponent<HumanPoseTransfer>();
                //box-manというのが出てくるけど出したくないので隠します。
                _humanPoseTransferSource.GetComponent<SkinnedMeshRenderer>().enabled = false;

                _humanPoseTransferTarget.Source = _humanPoseTransferSource;
                _humanPoseTransferTarget.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
                //競合するので。
                _simpleAnimation.enabled = false;
                _animator.enabled = false;

                //いったんIKからアニメーションにブレンディングし、後で元に戻す
                _ikWeightCrossFade.FadeOutArmIkWeights(_ikFadeDuration);

                float duration = context.Bvh.FrameCount * Time.deltaTime;
                Debug.Log("duration = " + duration.ToString("00.000"));
                _ikFadeInCountDown = duration - _ikFadeDuration;
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
                    if (_humanPoseTransferTarget != null)
                    {
                        _humanPoseTransferTarget.Source = null;
                        _humanPoseTransferTarget.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.None;
                    }

                    if (_humanPoseTransferSource != null)
                    {
                        Destroy(_humanPoseTransferSource.gameObject);
                        _humanPoseTransferSource = null;
                    }
                    _bvhStopCountDown = 0f;
                    if (_animator != null)
                    {
                        _animator.enabled = true;
                    }
                    if (_simpleAnimation != null)
                    {
                        _simpleAnimation.enabled = true;
                    }
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

        private char KeyName2Char(string keyName)
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
