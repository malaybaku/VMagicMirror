using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// 手を下げた姿勢のデフォルト値(カスタム姿勢ではない)を計算できるやつ
    /// </summary>
    public class HandDownIkCalculator : IInitializable
    {
        // ハンドトラッキングがロスしたときにAポーズへ落とし込むときの、腕の下げ角度(手首の曲げもコレに準拠します
        private const float APoseArmDownAngleDeg = 70f;
        // リファレンスモデル(Megumi Baxterさん)のUpperArmボーンからWristボーンまでの距離
        private const float ReferenceArmLength = 0.37f;
        // 腕のピンと張る度合いがこの値になるように手IKのy座標を調整する
        private const float ArmRelaxFactor = 0.99f;

        // Aポーズから少しだけ手首の位置を斜め前方上にズラすオフセット。肘をピンと伸ばすのを避けるために使う。身長に比例してスケールした値を用いる。
        private static readonly Vector3 APoseHandPosOffsetBase = new Vector3(0f, 0.01f, 0.03f);

        private static readonly Quaternion RightRot = Quaternion.Euler(0, 0, -APoseArmDownAngleDeg);
        private static readonly Quaternion LeftRot = Quaternion.Euler(0, 0, APoseArmDownAngleDeg);

        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKData LeftHand => _leftHand;
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKData RightHand => _rightHand;
        
        private readonly IKDataRecord _leftHandLocalFromHips = new IKDataRecord();
        public IIKData LeftHandLocalFromHips => _leftHandLocalFromHips;

        private readonly IVRMLoadable _vrmLoadable;
        private readonly ICoroutineSource _coroutineSource;

        private bool _hasModel = false;
        private Transform _hips;
        private Transform _leftUpperArm;
        private Transform _rightUpperArm;

        private float _rightArmLength = 0.4f;
        private float _leftArmLength = 0.4f;
        private Vector3 _rightPosHipsOffset;
        private Vector3 _leftPosHipsOffset;

        public HandDownIkCalculator(IVRMLoadable vrmLoadable, ICoroutineSource coroutineSource)
        {
            _vrmLoadable = vrmLoadable;
            _coroutineSource = coroutineSource;
            _leftHand.Rotation = LeftRot;
            _rightHand.Rotation = RightRot;
            _leftHandLocalFromHips.Rotation = LeftRot;
        }

        void IInitializable.Initialize()
        {
            _vrmLoadable.VrmLoaded += info => SetupAvatar(info.animator);
            _vrmLoadable.VrmDisposing += ClearAvatarReference;
            _coroutineSource.StartCoroutine(SetHandPositionsIfHasModel());
        }

        private void SetupAvatar(Animator animator)
        {
            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            _rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);

            var rightUpperArmPos = _rightUpperArm.position;
            var rightWristPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            var leftUpperArmPos = _leftUpperArm.position;
            var leftWristPos = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            var hipsPos = _hips.position;

            _rightArmLength = Vector3.Distance(rightWristPos, rightUpperArmPos);
            var rArmLengthFactor = Mathf.Clamp(_rightArmLength / ReferenceArmLength, 0.1f, 5f);

            _rightPosHipsOffset =
                rightUpperArmPos +
                Quaternion.AngleAxis(-APoseArmDownAngleDeg, Vector3.forward) * (rightWristPos - rightUpperArmPos) -
                hipsPos +
                rArmLengthFactor * APoseHandPosOffsetBase;

            _leftArmLength = Vector3.Distance(leftWristPos, leftUpperArmPos);
            var lArmLengthFactor = Mathf.Clamp(_leftArmLength / ReferenceArmLength, 0.1f, 5f);

            _leftPosHipsOffset =
                leftUpperArmPos +
                Quaternion.AngleAxis(APoseArmDownAngleDeg, Vector3.forward) * (leftWristPos - leftUpperArmPos) -
                hipsPos +
                lArmLengthFactor * APoseHandPosOffsetBase;

            _leftHand.Position = hipsPos + _leftPosHipsOffset;
            _rightHand.Position = hipsPos + _rightPosHipsOffset;

            _leftHandLocalFromHips.Position = _hips.InverseTransformPoint(_leftHand.Position);
            _leftHandLocalFromHips.Rotation = Quaternion.Inverse(_hips.rotation) * _leftHand.Rotation;

            _hasModel = true;
        }

        private void ClearAvatarReference()
        {
            _hasModel = false;
            _hips = null;
            _leftUpperArm = null;
            _rightUpperArm = null;
        }

        private IEnumerator SetHandPositionsIfHasModel()
        {
            var eof = new WaitForEndOfFrame();
            while (true)
            {
                yield return eof;
                if (!_hasModel)
                {
                    continue;
                }

                //やること: LateUpdateの時点で手の位置を合わせる。
                //フレーム終わりじゃないと調整されたあとのボーン位置が拾えないので、このタイミングでわざわざやってます
                
                var hipsPos = _hips.position;

                var leftUpperArmPos = _leftUpperArm.position;
                var leftPos = hipsPos + _leftPosHipsOffset;

                var leftTargetLength = _leftArmLength * ArmRelaxFactor;
                //UpperArmとWristの距離が一定になるようY軸の調整をするとこういう式になる
                leftPos.y = leftUpperArmPos.y - MathUtil.Sqrt(
                    leftTargetLength * leftTargetLength -
                    (leftPos.x - leftUpperArmPos.x) * (leftPos.x - leftUpperArmPos.x) -
                    (leftPos.z - leftUpperArmPos.z) * (leftPos.z - leftUpperArmPos.z)
                );

                var rightUpperArmPos = _rightUpperArm.position;
                var rightPos = hipsPos + _rightPosHipsOffset;

                var rightTargetLength = _rightArmLength * ArmRelaxFactor;
                //UpperArmとWristの距離が一定になるようY軸の調整をするとこういう式になる
                rightPos.y = rightUpperArmPos.y - MathUtil.Sqrt(
                    rightTargetLength * rightTargetLength -
                    (rightPos.x - rightUpperArmPos.x) * (rightPos.x - rightUpperArmPos.x) -
                    (rightPos.z - rightUpperArmPos.z) * (rightPos.z - rightUpperArmPos.z)
                );

                _leftHand.Position = leftPos;
                _rightHand.Position = rightPos;
            }
        }
    }
}
