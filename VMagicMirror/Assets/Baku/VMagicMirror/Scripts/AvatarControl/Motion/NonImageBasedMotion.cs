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

        [Tooltip("離散的なアクション(ボタン押しとか)が終わったあとでアクション完了を判断するまでのフレーム数の下限")]
        [SerializeField] private int actionEndCountMin = 30;
        [Tooltip("離散的なアクション(ボタン押しとか)が終わったあとでアクション完了を判断するまでのフレーム数の上限")]
        [SerializeField] private int actionEndCountMax = 240;

        [SerializeField] private float activeSpeedFactor = 6.0f;
        [SerializeField] private float inactiveSpeedFactor = 3.0f;

        [Tooltip("アクション中/非アクション中のモーションを遷移させる時間")]
        [Range(0.1f, 2.0f)] [SerializeField] private float motionBlendDuration = 0.3f;
        
        [Tooltip("active状態で首をやや大きく動かすとき、目を首と逆方向に動かす確率")]
        [Range(0f, 1f)] [SerializeField] private float keepEyeCenterProbability = 0.1f;

        [Tooltip("active状態のとき、首の回転角に対して目の回転角を何倍にする、みたいなファクター")]
        [SerializeField] private float eyeRotationFactor = 0.5f;

        [Tooltip("目を首と順方向/逆方向に動かすとき、その方向ピッタリから角度を多少ランダムにしてもいいよね、という値")]
        [SerializeField] private float eyeOrientationVaryRange = 20f;
        
        
        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable,
            DeviceSelectableLipSyncContext lipSync
            )
        {
            //TODO: オプトアウトをreceiverからやるのでは？

            _voiceOnOffParser = new VoiceOnOffParser(lipSync)
            {
                OnCountThreshold = 8,
                OffCountThreshold = 20,
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
        private bool _isActive = false;
        private int _actionEndCountdown = 0;

        /// <summary> Updateで有効な姿勢制御を行ってもよいかどうかを取得、設定します。 </summary>
        public bool IsBehaviourActive { get; set; } = true;

        private void Start()
        {
            //NOTE: このへんSerializeFieldに公開するのも手
            _inactiveJitter.AngleRange = new Vector3(5f, 5f, 5f);
            _inactiveJitter.SpeedFactor = inactiveSpeedFactor;
            _inactiveJitter.ChangeTimeMin = 2.0f;
            _inactiveJitter.ChangeTimeMax = 8.0f;
            _inactiveJitter.UseZAngle = true;

            _activeJitter.AngleRange = new Vector3(10f, 10f, 10f);
            _activeJitter.SpeedFactor = activeSpeedFactor;
            _activeJitter.ChangeTimeMin = 0.8f;
            _activeJitter.ChangeTimeMax = 5.0f;
            _activeJitter.UseZAngle = true;

            _activeJitter.JitterTargetEulerUpdated += euler =>
            {
                //注視点目標によってやることを変える
                bool keepEyeCenter = UnityEngine.Random.Range(0f, 1f) < keepEyeCenterProbability;
                
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
                    var angle = UnityEngine.Random.Range(
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
            CalculateAngles();
            if (IsBehaviourActive)
            {
                _motionApplier.Apply(HeadRotation, EyeRotation);
            }
        }
        
        private void CalculateAngles()
        {
            if (!IsBehaviourActive)
            {
                _voiceOnOffParser.Reset(false);
                _isActive = false;
                _actionEndCountdown = 0;
                EyeRotation = Quaternion.identity;
                HeadRotation = Quaternion.identity;
                return;
            }

            //NOTE: 初期実装で声パーサーしか使わないのをいいことにこういう実装。
            _voiceOnOffParser.Update();
            if (_voiceOnOffParser.IsTalking)
            {
                _isActive = true;
                //NOTE: 声のパース処理はそれ自体がある程度余裕を持った処理なので、
                //声が切れたらほぼ直ちにアクション無し状態にする
                _actionEndCountdown = Mathf.Max(_actionEndCountdown, 1);
            }
            
            //カウントダウン + フラグ折り
            if (_actionEndCountdown >= 0)
            {
                _actionEndCountdown--;
            }
            if (_actionEndCountdown < 0)
            {
                _isActive = false;
            }

            //アクション中かどうかでブレンドウェイトが片側に寄っていく(最後は0か1で落ち着く)
            _actionBlendWeight = Mathf.Clamp01(
                Time.deltaTime / motionBlendDuration * (_isActive ? 1 : 0)
            );
            
            //個別のJitter群のアップデート
            _inactiveJitter.Update(Time.deltaTime);
            _activeJitter.Update(Time.deltaTime);
            _activeEyeRotation = Quaternion.Slerp(
                _activeEyeRotation,
                _activeEyeRotationTarget,
                activeSpeedFactor * Time.deltaTime
            );

            //アクティブ用と非アクティブ用の計算がどちらもキレイに走っているので、それらをブレンディングすれば勝ち
            HeadRotation = Quaternion.Slerp(
                _inactiveJitter.CurrentRotation,
                _activeJitter.CurrentRotation,
                _actionBlendWeight
            );
            
            //非アクティブの目は便宜的に正面向き: ここは後で変わるかも。
            EyeRotation = Quaternion.Slerp(
                Quaternion.identity,
                _activeEyeRotation,
                _actionBlendWeight
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
