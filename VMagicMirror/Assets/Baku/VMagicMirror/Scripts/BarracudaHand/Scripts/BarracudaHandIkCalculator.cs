using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// Barracudaが吐いてきた手のポイント情報を手のIKに変換するやつ。
    /// トラッキングロスト時の計算も行う。
    /// </summary>
    public class BarracudaHandIkCalculator
    {
        public BarracudaHandIkCalculator(Vector3[] leftHandPoints, Vector3[] rightHandPoints)
        {
            _leftHandPoints = leftHandPoints;
            _rightHandPoints = rightHandPoints;
        }

        
        private readonly Vector3[] _leftHandPoints;
        private readonly Vector3[] _rightHandPoints;

        private bool _hasModel;
        private Transform _leftUpperArm;
        private Transform _rightUpperArm;
        private AlwaysDownHandIkGenerator _downHand;

        public void SetModel(Vrm10RuntimeControlRig controlRig, AlwaysDownHandIkGenerator downHand)
        {
            _leftUpperArm = controlRig.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _rightUpperArm = controlRig.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _downHand = downHand;
            _hasModel = true;
        }

        public void RemoveModel()
        {
            _hasModel = false;
            _downHand = null;
            _leftUpperArm = null;
            _rightUpperArm = null;
        }
        
        //左手の取るべきワールド回転に関連して、回転、手の正面方向ベクトル、手のひらと垂直なベクトルの3つを計算します。
        public RotationAndCoordVectors CalculateLeftHandRotation()
        {
            var wristForward = (_leftHandPoints[9] - _leftHandPoints[0]).normalized;
            //NOTE: 右手と逆の順にすることに注意
            var wristUp = Vector3.Cross(
                _leftHandPoints[17] - _leftHandPoints[0],
                wristForward
            ).normalized;

            var rot = Quaternion.LookRotation(wristForward, wristUp);
            return new RotationAndCoordVectors(rot, wristForward, wristUp);
        }
    
        //右手の取るべきワールド回転に関連して、回転、手の正面方向ベクトル、手のひらと垂直なベクトルの3つを計算します。
        public RotationAndCoordVectors CalculateRightHandRotation()
        {
            //正面 = 中指方向
            var wristForward = (_rightHandPoints[9] - _rightHandPoints[0]).normalized;
            //手首と垂直 = 人差し指あるいは中指方向、および小指で外積を取ると手の甲方向のベクトルが得られる
            var wristUp = Vector3.Cross(
                wristForward, 
                _rightHandPoints[17] - _rightHandPoints[0]
            ).normalized;

            //局所座標の余ってるベクトル = 右手の親指付け根から小指付け根方向のベクトル
            // var right = Vector3.Cross(up, forward)

            var rot = Quaternion.LookRotation(wristForward, wristUp);
            return new RotationAndCoordVectors(rot, wristForward, wristUp);
        }

        public Pose GetLostLeftHandPose(float rate)
        {
            if (!_hasModel)
            {
                //普通ここには来ない
                return new Pose(Vector3.zero, Quaternion.identity);
            }
            
            var upperArmPos = _leftUpperArm.position;
            var diff = _downHand.LeftHand.Position - upperArmPos;
            //z成分はそのままに、真横に手を置いたベクトルを作る
            var diffHorizontal = Vector3.left * new Vector2(diff.x, diff.y).magnitude;

            //NOTE: -70degくらいになるよう符号を変換 + [-180, 180]で範囲保証
            var angle = Mathf.Repeat(Mathf.Rad2Deg * Mathf.Atan2(diff.y, -diff.x) + 180f, 360f) - 180f;
            //適用時は+方向に曲げたいのでこんな感じ
            var resultPos = upperArmPos + Quaternion.AngleAxis(-angle * rate, Vector3.forward) * diffHorizontal;
            //NOTE: Slerpでも書けるが、こっちのほうが計算的にラクなはず
            var resultRot = Quaternion.AngleAxis(-angle * rate, Vector3.forward);

            return new Pose(resultPos, resultRot);
        }
        
        public Pose GetLostRightHandPose(float rate)
        {
            if (!_hasModel)
            {
                //普通ここには来ない
                return new Pose(Vector3.zero, Quaternion.identity);
            }

            var upperArmPos = _rightUpperArm.position;
            var diff = _downHand.RightHand.Position - upperArmPos;
            var diffHorizontal = Vector3.right * new Vector2(diff.x, diff.y).magnitude;

            //NOTE: -70degくらいのはず
            var angle = Mathf.Repeat(Mathf.Rad2Deg * Mathf.Atan2(diff.y, diff.x) + 180f, 360f) - 180f;
            var resultPos = upperArmPos + Quaternion.AngleAxis(angle * rate, Vector3.forward) * diffHorizontal;
            var resultRot = Quaternion.AngleAxis(angle * rate, Vector3.forward);
            
            return new Pose(resultPos, resultRot);
        }
    }

    public class RotationAndCoordVectors
    {

        public RotationAndCoordVectors(Quaternion rot, Vector3 forward, Vector3 up)
        {
            Rotation = rot;
            Forward = forward;
            Up = up;
        }
        
        //NOTE: 基準姿勢をこう取ってるのは左右のフリップ処理を考えるとき都合がいいから。
        /// <summary> 手首のワールド回転について、手を正面に突き出して手のひらが下に向いた姿勢を基準とした回転量 </summary>
        public Quaternion Rotation { get; }
        /// <summary> 手首から中指に向かうベクトル </summary>
        public Vector3 Forward { get; }
        /// <summary> 手のひらと垂直で手の甲に向かうベクトル </summary>
        public Vector3 Up { get; }
    }
}
