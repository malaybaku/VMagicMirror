using System.Collections.Generic;
using Mediapipe.Tasks.Components.Containers;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MediaPipeUnityAddon.HolisticFacePoseEstimator
{
    /// <summary>
    /// Holisticタスクの顔ランドマークに対し、FaceLandmarkの出力と同じような行列の姿勢情報を推定して出力できるクラス。
    /// </summary>
    /// <remarks>
    /// ポイント:
    /// - static ctorで記述されている基準点はMediaPipeのcanonical faceモデルのランドマークのうち、特に顔の向きの推定に寄与するもの
    /// - このクラスの関数呼び出しではGCAllocは発生しない
    /// - 計算のコアロジックはBurst化される。
    ///   - ただしMediaPipe本体の推論のほうが重たいので、最適化の恩恵はそこまで期待していない
    /// - 本家mediapipeとは姿勢の推定方法が異なる。本クラスではSVDを使わず、Hornの方法で重み付き最適回転を求める
    ///   - FaceLandmarkと同時に実行したときの推定結果がほぼ同じになるので、これでヨシとしている
    /// </remarks>
    public static class HolisticFacePoseEstimator
    {
        private const int RequiredLandmarkCount = 468;
        private const float VerticalFovDegrees = 63.0f;
        private const float NearPlane = 1.0f;
        private const float CompactnessThreshold = 1e-3f;
        private const float Epsilon = 1e-6f;
        private const int PowerIterationCount = 32;

        private static readonly FixedList4096Bytes<BasisPoint> _basis;
        private static readonly FixedList512Bytes<float> _sqrtWeights;
        private static readonly FixedList512Bytes<float3> _weightedSources;
        private static readonly FixedList512Bytes<float3> _centeredWeightedSources;
        private static readonly float _totalWeight;
        private static readonly float _scaleDenominator;

        static HolisticFacePoseEstimator()
        {
            var basis = new FixedList4096Bytes<BasisPoint>();
            basis.Add(new BasisPoint(4, 0.07090994f, new float3(0.0f, -0.46317f, 7.58658f)));
            basis.Add(new BasisPoint(6, 0.032100145f, new float3(0.0f, 2.473255f, 5.788627f)));
            basis.Add(new BasisPoint(10, 0.008446551f, new float3(0.0f, 8.261778f, 4.481535f)));
            basis.Add(new BasisPoint(33, 0.05872417f, new float3(-4.445859f, 2.663991f, 3.173422f)));
            basis.Add(new BasisPoint(54, 0.00766708f, new float3(-6.279331f, 6.615427f, 1.42585f)));
            basis.Add(new BasisPoint(67, 0.009078059f, new float3(-3.523964f, 8.005976f, 3.729163f)));
            basis.Add(new BasisPoint(117, 0.009791938f, new float3(-5.258659f, 0.945811f, 2.974312f)));
            basis.Add(new BasisPoint(119, 0.014565368f, new float3(-3.300681f, 0.861641f, 3.872784f)));
            basis.Add(new BasisPoint(121, 0.018591361f, new float3(-1.820731f, 1.467954f, 4.224124f)));
            basis.Add(new BasisPoint(127, 0.0051979944f, new float3(-7.743095f, 2.364999f, -2.005167f)));
            basis.Add(new BasisPoint(129, 0.120625205f, new float3(-1.785794f, -0.978284f, 4.85047f)));
            basis.Add(new BasisPoint(132, 0.0055600186f, new float3(-7.270895f, -2.890917f, -2.252455f)));
            basis.Add(new BasisPoint(133, 0.053286184f, new float3(-1.856432f, 2.585245f, 3.757904f)));
            basis.Add(new BasisPoint(136, 0.066890456f, new float3(-5.085276f, -7.17859f, 0.714711f)));
            basis.Add(new BasisPoint(143, 0.014816548f, new float3(-6.407571f, 2.236021f, 1.560843f)));
            basis.Add(new BasisPoint(147, 0.014262834f, new float3(-6.234883f, -1.94443f, 1.663542f)));
            basis.Add(new BasisPoint(198, 0.025462192f, new float3(-1.246815f, 0.230297f, 5.681036f)));
            basis.Add(new BasisPoint(205, 0.04725228f, new float3(-3.832928f, -1.537326f, 4.137731f)));
            basis.Add(new BasisPoint(263, 0.05872417f, new float3(4.445859f, 2.663991f, 3.173422f)));
            basis.Add(new BasisPoint(284, 0.00766708f, new float3(6.279331f, 6.615427f, 1.42585f)));
            basis.Add(new BasisPoint(297, 0.009078059f, new float3(3.523964f, 8.005976f, 3.729163f)));
            basis.Add(new BasisPoint(346, 0.009791938f, new float3(5.258659f, 0.945811f, 2.974312f)));
            basis.Add(new BasisPoint(348, 0.014565368f, new float3(3.300681f, 0.861641f, 3.872784f)));
            basis.Add(new BasisPoint(350, 0.018591361f, new float3(1.820731f, 1.467954f, 4.224124f)));
            basis.Add(new BasisPoint(356, 0.0051979944f, new float3(7.743095f, 2.364999f, -2.005167f)));
            basis.Add(new BasisPoint(358, 0.120625205f, new float3(1.785794f, -0.978284f, 4.85047f)));
            basis.Add(new BasisPoint(361, 0.0055600186f, new float3(7.270895f, -2.890917f, -2.252455f)));
            basis.Add(new BasisPoint(362, 0.053286184f, new float3(1.856432f, 2.585245f, 3.757904f)));
            basis.Add(new BasisPoint(365, 0.066890456f, new float3(5.085276f, -7.17859f, 0.714711f)));
            basis.Add(new BasisPoint(372, 0.014816548f, new float3(6.407571f, 2.236021f, 1.560843f)));
            basis.Add(new BasisPoint(376, 0.014262834f, new float3(6.234883f, -1.94443f, 1.663542f)));
            basis.Add(new BasisPoint(420, 0.025462192f, new float3(1.246815f, 0.230297f, 5.681036f)));
            basis.Add(new BasisPoint(425, 0.04725228f, new float3(3.832928f, -1.537326f, 4.137731f)));
            _basis = basis;

            var sqrtWeights = new FixedList512Bytes<float>();
            var weightedSources = new FixedList512Bytes<float3>();
            var centeredWeightedSources = new FixedList512Bytes<float3>();
            var weightedSourceSum = float3.zero;
            var totalWeight = 0.0f;

            for (var i = 0; i < _basis.Length; i++)
            {
                var sqrtWeight = math.sqrt(_basis[i].Weight);
                var weightedSource = _basis[i].Position * sqrtWeight;
                sqrtWeights.Add(sqrtWeight);
                weightedSources.Add(weightedSource);
                totalWeight += _basis[i].Weight;
                weightedSourceSum += weightedSource * sqrtWeight;
            }

            var sourceCenterOfMass = weightedSourceSum / totalWeight;
            var scaleDenominator = 0.0f;
            for (var i = 0; i < _basis.Length; i++)
            {
                var centeredWeightedSource = weightedSources[i] - sourceCenterOfMass * sqrtWeights[i];
                centeredWeightedSources.Add(centeredWeightedSource);
                scaleDenominator += math.dot(centeredWeightedSource, weightedSources[i]);
            }

            _sqrtWeights = sqrtWeights;
            _weightedSources = weightedSources;
            _centeredWeightedSources = centeredWeightedSources;
            _totalWeight = totalWeight;
            _scaleDenominator = scaleDenominator;
        }

        public enum FailureReason
        {
            None,
            InvalidInput,
            TooCompact,
            FirstScaleEstimationFailed,
            SecondScaleEstimationFailed,
            FinalSolveFailed,
        }

        public readonly struct DebugInfo
        {
            public readonly FailureReason Failure;
            public readonly int LandmarkCount;
            public readonly float DepthOffset;
            public readonly float FirstScale;
            public readonly float SecondScale;

            public DebugInfo(FailureReason failure, int landmarkCount, float depthOffset, float firstScale,
                float secondScale)
            {
                Failure = failure;
                LandmarkCount = landmarkCount;
                DepthOffset = depthOffset;
                FirstScale = firstScale;
                SecondScale = secondScale;
            }
        }

        public static bool TryEstimate(
            in Mediapipe.Tasks.Vision.HolisticLandmarker.HolisticLandmarkerResult result,
            int imageWidth,
            int imageHeight,
            out Matrix4x4 facialTransformationMatrix)
            => TryEstimate(result.faceLandmarks, imageWidth, imageHeight, out facialTransformationMatrix);

        public static bool TryEstimate(
            NormalizedLandmarks faceLandmarks,
            int imageWidth,
            int imageHeight,
            out Matrix4x4 facialTransformationMatrix)
            => TryEstimate(faceLandmarks, imageWidth, imageHeight, out facialTransformationMatrix, out _);

        public static bool TryEstimate(
            NormalizedLandmarks faceLandmarks,
            int imageWidth,
            int imageHeight,
            out Matrix4x4 facialTransformationMatrix,
            out DebugInfo debugInfo)
        {
            facialTransformationMatrix = Matrix4x4.identity;
            debugInfo = new DebugInfo(FailureReason.None, 0, 0.0f, 0.0f, 0.0f);

            var landmarks = faceLandmarks.landmarks;
            if (landmarks == null || landmarks.Count < RequiredLandmarkCount || imageWidth <= 0 || imageHeight <= 0)
            {
                debugInfo = new DebugInfo(FailureReason.InvalidInput, landmarks?.Count ?? 0, 0.0f, 0.0f, 0.0f);
                return false;
            }

            if (IsTooCompact(landmarks))
            {
                debugInfo = new DebugInfo(FailureReason.TooCompact, landmarks.Count, 0.0f, 0.0f, 0.0f);
                return false;
            }

            var frustum = PerspectiveCameraFrustum.Create(imageWidth, imageHeight);
            var depthOffset = ComputeDepthOffset(landmarks, frustum);
            var projectedBasis = BuildProjectedBasisLandmarks(landmarks, frustum);
            if (!TryEstimateRawMetricTransform(
                    projectedBasis,
                    depthOffset,
                    out var rawPoseTransform,
                    out var failureReason,
                    out var firstScale,
                    out var secondScale))
            {
                debugInfo = new DebugInfo(failureReason, landmarks.Count, depthOffset, firstScale, secondScale);
                return false;
            }

            facialTransformationMatrix = ConvertRawMetricTransformToUnity(rawPoseTransform);
            debugInfo = new DebugInfo(FailureReason.None, landmarks.Count, depthOffset, firstScale, secondScale);
            return true;
        }

        private static bool IsTooCompact(List<NormalizedLandmark> landmarks)
        {
            var meanX = 0.0f;
            var meanY = 0.0f;
            for (var i = 0; i < landmarks.Count; i++)
            {
                meanX += (landmarks[i].x - meanX) / (i + 1.0f);
                meanY += (landmarks[i].y - meanY) / (i + 1.0f);
            }

            var maxSqDist = 0.0f;
            for (var i = 0; i < landmarks.Count; i++)
            {
                var dx = landmarks[i].x - meanX;
                var dy = landmarks[i].y - meanY;
                maxSqDist = math.max(maxSqDist, dx * dx + dy * dy);
            }

            return math.sqrt(maxSqDist) <= CompactnessThreshold;
        }

        private static float ComputeDepthOffset(List<NormalizedLandmark> landmarks, PerspectiveCameraFrustum frustum)
        {
            var sum = 0.0f;
            for (var i = 0; i < landmarks.Count; i++)
            {
                sum += landmarks[i].z * frustum.XScale;
            }

            return sum / landmarks.Count;
        }

        private static FixedList512Bytes<float3> BuildProjectedBasisLandmarks(List<NormalizedLandmark> landmarks,
            PerspectiveCameraFrustum frustum)
        {
            var result = new FixedList512Bytes<float3>();
            for (var i = 0; i < _basis.Length; i++)
            {
                var landmark = landmarks[_basis[i].LandmarkIndex];
                var x = landmark.x * frustum.XScale + frustum.Left;
                var y = (1.0f - landmark.y) * frustum.YScale + frustum.Bottom;
                var z = landmark.z * frustum.XScale;
                result.Add(new float3(x, y, z));
            }

            return result;
        }

        [BurstCompile]
        private static bool TryEstimateRawMetricTransform(
            in FixedList512Bytes<float3> projectedBasisLandmarks,
            float depthOffset,
            out float4x4 rawPoseTransform,
            out FailureReason failureReason,
            out float firstScale,
            out float secondScale)
        {
            rawPoseTransform = float4x4.identity;
            failureReason = FailureReason.None;
            firstScale = 0.0f;
            secondScale = 0.0f;

            var firstPassBasis = projectedBasisLandmarks;
            ChangeHandedness(ref firstPassBasis);
            if (!TryEstimateScale(firstPassBasis, out firstScale))
            {
                failureReason = FailureReason.FirstScaleEstimationFailed;
                return false;
            }

            var secondPassBasis = projectedBasisLandmarks;
            MoveAndRescaleZ(ref secondPassBasis, depthOffset, firstScale);
            UnprojectXY(ref secondPassBasis);
            ChangeHandedness(ref secondPassBasis);
            if (!TryEstimateScale(secondPassBasis, out secondScale))
            {
                failureReason = FailureReason.SecondScaleEstimationFailed;
                return false;
            }

            var finalMetricBasis = projectedBasisLandmarks;
            MoveAndRescaleZ(ref finalMetricBasis, depthOffset, firstScale * secondScale);
            UnprojectXY(ref finalMetricBasis);
            ChangeHandedness(ref finalMetricBasis);
            if (!TrySolveWeightedOrthogonalProblem(finalMetricBasis, out rawPoseTransform))
            {
                failureReason = FailureReason.FinalSolveFailed;
                return false;
            }

            return true;
        }

        [BurstCompile]
        private static void MoveAndRescaleZ(ref FixedList512Bytes<float3> landmarks, float depthOffset, float scale)
        {
            if (scale <= Epsilon)
            {
                return;
            }

            for (var i = 0; i < landmarks.Length; i++)
            {
                var point = landmarks[i];
                point.z = (point.z - depthOffset + NearPlane) / scale;
                landmarks[i] = point;
            }
        }

        [BurstCompile]
        private static void UnprojectXY(ref FixedList512Bytes<float3> landmarks)
        {
            for (var i = 0; i < landmarks.Length; i++)
            {
                var point = landmarks[i];
                point.x = point.x * point.z / NearPlane;
                point.y = point.y * point.z / NearPlane;
                landmarks[i] = point;
            }
        }

        [BurstCompile]
        private static void ChangeHandedness(ref FixedList512Bytes<float3> landmarks)
        {
            for (var i = 0; i < landmarks.Length; i++)
            {
                var point = landmarks[i];
                point.z *= -1.0f;
                landmarks[i] = point;
            }
        }

        [BurstCompile]
        private static bool TryEstimateScale(in FixedList512Bytes<float3> runtimeBasisPoints, out float scale)
        {
            scale = 1.0f;
            if (!TrySolveWeightedOrthogonalProblem(runtimeBasisPoints, out var transform))
            {
                return false;
            }

            scale = math.length(new float3(transform.c0.x, transform.c0.y, transform.c0.z));
            return scale > Epsilon;
        }

        [BurstCompile]
        private static bool TrySolveWeightedOrthogonalProblem(in FixedList512Bytes<float3> targetPoints,
            out float4x4 transform)
        {
            transform = float4x4.identity;

            if (_totalWeight <= Epsilon)
            {
                return false;
            }

            var weightedTargets = new FixedList512Bytes<float3>();
            for (var i = 0; i < _basis.Length; i++)
            {
                var sqrtWeight = _sqrtWeights[i];
                var weightedTarget = targetPoints[i] * sqrtWeight;
                weightedTargets.Add(weightedTarget);
            }

            var design = ComputeDesignMatrix(weightedTargets, _centeredWeightedSources);
            if (!TryComputeOptimalRotation(design, out var rotation))
            {
                return false;
            }

            var numerator = 0.0f;
            for (var i = 0; i < _basis.Length; i++)
            {
                var rotated = math.mul(rotation, _centeredWeightedSources[i]);
                numerator += math.dot(rotated, weightedTargets[i]);
            }

            if (math.abs(_scaleDenominator) <= Epsilon)
            {
                return false;
            }

            var scale = numerator / _scaleDenominator;
            if (scale <= Epsilon)
            {
                return false;
            }

            var scaledRotation = CreateScaledRotationMatrix(rotation, scale);
            var translation = float3.zero;
            for (var i = 0; i < _basis.Length; i++)
            {
                var transformedWeightedSource = math.mul(scaledRotation, new float4(_weightedSources[i], 1.0f)).xyz;
                var diff = weightedTargets[i] - transformedWeightedSource;
                translation += diff * _sqrtWeights[i];
            }

            translation /= _totalWeight;

            transform = scaledRotation;
            transform.c3 = new float4(translation, 1.0f);
            return true;
        }

        [BurstCompile]
        private static float3x3 ComputeDesignMatrix(in FixedList512Bytes<float3> weightedTargets,
            in FixedList512Bytes<float3> centeredWeightedSources)
        {
            var design = float3x3.zero;
            for (var i = 0; i < weightedTargets.Length; i++)
            {
                var target = weightedTargets[i];
                var source = centeredWeightedSources[i];

                design.c0.x += target.x * source.x;
                design.c1.x += target.x * source.y;
                design.c2.x += target.x * source.z;
                design.c0.y += target.y * source.x;
                design.c1.y += target.y * source.y;
                design.c2.y += target.y * source.z;
                design.c0.z += target.z * source.x;
                design.c1.z += target.z * source.y;
                design.c2.z += target.z * source.z;
            }

            return design;
        }

        [BurstCompile]
        private static bool TryComputeOptimalRotation(float3x3 design, out quaternion rotation)
        {
            rotation = quaternion.identity;
            var designNorm =
                design.c0.x * design.c0.x + design.c1.x * design.c1.x + design.c2.x * design.c2.x +
                design.c0.y * design.c0.y + design.c1.y * design.c1.y + design.c2.y * design.c2.y +
                design.c0.z * design.c0.z + design.c1.z * design.c1.z + design.c2.z * design.c2.z;
            if (designNorm <= Epsilon)
            {
                return false;
            }

            // `design` is built as target * source^T to mirror MediaPipe's SVD path.
            // The helper name historically suggested a transpose step, but the
            // compatibility-preserving implementation feeds the covariance through as-is.
            var n = BuildHornMatrix(design);
            var q = PowerIteration(n, PowerIterationCount);
            if (math.lengthsq(q) <= Epsilon)
            {
                return false;
            }

            q /= math.sqrt(math.lengthsq(q));
            rotation = math.normalizesafe(new quaternion(q.y, q.z, q.w, q.x), quaternion.identity);
            return true;
        }

        [BurstCompile]
        private static float4x4 CreateScaledRotationMatrix(quaternion rotation, float scale)
        {
            var matrix = new float4x4(rotation, float3.zero);
            matrix.c0.xyz *= scale;
            matrix.c1.xyz *= scale;
            matrix.c2.xyz *= scale;
            return matrix;
        }

        private static Matrix4x4 ConvertRawMetricTransformToUnity(float4x4 rawMetricTransform)
        {
            var unity = Matrix4x4.identity;
            unity.SetRow(0,
                new Vector4(rawMetricTransform.c0.x, rawMetricTransform.c1.x, -rawMetricTransform.c2.x,
                    rawMetricTransform.c3.x));
            unity.SetRow(1,
                new Vector4(rawMetricTransform.c0.y, rawMetricTransform.c1.y, -rawMetricTransform.c2.y,
                    rawMetricTransform.c3.y));
            unity.SetRow(2,
                new Vector4(-rawMetricTransform.c0.z, -rawMetricTransform.c1.z, rawMetricTransform.c2.z,
                    -rawMetricTransform.c3.z));
            unity.SetRow(3,
                new Vector4(rawMetricTransform.c0.w, rawMetricTransform.c1.w, -rawMetricTransform.c2.w,
                    rawMetricTransform.c3.w));
            return unity;
        }

        [BurstCompile]
        private static float4 BuildHornMatrix(float3x3 design, int row)
        {
            var trace = design.c0.x + design.c1.y + design.c2.z;
            return row switch
            {
                0 => new float4(trace, design.c1.z - design.c2.y, design.c2.x - design.c0.z, design.c0.y - design.c1.x),
                1 => new float4(design.c1.z - design.c2.y, design.c0.x - design.c1.y - design.c2.z,
                    design.c0.y + design.c1.x, design.c0.z + design.c2.x),
                2 => new float4(design.c2.x - design.c0.z, design.c0.y + design.c1.x,
                    -design.c0.x + design.c1.y - design.c2.z, design.c1.z + design.c2.y),
                _ => new float4(design.c0.y - design.c1.x, design.c0.z + design.c2.x, design.c1.z + design.c2.y,
                    -design.c0.x - design.c1.y + design.c2.z)
            };
        }

        [BurstCompile]
        private static float4x4 BuildHornMatrix(float3x3 design)
        {
            return FromRows(
                BuildHornMatrix(design, 0),
                BuildHornMatrix(design, 1),
                BuildHornMatrix(design, 2),
                BuildHornMatrix(design, 3));
        }

        [BurstCompile]
        private static float4 PowerIteration(float4x4 matrix, int iterationCount)
        {
            var vector = new float4(1.0f, 0.0f, 0.0f, 0.0f);
            for (var i = 0; i < iterationCount; i++)
            {
                vector = math.mul(matrix, vector);
                var norm = math.length(vector);
                if (norm <= Epsilon)
                {
                    break;
                }

                vector /= norm;
            }

            return vector;
        }

        [BurstCompile]
        private static float4x4 FromRows(float4 row0, float4 row1, float4 row2, float4 row3)
        {
            return new float4x4(
                new float4(row0.x, row1.x, row2.x, row3.x),
                new float4(row0.y, row1.y, row2.y, row3.y),
                new float4(row0.z, row1.z, row2.z, row3.z),
                new float4(row0.w, row1.w, row2.w, row3.w));
        }

        private readonly struct BasisPoint
        {
            public readonly int LandmarkIndex;
            public readonly float Weight;
            public readonly float3 Position;

            public BasisPoint(int landmarkIndex, float weight, float3 position)
            {
                LandmarkIndex = landmarkIndex;
                Weight = weight;
                Position = position;
            }
        }

        private readonly struct PerspectiveCameraFrustum
        {
            public readonly float Left;
            public readonly float Bottom;
            public readonly float XScale;
            public readonly float YScale;

            private PerspectiveCameraFrustum(float left, float bottom, float xScale, float yScale)
            {
                Left = left;
                Bottom = bottom;
                XScale = xScale;
                YScale = yScale;
            }

            public static PerspectiveCameraFrustum Create(int frameWidth, int frameHeight)
            {
                var heightAtNear = 2.0f * NearPlane * math.tan(0.5f * VerticalFovDegrees * math.PI / 180.0f);
                var widthAtNear = frameWidth * heightAtNear / frameHeight;
                return new PerspectiveCameraFrustum(-0.5f * widthAtNear, -0.5f * heightAtNear, widthAtNear,
                    heightAtNear);
            }
        }
    }
}
