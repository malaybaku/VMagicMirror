using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 首振り運動や発話の切れ目を参照してまばたき間隔を制御するすごいやつだよ
    /// </summary>
    public class BehaviorBasedAutoBlinkAdjust : MonoBehaviour
    {
        //NOTE: ヨーだけ使うのがいいかもしれないんだけど、ちょっと判断が難しい…
        [Tooltip("degベースで、この値だけ首を振るとまばたきイベントの判定を行う")]
        [SerializeField] private float headRotThreshold = 25f;

        [Tooltip("degベースで、首振りがほとんど発生してないときに累計値をリセットするための減速度(deg/s)")]
        [SerializeField] private float headRotResetSpeed = 10f;

        [Tooltip("計算上まばたきイベントの発生条件を満たしたとき、実際にまばたきをさせる確率")]
        [Range(0f, 1f)]
        [SerializeField] private float headRotBlinkProbability = 1.0f;

        [Tooltip("無次元化した視線の移動量(/sec)がこの値を下から上にまたいだとき、まばたき条件を満たす")]
        [SerializeField] private float eyeRotRateSpeedThreshold = 2.5f;
        
        [Tooltip("視線の向き変化によるまばたきイベントの発生条件を満たしたとき、実際にまばたきする確率")]
        [Range(0f, 1f)] 
        [SerializeField] private float eyeRotBlinkProbability = 0.7f;

        [Tooltip("Visemeのなかでこのしきい値を超える値が一つでもあれば、発声中だと判定する")]
        [SerializeField] private float lipSyncVisemeThreshold = 0.1f;

        [Tooltip("このフレーム数だけvisemeのオン・オフが同じ状態が継続したら切り替わり判定する")]
        [SerializeField] private int lipSyncOnCountThreshold = 3;
        [SerializeField] private int lipSyncOffCountThreshold = 3;

        [Tooltip("リップシンクベースで音節区切りを検出したとき、まばたきイベントを発生させる確率")]
        [Range(0f, 1f)]
        [SerializeField] private float lipSyncBlinkProbability = 0.3f;
        
        [Tooltip("このコンポーネントが瞬きイベントを1回発生させたあと、しばらく次のイベントを送らないクールダウンタイム")]
        [SerializeField] private float coolDown = 2.0f;

        [Tooltip("瞬き検出結果の入力先")]
        [SerializeField] private VRMAutoBlink autoBlink = null;
        
        private readonly WaitForEndOfFrame _waiter = new WaitForEndOfFrame();
        
        private bool _isVrmLoaded = false;

        //首振り
        private float _headRotBlinkCoolDownCount = 0f;
        private Transform _head = null;
        private Quaternion _prevHeadRotation = Quaternion.identity;
        private float _headRotationDegree = 0f;
        private bool _willBlinkByHeadMotion = false;

        //目のボーン回転
        private float _eyeRotBlinkCoolDownCount = 0f;
        //NOTE: この速度の計算根拠としては「左目と右目が見てる方向ベクトルの変化量」を使う。
        //ただし、首が動いたケースの計算は別でやってる事から、ここでは「首を使わない眼球運動」を扱うようにする。
        //ちょっとだけならしたいので、2フレーム分の値で速度を計算します
        private bool _willBlinkByEyeMotion = false;
        private Vector2 _prevEyeLookRate = Vector2.zero;
        private Vector2 _prev2EyeLookRate = Vector2.zero;
        private float _prevEyeRotRateSpeed = 0f;

        //リップシンク
        private VmmLipSyncContextBase _lipSyncContext = null;
        private float _lipSyncBlinkCoolDownCount = 0f;
        private int _lipSyncOffCount = 0;
        private int _lipSyncOnCount = 0;
        private bool _isTalking = false;

        public bool EnableHeadRotationBasedBlinkAdjust { get; set; } = true;
        public bool EnableLipSyncBasedBlinkAdjust { get; set; } = true;
 
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver, VmmLipSyncContextBase lipSyncContext)
        {
            _lipSyncContext = lipSyncContext;
            vrmLoadable.VrmLoaded += vrm =>
            {
                _head = vrm.controlRig.GetBoneTransform(HumanBodyBones.Head);
                _prevHeadRotation = _head.rotation;
                _headRotationDegree = 0;
                _isVrmLoaded = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _isVrmLoaded = false;
                _headRotationDegree = 0;
                _head = null;
            };
            
            var _ = new BehaviorBasedBlinkReceiver(receiver, this);
        }

        //NOTE: 無次元化された目の状態であって、Jitterの成分が除去されたような値を入れる
        public void SetEyeMoveRate(Vector2 rate)
        {
            //NOTE: 2で割るのは2フレーム前の状態とくらべているから
            var speed = (rate - _prev2EyeLookRate).magnitude * 0.5f / Time.deltaTime;
            
            if (_eyeRotBlinkCoolDownCount <= 0 &&
                _prevEyeRotRateSpeed < eyeRotRateSpeedThreshold &&
                speed >= eyeRotRateSpeedThreshold)
            {
                _willBlinkByEyeMotion = true;
            }

            _prevEyeRotRateSpeed = speed;
            _prev2EyeLookRate = _prevEyeLookRate;
            _prevEyeLookRate = rate;
        }
        
        private void Start()
        {
            StartCoroutine(ReadRotationsAtEndOfFrame());
        }

        private void Update()
        {
            UpdateByHeadRotation();
            UpdateByEyeRotation();
            UpdateByLipSync();
        }

        private void UpdateByHeadRotation()
        {
            if (_headRotBlinkCoolDownCount > 0)
            {
                _headRotBlinkCoolDownCount -= Time.deltaTime;
                //Hack: クールダウン中の首振りは無視する。このほうがヘンなタイミングで瞬きが発生しにくい…はず
                _headRotationDegree = 0f;
                return;
            }

            _headRotationDegree = Mathf.Max(
                _headRotationDegree - headRotResetSpeed * Time.deltaTime, 0
                );
            
            if (!EnableHeadRotationBasedBlinkAdjust)
            {
                return;
            }
            
            if (_willBlinkByHeadMotion)
            {
                _willBlinkByHeadMotion = false;
                _headRotBlinkCoolDownCount = coolDown;
                if (Random.Range(0.0f, 1.0f) < headRotBlinkProbability)
                {
                    autoBlink.ForceStartBlink();
                    //首振りベースでまばたきしたら、視線ベースのまばたきもしばらく止める
                    _willBlinkByEyeMotion = false;
                    _eyeRotBlinkCoolDownCount = coolDown;
                }
            }
        }

        private void UpdateByEyeRotation()
        {
            if (_eyeRotBlinkCoolDownCount > 0)
            {
                _eyeRotBlinkCoolDownCount -= Time.deltaTime;
                return;
            }
            
            if (!_isVrmLoaded || !EnableHeadRotationBasedBlinkAdjust)
            {
                return;
            }
            
            if (_willBlinkByEyeMotion)
            {
                _willBlinkByEyeMotion = false;
                _eyeRotBlinkCoolDownCount = coolDown;
                if (Random.Range(0f, 1f) < eyeRotBlinkProbability)
                {
                    autoBlink.ForceStartBlink();
                    
                    //NOTE: 視線ベースでまばたきした場合、首振りベースのまばたきもクールダウンでストップさせる
                    _willBlinkByHeadMotion = false;
                    _headRotBlinkCoolDownCount = coolDown;
                }
            }
        }
        
        private void UpdateByLipSync()
        {
            if (_lipSyncBlinkCoolDownCount > 0)
            {
                _lipSyncBlinkCoolDownCount -= Time.deltaTime;
                //Hack: クールダウン中は声の解析も実質的にストップ: これもへんてこ挙動防止です
                _isTalking = false;
                _lipSyncOffCount = 0;
                _lipSyncOnCount = 0;
                return;
            }
            
            //ざっくりやりたいこと: 音節の区切りをvisemeベースで推定し、visemeが有→無に転じたところで音節が区切れたものと扱う。
            //ただし、毎フレームでやるとノイズ耐性が低いので、数フレーム連続で続いた場合の立ち上がり/立ち下がりだけを扱う。
            if (!EnableLipSyncBasedBlinkAdjust ||
                !_lipSyncContext.enabled || 
                !(_lipSyncContext.GetCurrentPhonemeFrame() is OVRLipSync.Frame frame)
            )
            {
                return;
            }

            bool isTalking = false;
            //NOTE: 0にはsil(無音)があるのでそれを避ける
            for (int i = 1; i < frame.Visemes.Length; i++)
            {
                if (frame.Visemes[i] > lipSyncVisemeThreshold)
                {
                    isTalking = true;
                    break;
                }
            }

            if (isTalking)
            {
                _lipSyncOnCount++;
                _lipSyncOffCount = 0;
                if (_lipSyncOnCount >= lipSyncOnCountThreshold)
                {
                    SetIsTalking(true);
                    _lipSyncOnCount = lipSyncOnCountThreshold;
                }
            }
            else
            {
                _lipSyncOffCount++;
                _lipSyncOnCount = 0;
                if (_lipSyncOffCount >= lipSyncOffCountThreshold)
                {
                    SetIsTalking(false);
                    _lipSyncOffCount = lipSyncOffCountThreshold;
                }
            }
        }
        
        private IEnumerator ReadRotationsAtEndOfFrame()
        {
            while (true)
            {
                yield return _waiter;
                if (!_isVrmLoaded)
                {
                    continue;
                }
                
                ReadHeadRotation();
            }

            void ReadHeadRotation()
            {
                var rot = _head.rotation;
                (rot * Quaternion.Inverse(_prevHeadRotation)).ToAngleAxis(out float diffAngle, out _);
                diffAngle = Mathf.Repeat(diffAngle + 180f, 360f) - 180f;
                
                if (_headRotBlinkCoolDownCount <= 0)
                {
                    _headRotationDegree += diffAngle;
                }
                
                if (_headRotationDegree > headRotThreshold)
                {
                    _willBlinkByHeadMotion = true;
                }
                _prevHeadRotation = rot;
            }
        }
        
        private void SetIsTalking(bool isTalking)
        {
            if (_isTalking == isTalking)
            {
                return;
            }
            _isTalking = isTalking;
            
            //喋ってる状態から静かになった -> 喋りの区切り目のはずなのでまばたき判定に入る
            if (!_isTalking)
            {
                _lipSyncBlinkCoolDownCount = coolDown;
                if (Random.Range(0.0f, 1.0f) < lipSyncBlinkProbability)
                {
                    _lipSyncOffCount = 0;
                    _lipSyncOnCount = 0;
                    autoBlink.ForceStartBlink();
                }
            }
        }
    }
}
