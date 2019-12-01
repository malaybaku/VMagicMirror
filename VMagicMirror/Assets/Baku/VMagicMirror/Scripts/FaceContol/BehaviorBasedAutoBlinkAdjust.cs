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
        
        //NOTE: めちゃ雑な方法として、リップシンクが無かどうか、という指標でもって発話を判定します。ホントはもっと丁寧にやってね
        [Tooltip("発話の状態取得をする根拠となるリップシンク")]
        [SerializeField] private OVRLipSyncContextBase lipSyncContext = null;

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
        
        //首振りのほう
        private float _headRotBlinkCoolDownCount = 0f;
        private bool _isVrmLoaded = false;
        private Transform _head = null;
        private Quaternion _prevHeadRotation = Quaternion.identity;
        private float _rotationDegree = 0f;
        private bool _willBlinkByHeadMotion = false;
        
        //音素検出のほう
        private float _lipSyncBlinkCoolDownCount = 0f;
        private int _lipSyncOffCount = 0;
        private int _lipSyncOnCount = 0;
        private bool _isTalking = false;

        public bool EnableHeadRotationBasedBlinkAdjust { get; set; } = true;
        public bool EnableLipSyncBasedBlinkAdjust { get; set; } = true;
 
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += vrm =>
            {
                _head = vrm.animator.GetBoneTransform(HumanBodyBones.Head);
                _prevHeadRotation = _head.rotation;
                _rotationDegree = 0;
                _isVrmLoaded = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _isVrmLoaded = false;
                _rotationDegree = 0;
                _head = null;
            };
        }

        private void Start()
        {
            StartCoroutine(ReadRotationsAtEndOfFrame());
        }

        private void Update()
        {
            if (_headRotBlinkCoolDownCount > 0)
            {
                _headRotBlinkCoolDownCount -= Time.deltaTime;
                //Hack: クールダウン中の首振りは無視する。このほうがヘンなタイミングで瞬きが発生しにくい…はず
                _rotationDegree = 0f;
            }
            else
            {
                UpdateByHeadRotation();
            }

            if (_lipSyncBlinkCoolDownCount > 0)
            {
                _lipSyncBlinkCoolDownCount -= Time.deltaTime;
                //Hack: クールダウン中は声の解析も実質的にストップ: これもへんてこ挙動防止です
                _isTalking = false;
                _lipSyncOffCount = 0;
                _lipSyncOnCount = 0;
            }
            else
            {
                UpdateByLipSync();
            }
        }

        private void UpdateByHeadRotation()
        {
            if (!EnableHeadRotationBasedBlinkAdjust)
            {
                return;
            }
            
            _rotationDegree = Mathf.Max(
                _rotationDegree - headRotResetSpeed * Time.deltaTime, 0
                );
            
            if (_willBlinkByHeadMotion)
            {
                _willBlinkByHeadMotion = false;
                _headRotBlinkCoolDownCount = coolDown;
                if (Random.Range(0.0f, 1.0f) < headRotBlinkProbability)
                {
                    autoBlink.ForceStartBlink();
                }
            }
            
        }

        private void UpdateByLipSync()
        {
            //ざっくりやりたいこと: 音節の区切りをvisemeベースで推定し、visemeが有→無に転じたところで音節が区切れたものと扱う。
            //ただし、毎フレームでやるとノイズ耐性が低いので、数フレーム連続で続いた場合の立ち上がり/立ち下がりだけを扱う。
            if (!EnableLipSyncBasedBlinkAdjust ||
                !lipSyncContext.enabled || 
                !(lipSyncContext.GetCurrentPhonemeFrame() is OVRLipSync.Frame frame)
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

                var rot = _head.rotation;
                
                (rot * Quaternion.Inverse(_prevHeadRotation)).ToAngleAxis(out float difAngle, out _);

                if (_headRotBlinkCoolDownCount <= 0)
                {
                    _rotationDegree += difAngle;
                }
                
                if (_rotationDegree > headRotThreshold)
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
