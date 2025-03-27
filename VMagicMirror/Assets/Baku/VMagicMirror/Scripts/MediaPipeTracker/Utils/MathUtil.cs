using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public static class MathUtil
    {
        // NOTE: Hipsなど、そもそも左右がないものは登録しないでOK
        private static readonly Dictionary<HumanBodyBones, HumanBodyBones> MirroredBones = new()
        {
            [HumanBodyBones.LeftUpperLeg] = HumanBodyBones.RightUpperLeg,
            [HumanBodyBones.LeftLowerLeg] = HumanBodyBones.RightLowerLeg,
            [HumanBodyBones.LeftFoot] = HumanBodyBones.RightFoot,
            [HumanBodyBones.LeftToes] = HumanBodyBones.RightToes,
            [HumanBodyBones.LeftShoulder] = HumanBodyBones.RightShoulder,
            [HumanBodyBones.LeftUpperArm] = HumanBodyBones.RightUpperArm,
            [HumanBodyBones.LeftLowerArm] = HumanBodyBones.RightLowerArm,
            [HumanBodyBones.LeftHand] = HumanBodyBones.RightHand,
            [HumanBodyBones.LeftThumbProximal] = HumanBodyBones.RightThumbProximal,
            [HumanBodyBones.LeftThumbIntermediate] = HumanBodyBones.RightThumbIntermediate,
            [HumanBodyBones.LeftThumbDistal] = HumanBodyBones.RightThumbDistal,
            [HumanBodyBones.LeftIndexProximal] = HumanBodyBones.RightIndexProximal,
            [HumanBodyBones.LeftIndexIntermediate] = HumanBodyBones.RightIndexIntermediate,
            [HumanBodyBones.LeftIndexDistal] = HumanBodyBones.RightIndexDistal,
            [HumanBodyBones.LeftMiddleProximal] = HumanBodyBones.RightMiddleProximal,
            [HumanBodyBones.LeftMiddleIntermediate] = HumanBodyBones.RightMiddleIntermediate,
            [HumanBodyBones.LeftMiddleDistal] = HumanBodyBones.RightMiddleDistal,
            [HumanBodyBones.LeftRingProximal] = HumanBodyBones.RightRingProximal,
            [HumanBodyBones.LeftRingIntermediate] = HumanBodyBones.RightRingIntermediate,
            [HumanBodyBones.LeftRingDistal] = HumanBodyBones.RightRingDistal,
            [HumanBodyBones.LeftLittleProximal] = HumanBodyBones.RightLittleProximal,
            [HumanBodyBones.LeftLittleIntermediate] = HumanBodyBones.RightLittleIntermediate,
            [HumanBodyBones.LeftLittleDistal] = HumanBodyBones.RightLittleDistal,

            [HumanBodyBones.RightUpperLeg] = HumanBodyBones.LeftUpperLeg,
            [HumanBodyBones.RightLowerLeg] = HumanBodyBones.LeftLowerLeg,
            [HumanBodyBones.RightFoot] = HumanBodyBones.LeftFoot,
            [HumanBodyBones.RightToes] = HumanBodyBones.LeftToes,
            [HumanBodyBones.RightShoulder] = HumanBodyBones.LeftShoulder,
            [HumanBodyBones.RightUpperArm] = HumanBodyBones.LeftUpperArm,
            [HumanBodyBones.RightLowerArm] = HumanBodyBones.LeftLowerArm,
            [HumanBodyBones.RightHand] = HumanBodyBones.LeftHand,
            [HumanBodyBones.RightThumbProximal] = HumanBodyBones.LeftThumbProximal,
            [HumanBodyBones.RightThumbIntermediate] = HumanBodyBones.LeftThumbIntermediate,
            [HumanBodyBones.RightThumbDistal] = HumanBodyBones.LeftThumbDistal,
            [HumanBodyBones.RightIndexProximal] = HumanBodyBones.LeftIndexProximal,
            [HumanBodyBones.RightIndexIntermediate] = HumanBodyBones.LeftIndexIntermediate,
            [HumanBodyBones.RightIndexDistal] = HumanBodyBones.LeftIndexDistal,
            [HumanBodyBones.RightMiddleProximal] = HumanBodyBones.LeftMiddleProximal,
            [HumanBodyBones.RightMiddleIntermediate] = HumanBodyBones.LeftMiddleIntermediate,
            [HumanBodyBones.RightMiddleDistal] = HumanBodyBones.LeftMiddleDistal,
            [HumanBodyBones.RightRingProximal] = HumanBodyBones.LeftRingProximal,
            [HumanBodyBones.RightRingIntermediate] = HumanBodyBones.LeftRingIntermediate,
            [HumanBodyBones.RightRingDistal] = HumanBodyBones.LeftRingDistal,
            [HumanBodyBones.RightLittleProximal] = HumanBodyBones.LeftLittleProximal,
            [HumanBodyBones.RightLittleIntermediate] = HumanBodyBones.LeftLittleIntermediate,
            [HumanBodyBones.RightLittleDistal] = HumanBodyBones.LeftLittleDistal,
        };
        
        /// <summary>
        /// 始点、終点、始点の接線ベクトルの方向と[0,1]のパラメータを指定することで、
        /// 中点からみて面対称になっているようなベジェ曲線上の点を取得します。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="startTangent"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 GetCubicBezierWithStartTangent(
            Vector3 start, Vector3 end, Vector3 startTangent, float t
            )
        {
            // 制御点が start と end から等距離になるようにkの位置を決めている
            var halfDiff = 0.5f * (end - start);

            var dot = Vector3.Dot(startTangent, halfDiff);
            // うまく制御点が決まらない場合、直線にしてしまう
            if (dot < 0.00001f)
            {
                return Vector3.Lerp(start, end, t);
            }

            var k = halfDiff.sqrMagnitude / dot;
            var c = start + startTangent * k;

            return (1 - t) * (1 - t) * start +
                 2 * (1 - t) * t * c +
                 t * t * end;
        }

        // 左右反転系のやつ
        public static Vector2 Mirror(Vector2 v) => new Vector2(-v.x, v.y);
        public static Vector3 Mirror(Vector3 v) => new Vector3(-v.x, v.y, v.z);
        public static Quaternion Mirror(Quaternion q) => new Quaternion(q.x, -q.y, -q.z, q.w);
        public static Pose Mirror(Pose p) => new Pose(Mirror(p.position), Mirror(p.rotation));
        
        public static HumanBodyBones Mirror(HumanBodyBones bone) => MirroredBones.GetValueOrDefault(bone, bone);
        

    }
}
