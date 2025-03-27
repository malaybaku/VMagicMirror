using System.Collections.Generic;
using Baxter;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // NOTE: このコードはVMMでは再利用しないつもり。
    // VMMではVRM 1.0を前提にするし、VMMの表情の適用コードは強めにラップされているのが理由。
    public class FaceResultSetter
    {
        private const string LeftEyeLeftKey = "eyeLookOutLeft";
        private const string LeftEyeRightKey = "eyeLookInLeft";
        private const string LeftEyeDownKey = "eyeLookDownLeft";
        private const string LeftEyeUpKey = "eyeLookUpLeft";
        private const string RightEyeLeftKey = "eyeLookInRight";
        private const string RightEyeRightKey = "eyeLookOutRight";
        private const string RightEyeDownKey = "eyeLookDownRight";
        private const string RightEyeUpKey = "eyeLookUpRight";

        private readonly KinematicSetter _kinematicSetter;
        private readonly FacialSetter _facialSetter;
        
        private readonly HashSet<string> _latestBlendShapeNames = new();

        /// <summary>
        /// eyeLook(Left|Right|Up|Down) 系のBlendShape値が1になったときに眼球ボーンを回転させる量(deg)
        /// ちびキャラBaxterちゃんだと30くらいが適正値
        /// </summary>
        public float EyeLookAtValueToAngle { get; set; } = 30f;
        
        public FaceResultSetter(KinematicSetter kinematicSetter, FacialSetter facialSetter)
        {
            _kinematicSetter = kinematicSetter;
            _facialSetter = facialSetter;
        }

        public void SetPerfectSyncBlendShapes(Dictionary<string, float> values)
        {
            _latestBlendShapeNames.Clear();
            _latestBlendShapeNames.UnionWith(values.Keys);
            
            _facialSetter.SetValues(values);
            //_facialSetter.SetValues(values.Where(pair => !IsEyeLookBlendShape(pair.Key)));
         
            // 目はブレンドシェイプじゃなくてボーンに利かす

            // NOTE: Yで下向きを正にするのは下向き回転が正だから
            var leftEyeYaw = Mathf.Clamp(values[LeftEyeRightKey] - values[LeftEyeLeftKey], -1, 1);
            var leftEyePitch = Mathf.Clamp(values[LeftEyeDownKey] - values[LeftEyeUpKey], -1, 1);
            var leftEyeLocalRotation = Quaternion.Euler(
                leftEyePitch * EyeLookAtValueToAngle,
                leftEyeYaw * EyeLookAtValueToAngle,
                0f
            );
            //_kinematicSetter.SetLocalRotationBeforeIK(HumanBodyBones.LeftEye, leftEyeLocalRotation);
            
            var rightEyeYaw = Mathf.Clamp(values[RightEyeRightKey] - values[RightEyeLeftKey], -1, 1);
            var rightEyePitch = Mathf.Clamp(values[RightEyeDownKey] - values[RightEyeUpKey], -1, 1);
            var rightEyeLocalRotation = Quaternion.Euler(
                rightEyePitch * EyeLookAtValueToAngle,
                rightEyeYaw * EyeLookAtValueToAngle,
                0f
            );
            //_kinematicSetter.SetLocalRotationBeforeIK(HumanBodyBones.RightEye, rightEyeLocalRotation);
        }

        public void ClearBlendShapes() => _facialSetter.ResetValues();

        private static bool IsEyeLookBlendShape(string key) => key switch
        {
            LeftEyeLeftKey => true,
            LeftEyeRightKey => true,
            LeftEyeUpKey => true,
            LeftEyeDownKey => true,
            RightEyeLeftKey => true,
            RightEyeRightKey => true,
            RightEyeUpKey => true,
            RightEyeDownKey => true,
            _ => false,
        };
    }
}
