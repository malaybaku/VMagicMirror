namespace VRM.Optimize.Jobs
{
    using System.Collections.Generic;
    using UnityEngine.Jobs;
    using UnityEngine.Assertions;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// NativeMultiHashMap登録用
    /// </summary>
    public struct SphereCollider
    {
        public float3 Position;
        public float Radius;
    }

    /// <summary>
    /// Jobで使用するColliderの情報
    /// </summary>
    public unsafe struct ColliderGroupJobData : System.IDisposable
    {
        /// <summary>
        /// VRMSpringBoneColliderGroupが持つ情報
        /// </summary>
        public struct GroupParam
        {
            public int InstanceID;
            public VRMSpringBoneColliderGroupJob.BlittableFields* BlittableFieldsPtr;
            public int SphereCollidersLength;

            public VRMSpringBoneColliderGroupJob.BlittableFields GetBlittableFields(int index)
            {
                Assert.IsTrue((index >= 0) && (index < this.SphereCollidersLength));
                return *(this.BlittableFieldsPtr + index);
            }
        }

        public TransformAccessArray TransformAccessArray;
        public NativeArray<GroupParam> GroupParams;

        public bool IsCreated => TransformAccessArray.isCreated;

        public ColliderGroupJobData(IReadOnlyList<VRMSpringBoneColliderGroupJob> groups)
        {
            var length = groups.Count;
            this.TransformAccessArray = new TransformAccessArray(length);
            foreach (var group in groups)
            {
                this.TransformAccessArray.Add(group.transform);
            }            
            
            this.GroupParams = new NativeArray<GroupParam>(length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < length; i++)
            {
                var group = groups[i];
                Assert.IsTrue(group.BlittableFieldsArray.IsCreated);
                this.GroupParams[i] = new GroupParam
                {
                    InstanceID = group.GetInstanceID(),
                    BlittableFieldsPtr =
                        (VRMSpringBoneColliderGroupJob.BlittableFields*) @group.BlittableFieldsArray.GetUnsafePtr(),
                    SphereCollidersLength = group.BlittableFieldsArray.Length,
                };
            }
        }

        public void Dispose()
        {
            this.TransformAccessArray.Dispose();
            this.GroupParams.Dispose();
        }
    }

    /// <summary>
    /// Jobで使用するSpringBoneの情報
    /// </summary>
    public unsafe struct SpringBoneJobData : System.IDisposable
    {
        /// <summary>
        /// ノードが持つパラメータ(不変値)
        /// </summary>
        public struct ImmutableNodeParam
        {
            public float Length;
            public quaternion LocalRotation;
            public float3 BoneAxis;
            public VRMSpringBoneJob.BlittableFields* BlittableFieldsPtr;
            public float4x4* CenterMatrixPtr;
        }

        /// <summary>
        /// ノードが持つパラメータ(可変値)
        /// </summary>
        public struct VariableNodeParam
        {
            public float3 CurrentTail;
            public float3 PrevTail;
        }

        public TransformAccessArray TransformAccessArray;
        public TransformAccessArray ParentTransformAccessArray;
        public NativeArray<ImmutableNodeParam> ImmutableNodeParams;
        public NativeArray<VariableNodeParam> VariableNodeParams;

        public bool IsCreated => TransformAccessArray.isCreated;
        public int Length { get; }

        public SpringBoneJobData(IReadOnlyList<VRMSpringBoneJob.Node> nodes)
        {
            var length = nodes.Count;
            this.Length = length;
            this.TransformAccessArray = new TransformAccessArray(length);
            this.ParentTransformAccessArray = new TransformAccessArray(length);
            foreach (var node in nodes)
            {
                this.TransformAccessArray.Add(node.Transform);
                this.ParentTransformAccessArray.Add(node.Transform.parent);
            }
                 
            this.ImmutableNodeParams = new NativeArray<ImmutableNodeParam>(length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            this.VariableNodeParams = new NativeArray<VariableNodeParam>(length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < length; i++)
            {
                var node = nodes[i];
                this.ImmutableNodeParams[i] = new ImmutableNodeParam
                {
                    Length = node.Length,
                    LocalRotation = node.LocalRotation,
                    BoneAxis = node.BoneAxis,
                    BlittableFieldsPtr = node.BlittableFieldsPtr,
                    CenterMatrixPtr = node.CenterMatrixPtr,
                };
                this.VariableNodeParams[i] = new VariableNodeParam
                {
                    CurrentTail = node.InitTail,
                    PrevTail = node.InitTail,
                };
            }
        }

        public void Dispose()
        {
            this.TransformAccessArray.Dispose();
            this.ParentTransformAccessArray.Dispose();
            this.ImmutableNodeParams.Dispose();
            this.VariableNodeParams.Dispose();
        }
    }
}
