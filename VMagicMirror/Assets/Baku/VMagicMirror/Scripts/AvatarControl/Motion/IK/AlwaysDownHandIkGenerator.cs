using System;
using System.Collections;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary>常に手を下げた姿勢になるような手IKの生成処理。</summary>
    public sealed class AlwaysDownHandIkGenerator : HandIkGeneratorBase
    {
        // ハンドトラッキングがロスしたときにAポーズへ落とし込むときの、腕の下げ角度(手首の曲げもコレに準拠します
        private const float APoseArmDownAngleDeg = 70f;
        // Aポーズから少しだけ手首の位置を斜め前方上にズラすオフセット。肘をピンと伸ばすのを避けるために使う。身長に比例してスケールした値を用いる。
        private readonly Vector3 APoseHandPosOffsetBase = new Vector3(0f, 0.01f, 0.03f);
        // リファレンスモデル(Megumi Baxterさん)のUpperArmボーンからWristボーンまでの距離
        private const float ReferenceArmLength = 0.37f;

        // 腕のピンと張る度合いがこの値になるように手IKのy座標を調整する
        private const float ArmRelaxFactor = 0.99f;

        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKData LeftHand => _leftHand;
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKData RightHand => _rightHand;

        private bool _hasModel = false;

        private Transform _hips;
        private Transform _leftUpperArm;
        private Transform _rightUpperArm;

        private float _rightArmLength = 0.4f;
        private float _leftArmLength = 0.4f;
        private Vector3 _rightPosHipsOffset;
        private Vector3 _leftPosHipsOffset;
        private readonly Quaternion RightRot = Quaternion.Euler(0, 0, -APoseArmDownAngleDeg);
        private readonly Quaternion LeftRot = Quaternion.Euler(0, 0, APoseArmDownAngleDeg);

        private readonly AlwaysHandDownState _leftHandState;
        private readonly AlwaysHandDownState _rightHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        public override IHandIkState RightHandState => _rightHandState; 
        
        public AlwaysDownHandIkGenerator(HandIkGeneratorDependency dependency, IVRMLoadable vrmLoadable)
            : base(dependency)
        {
            _leftHand.Rotation = LeftRot;
            _rightHand.Rotation = RightRot;

            _leftHandState = new AlwaysHandDownState(this, ReactedHand.Left);
            _rightHandState = new AlwaysHandDownState(this, ReactedHand.Right);

            vrmLoadable.VrmLoaded += info =>
            {
                var animator = info.controlRig;

                _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                _rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                _leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                
                var rightUpperArmPos = _rightUpperArm.position;
                var rightWristPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                var leftUpperArmPos = _leftUpperArm.position;
                var leftWristPos = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                var hipsPos = _hips.position;
                
                _rightArmLength = Vector3.Distance(rightWristPos, rightUpperArmPos);
                float rArmLengthFactor = Mathf.Clamp(_rightArmLength / ReferenceArmLength, 0.1f, 5f);
                
                _rightPosHipsOffset =
                    rightUpperArmPos +
                    Quaternion.AngleAxis(-APoseArmDownAngleDeg, Vector3.forward) * (rightWristPos - rightUpperArmPos) -
                    hipsPos + 
                    rArmLengthFactor * APoseHandPosOffsetBase;

                _leftArmLength = Vector3.Distance(leftWristPos, leftUpperArmPos);
                float lArmLengthFactor = Mathf.Clamp(_leftArmLength / ReferenceArmLength, 0.1f, 5f);
                
                _leftPosHipsOffset =
                    leftUpperArmPos + 
                    Quaternion.AngleAxis(APoseArmDownAngleDeg, Vector3.forward) * (leftWristPos - leftUpperArmPos) -
                    hipsPos + 
                    lArmLengthFactor * APoseHandPosOffsetBase;

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

            dependency.Config.IsAlwaysHandDown
                .Subscribe(v =>
                {
                    if (v)
                    {
                        _leftHandState.RaiseRequest();
                        _rightHandState.RaiseRequest();
                    }
                })
                .AddTo(dependency.Component);

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
                
                var hipsPos = _hips.position;

                var leftUpperArmPos = _leftUpperArm.position;
                var leftPos = hipsPos + _leftPosHipsOffset;

                var leftTargetLength = _leftArmLength * ArmRelaxFactor;
                //UpperArmとWristの距離が一定になるようY軸の調整をするとこういう式になる
                leftPos.y = leftUpperArmPos.y - Mathf.Sqrt(
                    leftTargetLength * leftTargetLength -
                    (leftPos.x - leftUpperArmPos.x) * (leftPos.x - leftUpperArmPos.x) -
                    (leftPos.z - leftUpperArmPos.z) * (leftPos.z - leftUpperArmPos.z)
                );

                var rightUpperArmPos = _rightUpperArm.position;
                var rightPos = hipsPos + _rightPosHipsOffset;

                var rightTargetLength = _rightArmLength * ArmRelaxFactor;
                //UpperArmとWristの距離が一定になるようY軸の調整をするとこういう式になる
                rightPos.y = rightUpperArmPos.y - Mathf.Sqrt(
                    rightTargetLength * rightTargetLength -
                    (rightPos.x - rightUpperArmPos.x) * (rightPos.x - rightUpperArmPos.x) -
                    (rightPos.z - rightUpperArmPos.z) * (rightPos.z - rightUpperArmPos.z)
                );

                _leftHand.Position = leftPos;
                _rightHand.Position = rightPos;
            }
        }

        private class AlwaysHandDownState : IHandIkState
        {
            public AlwaysHandDownState(AlwaysDownHandIkGenerator parent, ReactedHand hand)
            {
                _parent = parent;
                Hand = hand;
                _data = hand == ReactedHand.Right ? _parent._rightHand : _parent._leftHand;
            }

            private readonly AlwaysDownHandIkGenerator _parent;
            private readonly IIKData _data;

            public bool SkipEnterIkBlend => false;
            public void RaiseRequest() => RequestToUse?.Invoke(this);

            public Vector3 Position => _data.Position;
            public Quaternion Rotation => _data.Rotation;
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.AlwaysDown;
            
            public event Action<IHandIkState> RequestToUse;

            public void Enter(IHandIkState prevState)
            {
            }

            public void Quit(IHandIkState nextState)
            {
            }
        }
    }
}
