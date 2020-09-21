using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>常に手を下げた姿勢になるような手IKの生成処理。</summary>
    public sealed class AlwaysDownHandIkGenerator : HandIkGeneratorBase
    {
        // ハンドトラッキングがロスしたときにAポーズへ落とし込むときの、腕の下げ角度(手首の曲げもコレに準拠します
        private const float APoseArmDownAngleDeg = 70f;
        // Aポーズから少しだけ手首の位置を斜め前方上にズラすオフセット
        private readonly Vector3 APoseHandPosOffset = new Vector3(0f, 0.02f, 0.02f);

        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        private bool _hasModel = false;

        private Transform _hips;
        private Transform _leftUpperArm;
        private Transform _rightUpperArm;
        //NOTE: 
        private Vector3 _rightPosHipsOffset;
        private Vector3 _leftPosHipsOffset;
        private readonly Quaternion RightRot = Quaternion.Euler(0, 0, -APoseArmDownAngleDeg);
        private readonly Quaternion LeftRot = Quaternion.Euler(0, 0, APoseArmDownAngleDeg);
        
        public AlwaysDownHandIkGenerator(MonoBehaviour coroutineResponder, IVRMLoadable vrmLoadable)
            : base(coroutineResponder)
        {
            _leftHand.Rotation = LeftRot;
            _rightHand.Rotation = RightRot;

            vrmLoadable.VrmLoaded += info =>
            {
                var animator = info.animator;

                _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                _rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                _leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                
                var rightUpperArmPos = _rightUpperArm.position;
                var rightWristPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                var leftUpperArmPos = _leftUpperArm.position;
                var leftWristPos = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                var hipsPos = _hips.position;
                
                _rightPosHipsOffset =
                    _rightUpperArm.position + 
                    Quaternion.AngleAxis(-APoseArmDownAngleDeg, Vector3.forward) * (rightWristPos - rightUpperArmPos) -
                    hipsPos + 
                    APoseHandPosOffset;
                
                _leftPosHipsOffset =
                    _leftUpperArm.position + 
                    Quaternion.AngleAxis(APoseArmDownAngleDeg, Vector3.forward) * (leftWristPos - leftUpperArmPos) -
                    hipsPos + 
                    APoseHandPosOffset;

                _leftHand.Position = hipsPos + _leftPosHipsOffset;
                _rightHand.Position = hipsPos + _rightPosHipsOffset;
                _hasModel = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hips = null;
                _leftUpperArm = null;
                _rightUpperArm = null;
            };

            StartCoroutine(SetHandPositionsIfHasModel());
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

                
                //肩の上げ下げに沿って手を上下に動かす。
                var armDiffAdjust =
                    Vector3.up * 
                    ((_leftUpperArm.position - _rightUpperArm.position).y);
                
                var hipsPos = _hips.position;
                _leftHand.Position = hipsPos + _leftPosHipsOffset + armDiffAdjust;
                _rightHand.Position = hipsPos + _rightPosHipsOffset - armDiffAdjust;
            }
        }
    }
}
