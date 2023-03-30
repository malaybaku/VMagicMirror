using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像処理ではない、音声とかキー入力の情報をトリガーにして、首と目のルーチン的な運動情報を出力します。
    /// </summary>
    /// <remarks>
    /// 値を実際に使うのはカメラ系のトラッキングが一切効いてないときだけ。のつもり
    /// </remarks>
    public class NonImageBasedMotion : MonoBehaviour, IEyeRotationRequestSource
    {
        private FaceControlConfiguration _faceConfig;
        private GameInputBodyMotionController _gameInputBodyMotionController;
        private VoiceOnOffParser _voiceOnOffParser = null;

        //NOTE: 首の動きがeaseされるのを踏まえて気持ちゆっくりめ
        [SerializeField] private float eyeSpeedFactor = 4.0f;

        [Tooltip("アクション中/非アクション中のモーションを遷移させる時間")]
        [Range(0.1f, 2.0f)] [SerializeField] private float motionBlendDuration = 0.3f;
        
        [Tooltip("active状態で首をやや大きく動かすとき、目を首と逆方向に動かす確率")]
        [Range(0f, 1f)] [SerializeField] private float keepEyeCenterProbability = 0.1f;

        [Tooltip("active状態のとき、首の回転角に対して目の回転角を何倍にする、みたいなファクター")]
        [SerializeField] private float eyeRotationRateFactor = 0.05f;

        [Tooltip("目を首と順方向/逆方向に動かすとき、その方向ピッタリから角度を多少ランダムにしてもいいよね、という値")]
        [SerializeField] private float eyeOrientationVaryRange = 20f;
        
        [SerializeField] private Vector2 inactiveFactors = new Vector2(10, 10);
        [SerializeField] private Vector2 activeFactors = new Vector2(10, 10);

        //active/inactiveが1回切り替わったらしばらくフラグ状態を維持する長さ
        [SerializeField] private float activeSwitchCoolDownDuration = 1.0f;

        private FaceTracker _faceTracker = null;
        
        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable,
            FaceTracker faceTracker,
            GameInputBodyMotionController gameInputBodyMotionController,
            VmmLipSyncContextBase lipSyncContext
            )
        {
            _faceTracker = faceTracker;
            _gameInputBodyMotionController = gameInputBodyMotionController;
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableVoiceBasedMotion,
                command => _operationEnabled = command.ToBoolean());

            _voiceOnOffParser = new VoiceOnOffParser(lipSyncContext)
            {
                //そこそこちゃんと喋ってないと検出しない、という設定のつもり
                VisemeThreshold = 0.2f,
                OnCountThreshold = 6,
                OffCountThreshold = 16,
            };
            
            _motionApplier = new NonImageBasedMotionApplier(vrmLoadable);
        }
        
        #region IEyeRotationRequestSource
        
        bool IEyeRotationRequestSource.IsActive => ShouldApply;
        private Vector2 _eyeRotationRate = Vector2.zero;
        public Vector2 LeftEyeRotationRate => _eyeRotationRate;
        public Vector2 RightEyeRotationRate => _eyeRotationRate;

        #endregion

        /// <summary> 首に追加してほしい回転値(※HeadとNeckの合計値) </summary>
        public Quaternion HeadRotation { get; private set; } = Quaternion.identity;

        private NonImageBasedMotionApplier _motionApplier = null;
        private readonly Jitter _inactiveJitter = new Jitter();
        private readonly Jitter _activeJitter = new Jitter();

        private Vector2 _rawEyeRot = Vector2.zero;
        private Vector2 _rawEyeRotTarget = Vector2.zero;
        
        //0 ~ 1の値を取り、喋ってないモーションと喋ってるモーションをブレンドする重みに使います
        private float _actionBlendWeight = 0f;
        
        //音声またはキー入力とかによって「ユーザーが何かしら動いてるらしい」と判断できてる間はtrueになる値
        private bool _rawActive = false;
        //rawActiveをもとに、頻繁なチャタリングが起きないように加工されたフラグ
        private bool _active = false;
        private float _activeSwitchCountDown = 0f;

        //GUI上からこの処理を許可されてるかどうか
        private bool _operationEnabled = true;

        
        private bool _shouldApply = true;
        private bool ShouldApply
        {
            get => _shouldApply;
            set
            {
                if (_shouldApply == value)
                {
                    return;
                }

                _shouldApply = value;
                if (!_shouldApply)
                {
                    _voiceOnOffParser.Reset(false);
                    _rawActive = false;
                    _active = false;
                    _activeSwitchCountDown = 0f;
                    _eyeRotationRate = Vector2.zero;
                    _rawEyeRot = Vector2.zero;
                    HeadRotation = Quaternion.identity;
                    _activeJitter.Reset();
                    _inactiveJitter.Reset();   
                }
            }
        }

        //外部トラッキングについては接続できてる/できてないが明確なほうがバリューありそうなので、適用しない。
        //いっぽうwebカメラで顔トラが動く前 == 初期インストール直後を意味し、ここは親切にしたいので適用する。
        private void UpdateShouldApply() => ShouldApply = 
            FaceControlMode == FaceControlModes.None || 
            (FaceControlMode == FaceControlModes.WebCam && !_faceTracker.FaceDetectedAtLeastOnce);
        
        private FaceControlModes _faceControlMode = FaceControlModes.WebCam;
        public FaceControlModes FaceControlMode
        {
            get => _faceControlMode;
            set
            {
                if (_faceControlMode == value)
                {
                    return;
                }
                _faceControlMode = value;
                UpdateShouldApply();
            }
        }
        
        private void Start()
        {
            _inactiveJitter.AngleRange = new Vector3(4f, 4f, 4f);
            _inactiveJitter.ChangeTimeMin = 6.0f;
            _inactiveJitter.ChangeTimeMax = 15.0f;
            _inactiveJitter.DumpFactor = inactiveFactors.x;
            _inactiveJitter.PositionFactor = inactiveFactors.y;
            _inactiveJitter.UseZAngle = true;

            _activeJitter.AngleRange = new Vector3(6f, 12f, 12f);
            _activeJitter.ChangeTimeMin = 0.5f;
            _activeJitter.ChangeTimeMax = 2.0f;
            _activeJitter.DumpFactor = activeFactors.x;
            _activeJitter.PositionFactor = activeFactors.y;
            _activeJitter.UseZAngle = true;

            _activeJitter.JitterTargetEulerUpdated += euler =>
            {
                //注視点目標によってやることを変える
                var keepEyeCenter = Random.Range(0f, 1f) < keepEyeCenterProbability;
                var sign = keepEyeCenter ? -1f : 1f;
                
                var rawRotationRate = new Vector2(
                    euler.y * eyeRotationRateFactor * sign,
                    -euler.x * eyeRotationRateFactor * sign
                    );

                if (keepEyeCenter)
                {
                    //注視点維持はキレイに動かすのを目標とする。同じ位置を見ようとしての動きのはずなため
                    _rawEyeRotTarget = rawRotationRate;
                }
                else
                {
                    //視線を首とほぼ同じ方向に動かすとき、首と完全に一緒ではなく、ちょっとズレた方向を見るのを許可する。
                    
                    var angle = Random.Range(
                        -eyeOrientationVaryRange * Mathf.Deg2Rad, eyeOrientationVaryRange * Mathf.Deg2Rad
                    );

                    var cos = Mathf.Cos(angle);
                    var sin = Mathf.Sin(angle);
                    _rawEyeRotTarget = new Vector2(
                        cos * rawRotationRate.x - sin * rawRotationRate.y,
                        sin * rawRotationRate.x + cos * rawRotationRate.y
                    );
                }
            };
        }
        
        private void LateUpdate()
        {
            //オプションで止められている場合、本格的に何もやらない
            if (!_operationEnabled)
            {
                return;
            }
            
            UpdateShouldApply();
            if (!ShouldApply)
            {
                return;
            }
            
            //NOTE: ちょっとイビツな処理だが、これらのカウントは秒数と深いかかわりがあるので許してくれ…
            if (Application.targetFrameRate == 30)
            {
                _voiceOnOffParser.OnCountThreshold = 3;
                _voiceOnOffParser.OffCountThreshold = 8;
            }
            else
            {
                _voiceOnOffParser.OnCountThreshold = 6;
                _voiceOnOffParser.OffCountThreshold = 16;
            }

            //DEBUG: パラメータ感を知りたい
            _inactiveJitter.DumpFactor = inactiveFactors.x;
            _inactiveJitter.PositionFactor = inactiveFactors.y;
            _activeJitter.DumpFactor = activeFactors.x;
            _activeJitter.PositionFactor = activeFactors.y;
            
            CalculateAngles();
            _motionApplier.Apply(_gameInputBodyMotionController.LookAroundRotation * HeadRotation);
        }
        
        private void CalculateAngles()
        {
            //NOTE: 初期実装で声パーサーしか使わないのをいいことにこういう実装。
            _voiceOnOffParser.Update();
            _rawActive = _voiceOnOffParser.IsTalking;

            //チャタリングを防ぐようにして_activeに適用
            if (_activeSwitchCountDown > 0)
            {
                _activeSwitchCountDown -= Time.deltaTime;
            }
            if (_rawActive != _active && _activeSwitchCountDown <= 0f)
            {
                _active = _rawActive;
                _activeSwitchCountDown = activeSwitchCoolDownDuration;
            }

            //アクション中かどうかでブレンドウェイトが片側に寄っていく(最後は0か1で落ち着く)
            _actionBlendWeight = Mathf.Clamp01(
                _actionBlendWeight + Time.deltaTime / motionBlendDuration * (_active ? 1 : -1)
            );
            
            //個別の動きのアップデート
            _inactiveJitter.Update(Time.deltaTime);
            _activeJitter.Update(Time.deltaTime);
            
            _rawEyeRot = Vector2.Lerp(_rawEyeRot, _rawEyeRotTarget, eyeSpeedFactor * Time.deltaTime);

            float blendWeight = Mathf.SmoothStep(0, 1, _actionBlendWeight);
            
            //アクティブ用と非アクティブ用の計算がどちらもキレイに走っているので、それらをブレンディングすれば勝ち
            HeadRotation = Quaternion.Slerp(
                _inactiveJitter.CurrentRotation,
                _activeJitter.CurrentRotation,
                blendWeight
            );
            
            //非アクティブの目は便宜的に正面向き: ここは後で変わるかも。
            _eyeRotationRate = Vector2.Lerp(Vector2.zero, _rawEyeRot, blendWeight);
        }

    }
    
    /// <summary> 画像なしモーションの適用部分だけサブルーチン化したやつ </summary>
    class NonImageBasedMotionApplier 
    {
        public NonImageBasedMotionApplier(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _head = info.controlRig.GetBoneTransform(HumanBodyBones.Head);
                _neck = info.controlRig.GetBoneTransform(HumanBodyBones.Neck);
                _hasNeck = _neck != null;
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hasNeck = false;
                _head = null;
                _neck = null;
            };
        }

        //Neckがある場合にNeckとHeadに回転を分配する度合い
        private const float HeadRate = 0.5f;
        private const float HeadTotalRotationLimitDeg = 40.0f;
        
        private Transform _head;
        private Transform _neck;
        private bool _hasModel;
        private bool _hasNeck;

        public void Apply(Quaternion headRot)
        {
            if (!_hasModel)
            {
                return;
            }
            
            //首と頭を一括で回すにあたって、コーナーケースを安全にするため以下のアプローチを取る
            // - 一旦今の回転値を混ぜて、
            // - 角度制限つきの最終的な回転値を作り、
            // - その回転値を角度ベースで首と頭に配り直す
            var totalRot = _hasNeck
                ? headRot * _neck.localRotation * _head.localRotation
                : headRot * _head.localRotation;
            
            totalRot.ToAngleAxis(
                out float totalHeadRotDeg,
                out Vector3 totalHeadRotAxis
            );
            totalHeadRotDeg = Mathf.Repeat(totalHeadRotDeg + 180f, 360f) - 180f;
            
            //素朴に値を適用すると首が曲がりすぎる、と判断されたケース
            if (Mathf.Abs(totalHeadRotDeg) > HeadTotalRotationLimitDeg)
            {
                totalHeadRotDeg = Mathf.Sign(totalHeadRotDeg) * HeadTotalRotationLimitDeg;
            }

            if (_hasNeck)
            {
                _neck.localRotation = Quaternion.AngleAxis(totalHeadRotDeg * (1 - HeadRate), totalHeadRotAxis);
                _head.localRotation = Quaternion.AngleAxis(totalHeadRotDeg * HeadRate, totalHeadRotAxis);
            }
            else
            {
                _head.localRotation = Quaternion.AngleAxis(totalHeadRotDeg, totalHeadRotAxis);
            }
        }
    }
}
