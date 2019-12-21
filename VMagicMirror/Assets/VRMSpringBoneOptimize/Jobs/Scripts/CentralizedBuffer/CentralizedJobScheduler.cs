namespace VRM.Optimize.Jobs
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Jobs;
    using UnityEngine.Assertions;
    using Unity.Jobs;
    using Unity.Collections;
    using Unity.Mathematics;
    using IDisposable = System.IDisposable;

#if UNITY_5_5_OR_NEWER
    // DefaultExecutionOrder(11000) means calclate springbone after FinaiIK( VRIK )
    [DefaultExecutionOrder(11000)]
#endif
    [DisallowMultipleComponent]
    public sealed class CentralizedJobScheduler : MonoBehaviour, IDisposable
    {
        // ------------------------------

        #region // Private Fields(Editable)

        [Header("【Settings】")] [SerializeField]
        bool _isAutoGetBuffers = false;

#if UNITY_EDITOR && ENABLE_DEBUG
        [Header("【Gizmos】")] [SerializeField] bool _drawGizmo = false;
        [SerializeField] Color _jobSpringBoneColor = Color.red;
        [SerializeField] Color _jobColliderColor = Color.magenta;
        [SerializeField] Color _originalColliderColor = Color.green;
#endif

        #endregion // Private Fields(Editable)

        // ------------------------------

        #region // Private Fields

        JobHandle _jobHandle;

        // Jobs Data
        SpringBoneJobData _springBoneJobData;
        ColliderGroupJobData _colliderGroupJobData;

        // Collider Data
        NativeMultiHashMap<int, SphereCollider> _colliderHashMap;
        int _colliderHashMapLength;

        // Parent Rotations
        NativeArray<quaternion> _parentRotations;

        // Center
        readonly List<VRMSpringBoneJob> _updateCenterBones = new List<VRMSpringBoneJob>();

        // Buffers
        readonly List<CentralizedBuffer> _currentBuffers = new List<CentralizedBuffer>();
        readonly List<VRMSpringBoneJob.Node> _allNodes = new List<VRMSpringBoneJob.Node>();
        readonly List<VRMSpringBoneColliderGroupJob> _allColliderGroups = new List<VRMSpringBoneColliderGroupJob>();
#if UNITY_EDITOR && ENABLE_DEBUG
        readonly List<VRMSpringBoneJob> _currentAllBones = new List<VRMSpringBoneJob>();
#endif

        #endregion // Private Fields


        // ----------------------------------------------------

        #region // Unity Events

        /// <summary>
        /// MonoBehaviour.Start
        /// </summary>
        void Start()
        {
            if (!this._isAutoGetBuffers) return;
            this.CreateBuffer(FindObjectsOfType<CentralizedBuffer>());
        }

        /// <summary>
        /// MonoBehaviour.LateUpdate
        /// </summary>
        void LateUpdate()
        {
            this._jobHandle.Complete();

            // m_centerの更新
            if (this._updateCenterBones.Count > 0)
            {
                foreach (var springBone in this._updateCenterBones)
                {
                    springBone.UpdateCenterMatrix();
                }
            }

            this.ExecuteJobs();
        }

        /// <summary>
        /// MonoBehaviour.OnDestroy
        /// </summary>
        void OnDestroy() => ((IDisposable) this).Dispose();

        #endregion // Unity Events

        // ----------------------------------------------------

        #region // Public Methods

        public void AddBuffer(GameObject obj)
        {
            var buffer = obj.GetComponent<CentralizedBuffer>();
            if (buffer == null)
            {
                buffer = obj.AddComponent<CentralizedBuffer>();
            }

            AddBuffer(buffer);
        }

        public void RemoveBuffer(GameObject obj)
        {
            var buffer = obj.GetComponent<CentralizedBuffer>();
            Assert.IsTrue(buffer != null);

            RemoveBuffer(buffer);
        }

        public void AddBuffer(CentralizedBuffer buffer)
        {
            if (this._currentBuffers.Contains(buffer)) return;
            buffer.Initialize();
            this._currentBuffers.Add(buffer);
            this.CreateBuffer();
        }

        public void RemoveBuffer(CentralizedBuffer buffer)
        {
            if (!this._currentBuffers.Contains(buffer)) return;
            buffer.Dispose();
            this._currentBuffers.Remove(buffer);
            this.CreateBuffer();
        }

        #endregion // Public Methods

        // ----------------------------------------------------

        #region // Private Methods

        void ExecuteJobs()
        {
            if (this._springBoneJobData.Length <= 0) return;

            if (!this._colliderHashMap.IsCreated)
            {
                // コライダーの初期化
                this._colliderHashMap = new NativeMultiHashMap<int, SphereCollider>(
                    this._colliderHashMapLength, Allocator.Persistent);
            }
            else
            {
                this._colliderHashMap.Clear();
            }

            if (this._colliderHashMap.Capacity != this._colliderHashMapLength)
            {
                this._colliderHashMap.Dispose();
                // コライダーの初期化
                this._colliderHashMap = new NativeMultiHashMap<int, SphereCollider>(
                    this._colliderHashMapLength, Allocator.Persistent);
            }

            var updateColliderHashJobHandle = new UpdateColliderHashJob
            {
                GroupParams = this._colliderGroupJobData.GroupParams,
                ColliderHashMap = this._colliderHashMap.ToConcurrent(),
            }.Schedule(this._colliderGroupJobData.TransformAccessArray);

            // 親の回転の取得
            var updateParentRotationJobHandle = new UpdateParentRotationJob
            {
                ParentRotations = this._parentRotations,
            }.Schedule(this._springBoneJobData.ParentTransformAccessArray);

            // 物理演算
            this._jobHandle = new LogicJob
            {
                ImmutableNodeParams = this._springBoneJobData.ImmutableNodeParams,
                ParentRotations = this._parentRotations,
                DeltaTime = Time.deltaTime,
                ColliderHashMap = this._colliderHashMap,
                VariableNodeParams = this._springBoneJobData.VariableNodeParams,
            }.Schedule(this._springBoneJobData.TransformAccessArray,
                JobHandle.CombineDependencies(updateColliderHashJobHandle, updateParentRotationJobHandle));

            JobHandle.ScheduleBatchedJobs();
        }

        void CreateBuffer(CentralizedBuffer[] initBuffers = null)
        {
            if (initBuffers != null)
            {
                this._currentBuffers.AddRange(initBuffers);
                foreach (var buffer in this._currentBuffers)
                {
                    buffer.Initialize();
                }
            }

            this.DisposeBuffers();

            foreach (var buffer in this._currentBuffers)
            {
#if UNITY_EDITOR && ENABLE_DEBUG
                foreach (var bone in buffer.SpringBones)
                {
                    this._currentAllBones.Add(bone);
                }
#endif
                foreach (var node in buffer.AllNodes)
                {
                    this._allNodes.Add(node);
                }

                foreach (var group in buffer.ColliderGroups)
                {
                    this._allColliderGroups.Add(group);
                }

                this._colliderHashMapLength += buffer.ColliderHashMapLength;

                if (buffer.UpdateCenterBones == null) continue;
                this._updateCenterBones.AddRange(buffer.UpdateCenterBones);
            }

            this._springBoneJobData = new SpringBoneJobData(this._allNodes);
            this._colliderGroupJobData = new ColliderGroupJobData(this._allColliderGroups);
            this._parentRotations = new NativeArray<quaternion>(this._allNodes.Count, Allocator.Persistent);
        }

        void IDisposable.Dispose()
        {
            foreach (var buffer in this._currentBuffers)
            {
                buffer.Dispose();
            }

            this._currentBuffers.Clear();

            DisposeBuffers();
        }

        void DisposeBuffers()
        {
            this._jobHandle.Complete();

            if (this._springBoneJobData.IsCreated)
            {
                this._springBoneJobData.Dispose();
            }

            if (this._colliderGroupJobData.IsCreated)
            {
                this._colliderGroupJobData.Dispose();
            }

            if (this._colliderHashMap.IsCreated)
            {
                this._colliderHashMap.Dispose();
            }

            if (this._parentRotations.IsCreated)
            {
                this._parentRotations.Dispose();
            }

            this._updateCenterBones.Clear();
            this._allNodes.Clear();
            this._allColliderGroups.Clear();
            this._colliderHashMapLength = 0;
#if UNITY_EDITOR && ENABLE_DEBUG
            this._currentAllBones.Clear();
#endif
        }

        #endregion // Private Methods


#if UNITY_EDITOR && ENABLE_DEBUG
        // ----------------------------------------------------

        #region // OnDrawGizmos

        /// <summary>
        /// MonoBehaviour.OnDrawGizmos
        /// </summary>
        void OnDrawGizmos()
        {
            if (this._currentAllBones == null || this._currentAllBones.Count <= 0 || !this._drawGizmo)
            {
                return;
            }

            this._jobHandle.Complete();
            this.DrawSpringBoneGizmos();
            this.DrawColliderGroupGizmos();
            this.DrawOriginalColliderGroupGizmos();
        }

        unsafe void DrawSpringBoneGizmos()
        {
            Gizmos.matrix = Matrix4x4.identity;
            var length = this._springBoneJobData.TransformAccessArray.length;
            var immParam = this._springBoneJobData.ImmutableNodeParams;
            var valParam = this._springBoneJobData.VariableNodeParams;
            for (var i = 0; i < length; i++)
            {
                var centerMatrixPtr = immParam[i].CenterMatrixPtr;
                var isCenter = (centerMatrixPtr != null);
                var centerMatrix = isCenter ? *centerMatrixPtr : float4x4.identity;
                var currentTailVal = math.transform(centerMatrix, valParam[i].CurrentTail);
                var prevTailVal = math.transform(centerMatrix, valParam[i].PrevTail);
                var param = immParam[i].BlittableFieldsPtr;
                var radius = param->HitRadius;
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(currentTailVal, prevTailVal);
                Gizmos.DrawWireSphere(prevTailVal, radius);

                var position = this._springBoneJobData.TransformAccessArray[i].position;
                Gizmos.color = this._jobSpringBoneColor;
                Gizmos.DrawLine(currentTailVal, position);
                Gizmos.DrawWireSphere(currentTailVal, radius);
            }
        }

        void DrawColliderGroupGizmos()
        {
            Gizmos.matrix = Matrix4x4.identity;
            var length = this._colliderGroupJobData.TransformAccessArray.length;
            var groupParam = this._colliderGroupJobData.GroupParams;
            for (var i = 0; i < length; i++)
            {
                var trs = this._colliderGroupJobData.TransformAccessArray[i];
                var mat = new float4x4(trs.rotation, trs.position);
                for (var j = 0; j < groupParam[i].SphereCollidersLength; j++)
                {
                    var sphereCollider = groupParam[i].GetBlittableFields(j);
                    Gizmos.color = this._jobColliderColor;
                    Gizmos.DrawWireSphere(math.transform(mat, sphereCollider.Offset), sphereCollider.Radius);
                }
            }
        }

        void DrawOriginalColliderGroupGizmos()
        {
            foreach (var springBone in this._currentAllBones)
            {
                Gizmos.matrix = Matrix4x4.identity;
                var colliderGroups = springBone.ColliderGroups;
                if (colliderGroups == null || colliderGroups.Length <= 0)
                {
                    continue;
                }

                foreach (var group in colliderGroups)
                {
                    Gizmos.color = this._originalColliderColor;
                    var mat = group.transform.localToWorldMatrix;
                    Gizmos.matrix = mat * Matrix4x4.Scale(new Vector3(
                                        1.0f / group.transform.lossyScale.x,
                                        1.0f / group.transform.lossyScale.y,
                                        1.0f / group.transform.lossyScale.z));
                    foreach (var y in group.Colliders)
                    {
                        Gizmos.DrawWireSphere(y.Offset, y.Radius);
                    }
                }
            }
        }

        #endregion // OnDrawGizmos

#endif
    }
}
