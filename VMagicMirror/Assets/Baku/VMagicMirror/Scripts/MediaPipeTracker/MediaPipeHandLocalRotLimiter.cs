using UnityEngine;
using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    // NOTE: 今やってる実装よりもキレイに制御する場合、手のIKの後処理ではなく下記のアプローチを取るのが良さそう。
    // ここまでやると手が誤ってクロスする問題も減るはず
    // - MediaPipeのタスクとしてHandではなくHolisticを使う
    // - とくに、ひじのBendGoalの参考情報としてHolisticの結果も使う

    public class MediaPipeHandLocalRotLimiter : PresenterBase
    {
        // Twistを除いた回転全体に対して回転量制限をする場合の上限角度 [deg]
        private const float ClampSwing = 30f;
        
        // 屈伸方向の制限角度 [deg]
        private const float ClampSwingBendStretch = 70f;

        // 「さよなら」とかで手を振る方向の制限角度 [deg]
        // NOTE: 現実の人体ではこの方向の曲げ角は20-30degが上限。
        // ただしVMMのIKではヒジを締めて手IKで動かす(=手首側に回転成分を押し付けやすい)ので、制限しすぎると全然曲がらなく見える
        // …という特性を踏まえて、少し緩めの角度制限にしている
        private const float ClampSwingLeftRight = 40f;

        private const float BendAngleMax = Mathf.Deg2Rad * ClampSwingBendStretch;
        private const float LeftRightAngleMax = Mathf.Deg2Rad * ClampSwingLeftRight;
        
        private readonly IVRMLoadable _vrmLoadable;
        private readonly LateUpdateSourceAfterFinalIK _lateUpdateSource;
        private readonly HandIKIntegrator _handIKIntegrator;

        private bool _hasModel;
        private Transform _leftHandBone;
        private Transform _rightHandBone;
        
        [Inject]
        public MediaPipeHandLocalRotLimiter(
            IVRMLoadable vrmLoadable,
            LateUpdateSourceAfterFinalIK lateUpdateSource,
            HandIKIntegrator handIKIntegrator
            )
        {
            _vrmLoadable = vrmLoadable;
            _handIKIntegrator = handIKIntegrator;
            _lateUpdateSource = lateUpdateSource;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                _leftHandBone = info.animator.GetBoneTransform(HumanBodyBones.LeftHand);
                _rightHandBone = info.animator.GetBoneTransform(HumanBodyBones.RightHand);
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _leftHandBone = null;
                _rightHandBone = null;
            };
            
            // NOTE: 発火タイミングの調整がしたい(TwistRelaxerのちょっと後くらいにLateUpateが走ってほしい)のでMonoBehaviourでやる
            _lateUpdateSource.OnPreLateUpdate
                .Subscribe(_ => LateUpdate())
                .AddTo(this);
        }

        private void LateUpdate()
        {
            if (!_hasModel) return;

            if (_handIKIntegrator.LeftTargetType.CurrentValue is HandTargetType.ImageBaseHand)
            {
                _leftHandBone.localRotation = GetClampedRotation(_leftHandBone.localRotation);
            }
        
            if (_handIKIntegrator.RightTargetType.CurrentValue is HandTargetType.ImageBaseHand)
            {
                _rightHandBone.localRotation = GetClampedRotation(_rightHandBone.localRotation);
            }
        }

        // 簡単だがキレイじゃない版: swing-twist分解して、swingの方に定数で制限をかける
        private static Quaternion GetClampedRotationNaive(Quaternion localRotation)
        {
            DecomposeSwingTwist(localRotation, out var swing, out var twist);

            swing.ToAngleAxis(out var swingAngle, out var swingAxis);
            swingAngle = Mathf.DeltaAngle(0f, swingAngle);

            if (Mathf.Abs(swingAngle) > ClampSwing)
            {
                swing = Quaternion.AngleAxis(Mathf.Sign(swingAngle) * ClampSwing, swingAxis);
                return swing * twist;
            }
            else
            {
                return localRotation;
            }
        }
        
        // 複雑だけど見た目が良い版: swing-twist分解したあと、swingをさらに屈伸とそうでない(左右に振る)方向に分けて制限
        private static Quaternion GetClampedRotation(Quaternion localRotation)
        {
            DecomposeSwingTwist(localRotation, out var swing, out var twist);

            // スイングの回転軸を前腕軸まわり角φに落とす
            swing.ToAngleAxis(out var swingAngle, out var swingAxis);
            swingAngle = Mathf.Abs(Mathf.DeltaAngle(0f, swingAngle)); // [0,180]

            // phi: 左右振り / 屈伸どっちに対して主に回転するような回転になってるかを表すような角度
            var phi = Mathf.Atan2(swingAxis.y, swingAxis.x);
            var c = Mathf.Cos(phi); 
            var s = Mathf.Sin(phi);

            // ここはrad
            var thetaMax = 1.0f / Mathf.Sqrt(
                (c * c) / (BendAngleMax * BendAngleMax) +
                (s * s) / (LeftRightAngleMax * LeftRightAngleMax)
                );
            var thetaMaxDeg = thetaMax * Mathf.Rad2Deg;

            if (swingAngle > thetaMaxDeg)
            {
                swing = Quaternion.AngleAxis(thetaMaxDeg, swingAxis);
            }

            return swing * twist;
        }
        
        private static void DecomposeSwingTwist(Quaternion q, out Quaternion swing, out Quaternion twist)
        {
            // やってること: ねじり回転だけ抽出することで、捻り + それ以外に分ける
            var twistAxis = Vector3.right;
            var proj = Vector3.Project(new Vector3(q.x, q.y, q.z), twistAxis);
            var qTwist = new Quaternion(proj.x, proj.y, proj.z, q.w);
            qTwist = Quaternion.Normalize(qTwist);
            twist = qTwist;
            swing = q * Quaternion.Inverse(twist);
        }
    }
}
