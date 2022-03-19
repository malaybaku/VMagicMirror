using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 顔の姿勢に追従して体の角度が変わる、という処理をやるクラス。 </summary>
    /// <remarks>
    /// 実装経緯として、もともとYaw, Pitch, Rollに対して別々のMonoBehaviourが居たのを、コイツ1人が統合クラスとしてまとめています
    /// </remarks>
    public class FaceAngleToBodyAngle : MonoBehaviour
    {
        private readonly FaceYawToBodyYaw _yaw = new FaceYawToBodyYaw();
        private readonly FacePitchToBodyPitch _pitch = new FacePitchToBodyPitch();
        private readonly FaceRollToBodyRoll _roll = new FaceRollToBodyRoll();

        private Transform _head = null;
        private Transform _neck = null;
        private bool _hasNeck = false;
        private bool _hasModel = false;

        /// <summary> オイラー角ごとの計算で得た体の推奨傾き角度を取得します。 </summary>
        public Quaternion BodyLeanSuggest { get; private set; } = Quaternion.identity;

        private void Start()
        {
            StartCoroutine(CheckAnglesOnEndOfFrame());
        }

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                SetZeroTarget();
                _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
                _neck = info.animator.GetBoneTransform(HumanBodyBones.Neck);
                _hasNeck = _neck != null;
                _hasModel = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hasNeck = false;
                _head = null;
                _neck = null;
                SetZeroTarget();
            };
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableTwistBodyMotion,
                c =>
                {
                    var enableTwistBodyMotion = c.ToBoolean();
                    _roll.EnableTwistMotion = enableTwistBodyMotion;
                    _yaw.EnableTwistMotion = enableTwistBodyMotion;
                });
        }

        private void Update()
        {
            _yaw.UpdateSuggestAngle();
            _pitch.UpdateSuggestAngle();
            _roll.UpdateSuggestAngle();

            BodyLeanSuggest = Quaternion.Euler(
                _pitch.PitchAngleDegree,
                _yaw.YawAngleDegree,
                _roll.RollAngleDegree
                );
        }

        private IEnumerator CheckAnglesOnEndOfFrame()
        {
            var eof = new WaitForEndOfFrame();
            while (true)
            {
                yield return eof;
                if (!_hasModel)
                {
                    SetZeroTarget();
                    continue;
                }

                var headRotation = _hasNeck
                    ? _neck.localRotation * _head.localRotation
                    : _head.localRotation;

                _roll.CheckAngle(headRotation);
                _yaw.CheckAngle(headRotation);
                _pitch.CheckAngle(headRotation);
            }
        }

        private void SetZeroTarget()
        {
            _yaw.SetZeroTarget();
            _pitch.SetZeroTarget();
            _roll.SetZeroTarget();            
        }
    }
}
