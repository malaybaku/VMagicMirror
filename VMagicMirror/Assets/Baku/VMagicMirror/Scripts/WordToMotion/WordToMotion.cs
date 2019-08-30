//using UnityEngine;
//using UniRx;
//using VRM;

//namespace Baku.VMagicMirror
//{
//    [RequireComponent(typeof(WordToMotionMapper))]
//    [RequireComponent(typeof(IkWeightCrossFade))]
//    [RequireComponent(typeof(WordToMotionBlendShape))]
//    public class WordToMotion : MonoBehaviour
//    {
//        [SerializeField]
//        [Tooltip("この時間だけキー入力が無かったらワードが途切れたものとして入力履歴をクリアする。")]
//        private float _forgetTime = 1.0f;

//        [SerializeField]
//        [Tooltip("ワード由来のモーションに入る時にIKを無効化するときの所要時間")]
//        private float _ikFadeDuration = 0.5f;

//        [SerializeField]
//        private ReceivedMessageHandler handler = null;

//        //立ち状態が入ってればOK。
//        //コレが必要なのは、デフォルトアニメーションが無いと下半身を動かさないアニメーションで脚が骨折するため
//        [SerializeField]
//        private AnimationClip _defaultAnimation = null;

//        private WordToMotionMapper _mapper = null;
//        private IkWeightCrossFade _ikWeightCrossFade = null;
//        private WordToMotionBlendShape _blendShape = null;

//        private SimpleAnimation _simpleAnimation = null;
//        private string _prevStateName = "";

//        private readonly WordAnalyzer _analyzer = new WordAnalyzer();
//        private float _count = 0f;
//        private float _ikFadeInCountDown = 0f;
//        private float _blendShapeResetCountDown = 0f;


//        public void AssignSimpleAnimation(SimpleAnimation simpleAnimation)
//        {
//            _simpleAnimation = simpleAnimation;
//            _simpleAnimation.AddState(_defaultAnimation, "Default");
//        }

//        public void AssignBlendShapeProxy(VRMBlendShapeProxy proxy)
//        {
//            _blendShape.Initialize(proxy);
//        }

//        public void Dispose()
//        {
//            _simpleAnimation = null;
//            _blendShape.DisposeProxy();
//        }

//        private void Start()
//        {
//            _count = _forgetTime;
//            _mapper = GetComponent<WordToMotionMapper>();
//            _ikWeightCrossFade = GetComponent<IkWeightCrossFade>();
//            _blendShape = GetComponent<WordToMotionBlendShape>();
//            RefreshWordAndRequests();

//            handler?.Commands?.Subscribe(c =>
//            {
//                if (c.Command == MessageCommandNames.KeyDown)
//                {
//                    ReceiveKeyDown(c.Content);
//                }
//            });

//            _analyzer.WordDetected.Subscribe(w =>
//            {
//                _mapper.ReceiveWord(w);
//            });

//            _mapper.MotionRequested.Subscribe(request =>
//            {
//                if (_simpleAnimation == null)
//                {
//                    return;
//                }

//                //前半: モーションを適用
//                if (request.Animation != null)
//                {
//                    if (_simpleAnimation.GetState(request.Word) == null)
//                    {
//                        //TODO: Removeが何か上手く動かないのでフレーズ別に足すような仕様にしてるが、これリフレッシュしづらいんですよね
//                        _simpleAnimation.AddState(request.Animation, request.Word);
//                    }
//                    else
//                    {
//                        //2回目が動かない事があるので
//                        //_simpleAnimation.Stop(request.Word);
//                        _simpleAnimation.Rewind(request.Word);
//                    }

//                    if (!string.IsNullOrEmpty(_prevStateName) && _simpleAnimation.IsPlaying(_prevStateName))
//                    {
//                        _simpleAnimation.Stop(_prevStateName);
//                    }

//                    _simpleAnimation.Play(request.Word);
//                    _prevStateName = request.Word;

//                    //いったんIKからアニメーションにブレンディングし、後で元に戻す
//                    _ikWeightCrossFade.FadeOutArmIkWeights(_ikFadeDuration);
//                    _ikFadeInCountDown = request.Animation.length - _ikFadeDuration;
//                    //ここは短すぎるモーションを指定されたときの対策
//                    if (_ikFadeInCountDown <= 0)
//                    {
//                        _ikFadeInCountDown = 0.01f;
//                    }
//                }

//                //後半: 表情を適用。
//                if (request.BlendShape.Count > 0)
//                {
//                    //Clearからやっているのは前回分のブレンドシェイプが残ってると混ざっちゃうから
//                    _blendShape.Clear();
//                    foreach (var pair in request.BlendShape)
//                    {
//                        _blendShape.Add(pair.Key, pair.Value);
//                    }
//                    _blendShapeResetCountDown = request.Duration;
//                }

//            });
//        }

//        private void Update()
//        {
//            _count -= Time.deltaTime;
//            if (_count < 0)
//            {
//                _count = _forgetTime;
//                _analyzer.Clear();
//            }

//            if (_ikFadeInCountDown > 0)
//            {
//                _ikFadeInCountDown -= Time.deltaTime;
//                if (_ikFadeInCountDown <= 0)
//                {
//                    _ikWeightCrossFade.FadeInArmIkWeights(_ikFadeDuration);
//                }
//            }

//            if (_blendShapeResetCountDown > 0)
//            {
//                _blendShapeResetCountDown -= Time.deltaTime;
//                if (_blendShapeResetCountDown <= 0)
//                {
//                    _blendShape.Clear();
//                    _blendShape.ReserveResetBlendShape();
//                }
//            }
//        }

//        //NOTE: WPF側でファイルを改変した時も呼ぶ必要あり

//        private void RefreshWordAndRequests()
//        {
//            _mapper.Load();
//            _analyzer.LoadWordSet(_mapper.WordSet);
//        }

//        private void ReceiveKeyDown(string keyName)
//        {
//            _count = _forgetTime;
//            _analyzer.Add(KeyName2Char(keyName));
//        }

//        private char KeyName2Char(string keyName)
//        {
//            if (keyName.Length == 1)
//            {
//                //a-z
//                return keyName.ToLower()[0];
//            }
//            else if (keyName.Length == 2 && keyName[0] == 'D' && char.IsDigit(keyName[1]))
//            {
//                //D0 ~ D9 (テンキーじゃないほうの0~9)
//                return keyName[1];
//            }
//            else if (keyName.Length == 7 && keyName.StartsWith("NumPad") && char.IsDigit(keyName[6]))
//            {
//                //NumPad0 ~ NumPad9 (テンキーの0~9)
//                return keyName[6];
//            }
//            else
//            {
//                //TEMP: 「ヘンな文字でワードが途切れた」という情報だけ残す
//                return ' ';
//            }
//        }
//    }
//}
