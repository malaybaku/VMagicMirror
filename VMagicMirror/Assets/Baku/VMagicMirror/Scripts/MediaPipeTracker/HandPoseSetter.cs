using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Tasks.Components.Containers;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public readonly struct WristPose
    {
        public WristPose(Vector3 position, Vector3 xAxis, Vector3 yAxis, Vector3 zAxis)
        {
            Position = position;
            XAxis = xAxis;
            YAxis = yAxis;
            ZAxis = zAxis;
        }

        /// <summary> 手首の位置 </summary>
        public Vector3 Position { get; }
 
        /// <summary> 手首から小指方向に伸びる、手のひらの平面上のベクトル </summary>
        public Vector3 XAxis { get; }
        
        /// <summary> 手首から中指方向に伸びる、手のひらの平面上のベクトル </summary>
        public Vector3 YAxis { get; }
        
        /// <summary> 手の甲から手のひらに向かうような、手のひらと垂直なベクトル </summary>
        public Vector3 ZAxis { get; }
    }

    /// <summary>
    /// Mediapipeで取得した手のLandmarkを使ってFK/IKを指定するやつ
    /// </summary>
    public class HandPoseSetter
    {
        public const int HandLandmarkCount = 21;

        private readonly KinematicSetter _kinematicSetter;

        // 指の回転をまとめて計算したときのキャッシュに使う (使わないでも書けるが、煩雑になるので…)
        
        // 要素はHumanBodyBones.(Left|Right)ThumbnProximal から順に入る
        private readonly Quaternion[] _rotationCache = new Quaternion[15];

        public HandPoseSetter(KinematicSetter forwardKinematicSetter)
        {
            _kinematicSetter = forwardKinematicSetter;
        }

        public bool LeftHandPoseHasValidValue { get; private set; }
        public bool RightHandPoseHasValidValue { get; private set; }

        public Quaternion LeftHandRotation { get; private set; } = Quaternion.identity;
        public Quaternion RightHandRotation { get; private set; } = Quaternion.identity;

        public void SetLeftHandPose(Landmarks worldLandmarks) => SetHandPose(worldLandmarks, true);
        public void SetRightHandPose(Landmarks worldLandmarks) => SetHandPose(worldLandmarks, false);

        private void SetHandPose(Landmarks worldLandmarks, bool isLeftHand)
        {
            // Holisticではここのガードに引っかかることがある
            var landmarks = worldLandmarks.landmarks;
            if (landmarks is not { Count: HandLandmarkCount })
            {
                return;
            }

            // 0の位置 = Wristの位置
            // 0, 5, 17 (手首、人差し指の付け根、小指の付け根) -> 手のひら平面を3点から特定することで、Wristのrotを定める
            // 0,1,2,3,4 -> 第1~第3関節の曲げがわかる。0,5,6,7,8 とかも同様
            // - いったん全部の曲げ方向が共通ということにする
            var wrist = landmarks[0].ToLocalPosition();
            var indexBase = landmarks[5].ToLocalPosition();
            var littleBase = landmarks[17].ToLocalPosition();

            // zAxis = 手首から手のひら方向に伸びていく、手のひらと垂直なベクトル
            // yAxis = 手首から中指方向に伸びる、手のひらの平面上のベクトル
            // z,yがそれぞれ　Vector3.forward, Vector3.up　になったとき「トラッキングしやすい正面向きの手」になるような基準にしてる

            var zAxis = Vector3.Cross(
                indexBase - wrist,
                littleBase - wrist
            ).normalized;
            if (!isLeftHand)
            {
                // 右手では外積の順序をひっくり返す必要がある = 結果が逆
                zAxis = -zAxis;
            }

            var middleBase = landmarks[9].ToLocalPosition();
            var yAxis = (middleBase - wrist).normalized;
            var xAxis = Vector3.Cross(yAxis, zAxis);
            var wristPose = new WristPose(wrist, xAxis, yAxis, zAxis);

            // NOTE: VRMの手のワールド姿勢ではなく、 KinematicSetter.SetLeftHandPose で必要な回転にしている。
            var handRotation = Quaternion.LookRotation(zAxis, yAxis);
            var fingerRotations = GetFingerRotations(wristPose, landmarks, isLeftHand);

            var startBone = isLeftHand
                ? (int)HumanBodyBones.LeftThumbProximal
                : (int)HumanBodyBones.RightThumbProximal;

            for (var i = 0; i < 15; i++)
            {
                var fingerBone = (HumanBodyBones)(i + startBone);
                _kinematicSetter.SetLocalRotationBeforeIK(fingerBone, fingerRotations[i]);
            }

            if (isLeftHand)
            {
                LeftHandRotation = handRotation;
                LeftHandPoseHasValidValue = true;
            }
            else
            {
                RightHandRotation = handRotation;
                RightHandPoseHasValidValue = true;
            }
        }

        private Quaternion[] GetFingerRotations(WristPose wristPose, List<Landmark> landmarks, bool isLeft)
        {
            for (var i = 0; i < 5; i++)
            {
                var j3 = landmarks[4 * i + 1].ToLocalPosition();
                var j2 = landmarks[4 * i + 2].ToLocalPosition();
                var j1 = landmarks[4 * i + 3].ToLocalPosition();
                var tips = landmarks[4 * i + 4].ToLocalPosition();

                var (proximal, intermediate, distal) = GetFingerBendAngle(
                    wristPose, j3, j2, j1, tips
                );

                // 親指だけ曲げ角度を特別に再計算する
                if (i == 0)
                {
                    proximal = GetThumbProximalBendAngle(wristPose, j3, j2);
                }

                //NOTE: 指がある程度ピンとしてる場合のみ、パー/チョップの検出のために追加で計算する
                var openAngle = 0f;
                if (proximal + intermediate + distal < 120f)
                {
                    // 第3-第2関節のベクトルを手のひら平面に射影する
                    var j3Horizontal = GetNormalizedVectorOnPlane(j2 - j3, wristPose.ZAxis);
                    // 中指とのなす角度の Sin を符号付きで計算
                    var cross = Vector3.Cross(j3Horizontal, wristPose.YAxis);
                    var sign = Mathf.Sign(Vector3.Dot(cross, wristPose.ZAxis));
                    // NOTE: この角度が案外デカいことがあるので注意！
                    openAngle = Mathf.Asin(sign * cross.magnitude) * Mathf.Rad2Deg;
                }

                // NOTE: clampの角度 = パーの手のときにその指と中指でつくる角度の限度。
                // - 親指は何ならもっと動くかも
                // - 指ごとに丁寧にプラマイを絞っていくのも有効
                openAngle = Mathf.Clamp(openAngle, -30f, 30f);

                if (i == 0)
                {
                    _rotationCache[3 * i] = GetThumbRot(proximal, openAngle, isLeft);
                    _rotationCache[3 * i + 1] = GetThumbRot(intermediate, 0f, isLeft);
                    _rotationCache[3 * i + 2] = GetThumbRot(distal, 0f, isLeft);
                }
                else
                {
                    _rotationCache[3 * i] = GetRot(proximal, openAngle, isLeft);
                    _rotationCache[3 * i + 1] = GetRot(intermediate, 0f, isLeft);
                    _rotationCache[3 * i + 2] = GetRot(distal, 0f, isLeft);
                }
            }

            return _rotationCache;
        }

        // NOTE: Thumbでもそれ以外でも、openAngleは正負まで呼び出し元でケアするのが期待値。
        // 例: 左手の場合、パーの姿勢を取ると人差し指はプラス、小指はマイナスのopenAngleが渡ってくるのが期待値。右手はその逆になる。

        private static Quaternion GetThumbRot(float bendAngle, float openAngle, bool isLeftHand)
        {
            // TODO: ほんとはTポーズ時点での状態も踏まえて計算したい
            // 「Tポーズ時点で指がちょっと開いてる」みたいな状態も考慮したいので
            var yawFactor = isLeftHand ? -1 : 1f;
            return Quaternion.Euler(0, bendAngle * yawFactor, 0);
        }

        private static Quaternion GetRot(float bendAngle, float openAngle, bool isLeftHand)
        {
            var rollFactor = isLeftHand ? 1 : -1f;
            return Quaternion.Euler(0, openAngle, bendAngle * rollFactor);
        }
        
        // TEMP: 親指は回転の計算がムズいので、ぜんぶ曲げ角度として取る
        private static float GetThumbProximalBendAngle(WristPose wristPose, Vector3 j3, Vector3 j2) 
            => GetBendAngle(wristPose.Position, j3, j2);

        // 指の付け根側から「手首、第(3|2|1)関節、指先」の位置を渡すことで、第(3|2|1)関節の折り曲げ角度を返す。
        // この関数はチョップとかパーの方向の回転は返さない
        private static (float, float, float) GetFingerBendAngle(
            WristPose wristPose, Vector3 j3, Vector3 j2, Vector3 j1, Vector3 tips)
        {
            // 第3関節の回転は指を折り曲げる方向とパーに開く方向の2軸があるため、
            // 手のひらと垂直な成分だけ抜き出すことで折り曲げ方向を見ている
            var proximalAngle =
                Mathf.Asin(Vector3.Dot((j2 - j3).normalized, wristPose.ZAxis)) * Mathf.Rad2Deg;
            
            return (
                Mathf.Abs(proximalAngle),
                GetBendAngle(j3, j2, j1),
                GetBendAngle(j2, j1, tips)
            );
        }

        private static float GetBendAngle(Vector3 v1, Vector3 v2, Vector3 v3) 
            => Vector3.Angle(v2 - v1, v3 - v2);

        private static Vector3 GetNormalizedVectorOnPlane(Vector3 v, Vector3 normal) 
            => (v - normal * Vector3.Dot(v, normal)).normalized;
    }
}
