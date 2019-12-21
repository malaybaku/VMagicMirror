namespace VRM.Optimize.Jobs
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using VRM.Optimize;
    using IDisposable = System.IDisposable;

    public sealed unsafe class VRMSpringBoneJob : MonoBehaviour, IDisposable
    {
        // ------------------------------

        #region // Defines

        /// <summary>
        /// アンマネージドメモリ上で運用する想定の値
        /// </summary>
        public unsafe struct BlittableFields
        {
            public float StiffnessForce;
            public float GravityPower;
            public float3 GravityDir;
            public float DragForce;
            public float HitRadius;

            public int* ColliderGroupInstanceIDs;
            public int ColliderGroupInstanceIDsLength;

            public int GetColliderGroupInstanceID(int index)
            {
                Assert.IsTrue((index >= 0) && (index < this.ColliderGroupInstanceIDsLength));
                return *(ColliderGroupInstanceIDs + index);
            }
        }

        /// <summary>
        /// 揺れ物の動作対象となるGameObject
        /// </summary>
        public sealed unsafe class Node
        {
            public Transform Transform { get; private set; }
            public float4x4* CenterMatrixPtr { get; private set; }
            public BlittableFields* BlittableFieldsPtr { get; private set; }
            public float3 InitTail { get; private set; }
            public float Length { get; private set; }
            public quaternion LocalRotation { get; private set; }
            public float3 BoneAxis { get; private set; }

            public Node(
                Transform nodeTrs,
                BlittableFields* blittableFieldsPtr,
                float4x4* centerMatrixPtr,
                float3 initTail,
                float length,
                quaternion localRotation,
                float3 boneAxis)
            {
                this.Transform = nodeTrs;
                this.BlittableFieldsPtr = blittableFieldsPtr;
                this.CenterMatrixPtr = centerMatrixPtr;
                this.InitTail = initTail;
                this.Length = length;
                this.LocalRotation = localRotation;
                this.BoneAxis = boneAxis;
            }
        }

        #endregion // Defines

        // ------------------------------

        #region // Fields(Editable)

        public string m_comment;

        [Header("Settings")] [Range(0, 4)] public float m_stiffnessForce = 1.0f;
        [Range(0, 2)] public float m_gravityPower = 0;
        public Vector3 m_gravityDir = new Vector3(0, -1.0f, 0);
        [Range(0, 1)] public float m_dragForce = 0.4f;
        public Transform m_center;
        public List<Transform> RootBones = new List<Transform>();

        [Header("Collider")] [Range(0, 0.5f)] public float m_hitRadius = 0.02f;
        public VRMSpringBoneColliderGroupJob[] ColliderGroups;

        #endregion // Fields(Editable)

        // ------------------------------

        #region // Fields

        public List<Node> Nodes { get; private set; } = new List<Node>();
        
        BlittableFields* _blittableFieldsPtr = null;
        float4x4* _centerMatrixPtr = null;
        NativeArray<int> _colliderGroupInstanceIDs;

        #endregion // Fields


        // ----------------------------------------------------

        #region // Unity Events

#if UNITY_EDITOR && ENABLE_DEBUG
        /// <summary>
        /// MonoBehaviour.Update
        /// </summary>
        void Update()
        {
            // Editor上でのみInspectorからの動的変更を考慮する
            if (this._blittableFieldsPtr == null)
            {
                return;
            }

            this.CopyBlittableFields();
        }
#endif

        #endregion // Unity Events

        // ----------------------------------------------------

        #region // Public Methods

        public void Initialize()
        {
            this._blittableFieldsPtr =
                (BlittableFields*) UnsafeUtilityHelper.Malloc<BlittableFields>(Allocator.Persistent);

            if (this.m_center != null)
            {
                this._centerMatrixPtr = (float4x4*) UnsafeUtilityHelper.Malloc<float4x4>(Allocator.Persistent);
                this.UpdateCenterMatrix();
            }

            if (this.ColliderGroups != null && this.ColliderGroups.Length > 0)
            {
                this._colliderGroupInstanceIDs = new NativeArray<int>(this.ColliderGroups.Length, Allocator.Persistent);
                for (var i = 0; i < this._colliderGroupInstanceIDs.Length; i++)
                {
                    this._colliderGroupInstanceIDs[i] = this.ColliderGroups[i].GetInstanceID();
                }
            }

            this.CopyBlittableFields();

            foreach (var go in RootBones)
            {
                if (go == null)
                {
                    continue;
                }

                this.CreateNode(go);
            }
        }

        public void Dispose()
        {
            if (this._blittableFieldsPtr != null)
            {
                UnsafeUtility.Free(this._blittableFieldsPtr, Allocator.Persistent);
                this._blittableFieldsPtr = null;
            }

            if (this._centerMatrixPtr != null)
            {
                UnsafeUtility.Free(this._centerMatrixPtr, Allocator.Persistent);
                this._centerMatrixPtr = null;
            }

            if (this._colliderGroupInstanceIDs.IsCreated)
            {
                this._colliderGroupInstanceIDs.Dispose();
            }

            Nodes?.Clear();
        }

        public void UpdateCenterMatrix()
        {
            *this._centerMatrixPtr = this.m_center.localToWorldMatrix;
        }

        #endregion // Public Methods

        // ----------------------------------------------------

        #region // Private Methods

        void CopyBlittableFields()
        {
            *this._blittableFieldsPtr = new BlittableFields
            {
                StiffnessForce = this.m_stiffnessForce,
                GravityPower = this.m_gravityPower,
                GravityDir = this.m_gravityDir,
                DragForce = this.m_dragForce,
                HitRadius = this.m_hitRadius,

                ColliderGroupInstanceIDs = (this._colliderGroupInstanceIDs.IsCreated)
                    ? (int*) this._colliderGroupInstanceIDs.GetUnsafePtr()
                    : null,
                ColliderGroupInstanceIDsLength = (this._colliderGroupInstanceIDs.IsCreated)
                    ? this._colliderGroupInstanceIDs.Length
                    : 0,
            };
        }

        void CreateNode(Transform trs)
        {
            var nodeTrs = trs;
            Vector3 localChildPosition;
            if (nodeTrs.childCount == 0)
            {
                var delta = nodeTrs.position - nodeTrs.parent.position;
                var childPosition = nodeTrs.position + delta.normalized * 0.07f;
                localChildPosition = nodeTrs.worldToLocalMatrix.MultiplyPoint(childPosition);
            }
            else
            {
                var firstChild = nodeTrs.childCount > 0 ? nodeTrs.GetChild(0) : null;
                var localPosition = firstChild.localPosition;
                var scale = firstChild.lossyScale;
                localChildPosition = new Vector3(
                    localPosition.x * scale.x,
                    localPosition.y * scale.y,
                    localPosition.z * scale.z);
            }

            var worldChildPosition = nodeTrs.TransformPoint(localChildPosition);
            var node = new Node(
                nodeTrs,
                this._blittableFieldsPtr,
                this._centerMatrixPtr,
                (this.m_center != null) ? this.m_center.InverseTransformPoint(worldChildPosition) : worldChildPosition,
                localChildPosition.magnitude,
                nodeTrs.localRotation,
                localChildPosition.normalized);
            this.Nodes.Add(node);
            foreach (Transform child in trs)
            {
                this.CreateNode(child);
            }
        }

        #endregion // Private Methods
    }
}
