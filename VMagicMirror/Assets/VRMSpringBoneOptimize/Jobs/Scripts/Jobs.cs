namespace VRM.Optimize.Jobs
{
    using UnityEngine;
    using UnityEngine.Jobs;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Burst;

    [BurstCompile]
    public struct UpdateColliderHashJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<ColliderGroupJobData.GroupParam> GroupParams;
        public NativeMultiHashMap<int, SphereCollider>.Concurrent ColliderHashMap;

        void IJobParallelForTransform.Execute(int index, TransformAccess trsAccess)
        {
            var mat = new float4x4(trsAccess.rotation, trsAccess.position);
            var param = this.GroupParams[index];
            for (var i = 0; i < param.SphereCollidersLength; i++)
            {
                var blittableFields = param.GetBlittableFields(i);
                var collider = new SphereCollider
                {
                    Position = math.transform(mat, blittableFields.Offset),
                    Radius = blittableFields.Radius,
                };
                this.ColliderHashMap.Add(param.InstanceID, collider);
            }
        }
    }

    [BurstCompile]
    public struct UpdateParentRotationJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<quaternion> ParentRotations;

        void IJobParallelForTransform.Execute(int index, TransformAccess parentTrsAccess)
        {
            ParentRotations[index] = parentTrsAccess.rotation;
        }
    }

    /// <summary>
    /// original from
    /// http://rocketjump.skr.jp/unity3d/109/
    /// </summary>
    [BurstCompile]
    public unsafe struct LogicJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<SpringBoneJobData.ImmutableNodeParam> ImmutableNodeParams;
        [ReadOnly] public NativeArray<quaternion> ParentRotations;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public NativeMultiHashMap<int, SphereCollider> ColliderHashMap;
        public NativeArray<SpringBoneJobData.VariableNodeParam> VariableNodeParams;

        void IJobParallelForTransform.Execute(int index, TransformAccess trsAccess)
        {
            var blittableFields = *this.ImmutableNodeParams[index].BlittableFieldsPtr;
            var vecLength = this.ImmutableNodeParams[index].Length;
            var localRotation = this.ImmutableNodeParams[index].LocalRotation;
            var boneAxis = this.ImmutableNodeParams[index].BoneAxis;

            var parentRotation = this.ParentRotations[index];
            float3 position = trsAccess.position;
            
            // 物理演算で用いるパラメータの事前計算
            var stiffnessForce = blittableFields.StiffnessForce * this.DeltaTime;
            var dragForce = blittableFields.DragForce;
            var external = blittableFields.GravityDir * (blittableFields.GravityPower * this.DeltaTime);

            var centerMatrixPtr = this.ImmutableNodeParams[index].CenterMatrixPtr;
            var isCenter = (centerMatrixPtr != null);
            var centerMatrix = isCenter ? *centerMatrixPtr : float4x4.identity;
            var centerInvertMatrix = isCenter ? math.inverse(centerMatrix) : float4x4.identity;

            var currentTail = math.transform(centerMatrix, this.VariableNodeParams[index].CurrentTail);
            var prevTail = math.transform(centerMatrix, this.VariableNodeParams[index].PrevTail);

            // verlet積分で次の位置を計算
            var nextTail = currentTail
                           // 前フレームの移動を継続する(減衰もあるよ)
                           + (currentTail - prevTail) * (1.0f - dragForce)
                           // 親の回転による子ボーンの移動目標
                           + math.mul(math.mul(parentRotation, localRotation), boneAxis) * stiffnessForce
                           // 外力による移動量
                           + external;

            // 長さをboneLengthに強制
            nextTail = position + math.normalize(nextTail - position) * vecLength;

            // Collisionで移動
            this.Collision(ref nextTail, ref position, ref vecLength, ref blittableFields);

            this.VariableNodeParams[index] = new SpringBoneJobData.VariableNodeParam
            {
                CurrentTail = math.transform(centerInvertMatrix, nextTail),
                PrevTail = math.transform(centerInvertMatrix, currentTail),
            };

            // 回転を適用
            trsAccess.rotation = this.ApplyRotation(ref nextTail, ref parentRotation, ref localRotation,
                ref position, ref boneAxis);
        }

        quaternion ApplyRotation(ref float3 nextTail, ref quaternion parentRotation, ref quaternion localRotation,
            ref float3 position, ref float3 boneAxis)
        {
            var rotation = math.mul(parentRotation, localRotation);
            return Quaternion.FromToRotation(math.mul(rotation, boneAxis), nextTail - position) * rotation;
        }

        void Collision(ref float3 nextTail, ref float3 position, ref float vecLength,
            ref VRMSpringBoneJob.BlittableFields blittableFields)
        {
            var hitRadius = blittableFields.HitRadius;
            for (var i = 0; i < blittableFields.ColliderGroupInstanceIDsLength; i++)
            {
                var instanceID = blittableFields.GetColliderGroupInstanceID(i);
                for (var success =
                        this.ColliderHashMap.TryGetFirstValue(instanceID, out var collider, out var iterator);
                    success;
                    success = this.ColliderHashMap.TryGetNextValue(out collider, ref iterator))
                {
                    var r = hitRadius + collider.Radius;
                    if (!(math.lengthsq(nextTail - collider.Position) <= (r * r))) continue;
                    // ヒット。Colliderの半径方向に押し出す
                    var normal = math.normalize(nextTail - collider.Position);
                    var posFromCollider = collider.Position + normal * (hitRadius + collider.Radius);
                    // 長さをboneLengthに強制
                    nextTail = position + math.normalize(posFromCollider - position) * vecLength;
                }
            }
        }
    }
}

