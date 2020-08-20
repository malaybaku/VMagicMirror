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
    public class NonImageBasedMotion : MonoBehaviour
    {
        private FaceControlConfiguration _faceConfig;
        private VoiceOnOffParser _voiceOnOffParser = null;

        //NOTE: 首の動きがeaseされるのを踏まえて気持ちゆっくりめ
        [SerializeField] private float eyeSpeedFactor = 4.0f;

        [Tooltip("アクション中/非アクション中のモーションを遷移させる時間")]
        [Range(0.1f, 2.0f)] [SerializeField] private float motionBlendDuration = 0.3f;
        
        [Tooltip("active状態で首をやや大きく動かすとき、目を首と逆方向に動かす確率")]
        [Range(0f, 1f)] [SerializeField] private float keepEyeCenterProbability = 0.1f;

        [Tooltip("active状態のとき、首の回転角に対して目の回転角を何倍にする、みたいなファクター")]
        [SerializeField] private float eyeRotationFactor = 0.5f;

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
            DeviceSelectableLipSyncContext lipSync
            )
        {
            _faceTracker = faceTracker;
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableVoiceBasedMotion,
                command => _operationEnabled = command.ToBoolean());

            _voiceOnOffParser = new VoiceOnOffParser(lipSync)
            {
                //そこそこちゃんと喋ってないと検出しない、という設定のつもり
                VisemeThreshold = 0.2f,
                OnCountThreshold = 6,
                OffCountThreshold = 16,
            };
            
            _motionApplier = new NonImageBasedMotionApplier(vrmLoadable);
        }

        /// <summary> 目に追加してほしい回転値。機能が無効なときは無回転。 </summary>
        public Quaternion EyeRotation { get; private set; } = Quaternion.identity;
        
        /// <summary> 首に追加してほしい回転値(※HeadとNeckの合計値) </summary>
        public Quaternion HeadRotation { get; private set; } = Quaternion.identity;

        private NonImageBasedMotionApplier _motionApplier = null;
        private readonly Jitter _inactiveJitter = new Jitter();
        private readonly Jitter _activeJitter = new Jitter();
        private Quaternion _activeEyeRotationTarget = Quaternion.identity;
        private Quaternion _activeEyeRotation = Quaternion.identity;
        
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
        //NOTE: 判断基準としては
        //「トラッキングが全く無いか、または顔トラッキングのチェック自体はオンだけど
        //(カメラ選びが不適切とかで)実質的に動いてない」
        //という状況なら処理を適用してもいいよね、と判断する
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
                    EyeRotation = Quaternion.identity;
                    HeadRotation = Quaternion.identity;
                    _activeJitter.Reset();
                    _inactiveJitter.Reset();   
                }
            }
        }
        
        //現在webカメラも外部トラッキングも無効のとき、つまりこの処理の出番っぽいときはtrueになる
        private bool _isNoTrackingApplied = true;

        /// <summary> Updateで有効な姿勢制御を行ってもよいかどうかを取得、設定します。 </summary>
        public bool IsNoTrackingApplied
        {
            get => _isNoTrackingApplied;
            set
            {
                if (_isNoTrackingApplied == value)
                {
                    return;
                }
                _isNoTrackingApplied = value;
                ShouldApply = IsNoTrackingApplied || !_faceTracker.FaceDetectedAtLeastOnce;
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
                bool keepEyeCenter = Random.Range(0f, 1f) < keepEyeCenterProbability;
                
                var sign = keepEyeCenter ? -1f : 1f;

                var rawRotation = new Vector2(
                    euler.x * eyeRotationFactor * sign,
                    euler.y * eyeRotationFactor * sign
                    );

                if (keepEyeCenter)
                {
                    //注視点維持はキレイに動かすのを目標とする: 同じ位置を見ようとしての動き、のはずなので
                    _activeEyeRotationTarget = Quaternion.Euler(rawRotation.x, rawRotation.y, 0);
                }
                else
                {
                    //視線を首と同じ方向に動かすとき、注視点がテキトーに離れるので向きがぴったり揃う必要はない。
                    //そこで、ぴったり同じ方向にせず、適当に汚す。
                    var angle = Random.Range(
                        -eyeOrientationVaryRange * Mathf.Deg2Rad, eyeOrientationVaryRange * Mathf.Deg2Rad
                    );

                    var cos = Mathf.Cos(angle);
                    var sin = Mathf.Sin(angle);
                    _activeEyeRotationTarget = Quaternion.Euler(
                        cos * rawRotation.x - sin * rawRotation.y,
                        sin * rawRotation.x + cos * rawRotation.y,
                        0
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
            
            ShouldApply = IsNoTrackingApplied || !_faceTracker.FaceDetectedAtLeastOnce;
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
            _motionApplier.Apply(HeadRotation, EyeRotation);
        }
        
        private void CalculateAngles()
        {
            if (!ShouldApply)
            {
                return;
            }

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
            _activeEyeRotation = Quaternion.Slerp(
                _activeEyeRotation,
                _activeEyeRotationTarget,
                eyeSpeedFactor * Time.deltaTime
            );

            float blendWeight = Mathf.SmoothStep(0, 1, _actionBlendWeight);
            
            //アクティブ用と非アクティブ用の計算がどちらもキレイに走っているので、それらをブレンディングすれば勝ち
            HeadRotation = Quaternion.Slerp(
                _inactiveJitter.CurrentRotation,
                _activeJitter.CurrentRotation,
                blendWeight
            );
            
            //非アクティブの目は便宜的に正面向き: ここは後で変わるかも。
            EyeRotation = Quaternion.Slerp(
                Quaternion.identity,
                _activeEyeRotation,
                blendWeight
            );
        }

    }
    
    /// <summary> 画像なしモーションの適用部分だけサブルーチン化したやつ </summary>
    class NonImageBasedMotionApplier 
    {
        public NonImageBasedMotionApplier(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
                _neck = info.animator.GetBoneTransform(HumanBodyBones.Neck);
                _leftEye = info.animator.GetBoneTransform(HumanBodyBones.LeftEye);
                _rightEye = info.animator.GetBoneTransform(HumanBodyBones.RightEye);
                _hasNeck = _neck != null;
                _hasEye = (_leftEye != null && _rightEye != null);
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hasEye = false;
                _hasNeck = false;
                _head = null;
                _neck = null;
                _leftEye = null;
                _rightEye = null;
            };
        }

        //Neckがある場合にNeckとHeadに回転を分配する度合い
        private const float HeadRate = 0.5f;
        private const float HeadTotalRotationLimitDeg = 40.0f;
        
        private Transform _head;
        private Transform _neck;
        private Transform _leftEye;
        private Transform _rightEye;
        private bool _hasModel;
        private bool _hasNeck;
        private bool _hasEye;

        public void Apply(Quaternion headRot, Quaternion eyeRot)
        {
            if (!_hasModel)
            {
                return;
            }
            
            if (_hasEye)
            {
                //NOTE: 前処理で目ボーンが毎フレーム中央付近にリセットされる、という前提でこう書いてます
                _leftEye.localRotation *= eyeRot;
                _rightEye.localRotation *= eyeRot;
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
