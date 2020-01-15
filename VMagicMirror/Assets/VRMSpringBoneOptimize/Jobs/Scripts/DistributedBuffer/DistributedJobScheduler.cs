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
    public sealed class DistributedJobScheduler : MonoBehaviour, IDisposable
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
        readonly List<DistributedBuffer> _currentBuffers = new List<DistributedBuffer>();

        #endregion // Private Fields


        // ----------------------------------------------------

        #region // Unity Events

        /// <summary>
        /// MonoBehaviour.Start
        /// </summary>
        void Start()
        {
            if (!this._isAutoGetBuffers) return;
            foreach (var buffer in FindObjectsOfType<DistributedBuffer>())
            {
                this.AddBuffer(buffer);
            }
        }

        /// <summary>
        /// MonoBehaviour.LateUpdate
        /// </summary>
        void LateUpdate()
        {
            this._jobHandle.Complete();

            // m_centerの更新
            foreach (var buff in this._currentBuffers)
            {
                if (buff.UpdateCenterBones == null) continue;
                foreach (var springBone in buff.UpdateCenterBones)
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
            var buffer = obj.GetComponent<DistributedBuffer>();
            if (buffer == null)
            {
                buffer = obj.AddComponent<DistributedBuffer>();
            }

            AddBuffer(buffer);
        }

        public void RemoveBuffer(GameObject obj)
        {
            var buffer = obj.GetComponent<DistributedBuffer>();
            Assert.IsTrue(buffer != null);

            RemoveBuffer(buffer);
        }

        public void AddBuffer(DistributedBuffer buffer)
        {
            if (this._currentBuffers.Contains(buffer)) return;
            buffer.Initialize();
            this._currentBuffers.Add(buffer);
        }

        public void RemoveBuffer(DistributedBuffer buffer)
        {
            if (!this._currentBuffers.Contains(buffer)) return;
            buffer.Dispose();
            this._currentBuffers.Remove(buffer);
        }

        #endregion // Public Methods

        // ----------------------------------------------------

        #region // Private Methods

        void ExecuteJobs()
        {
            var handles = new NativeArray<JobHandle>(this._currentBuffers.Count, Allocator.TempJob);
            for (var i = 0; i < this._currentBuffers.Count; ++i)
            {
                var buff = this._currentBuffers[i];
                if (buff.ColliderHashMap.IsCreated)
                {
                    buff.ColliderHashMap.Dispose();
                }

                // コライダーの更新
                buff.ColliderHashMap = new NativeMultiHashMap<int, SphereCollider>(
                    buff.ColliderHashMapLength, Allocator.TempJob);

                var handle = new UpdateColliderHashJob
                {
                    GroupParams = buff.ColliderGroupJobDataValue.GroupParams,
                    ColliderHashMap = buff.ColliderHashMap.ToConcurrent(),
                }.Schedule(buff.ColliderGroupJobDataValue.TransformAccessArray);

                // 親の回転の取得
                handle = new UpdateParentRotationJob
                {
                    ParentRotations = buff.ParentRotations,
                }.Schedule(buff.SpringBoneJobDataValue.ParentTransformAccessArray, handle);

                // 物理演算
                handles[i] = new LogicJob
                {
                    ImmutableNodeParams = buff.SpringBoneJobDataValue.ImmutableNodeParams,
                    ParentRotations = buff.ParentRotations,
                    DeltaTime = Time.deltaTime,
                    ColliderHashMap = buff.ColliderHashMap,
                    VariableNodeParams = buff.SpringBoneJobDataValue.VariableNodeParams,
                }.Schedule(buff.SpringBoneJobDataValue.TransformAccessArray, handle);
            }

            this._jobHandle = JobHandle.CombineDependencies(handles);
            handles.Dispose();
            JobHandle.ScheduleBatchedJobs();
        }

        void IDisposable.Dispose()
        {
            foreach (var buffer in this._currentBuffers.ToArray())
            {
                this.RemoveBuffer(buffer);
            }
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
            if (this._currentBuffers == null
                || this._currentBuffers.Count <= 0
                || !this._drawGizmo)
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
            foreach (var buff in this._currentBuffers)
            {
                Gizmos.matrix = Matrix4x4.identity;
                var length = buff.SpringBoneJobDataValue.TransformAccessArray.length;
                var immParam = buff.SpringBoneJobDataValue.ImmutableNodeParams;
                var valParam = buff.SpringBoneJobDataValue.VariableNodeParams;
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

                    var position = buff.SpringBoneJobDataValue.TransformAccessArray[i].position;
                    Gizmos.color = this._jobSpringBoneColor;
                    Gizmos.DrawLine(currentTailVal, position);
                    Gizmos.DrawWireSphere(currentTailVal, radius);
                }
            }
        }

        void DrawColliderGroupGizmos()
        {
            Gizmos.matrix = Matrix4x4.identity;
            foreach (var buff in this._currentBuffers)
            {
                var length = buff.ColliderGroupJobDataValue.TransformAccessArray.length;
                var groupParam = buff.ColliderGroupJobDataValue.GroupParams;
                for (var i = 0; i < length; i++)
                {
                    var trs = buff.ColliderGroupJobDataValue.TransformAccessArray[i];
                    var mat = new float4x4(trs.rotation, trs.position);
                    for (var j = 0; j < groupParam[i].SphereCollidersLength; j++)
                    {
                        var sphereCollider = groupParam[i].GetBlittableFields(j);
                        Gizmos.color = this._jobColliderColor;
                        Gizmos.DrawWireSphere(math.transform(mat, sphereCollider.Offset), sphereCollider.Radius);
                    }
                }
            }
        }

        void DrawOriginalColliderGroupGizmos()
        {
            foreach (var buff in this._currentBuffers)
            {
                foreach (var springBone in buff.SpringBones)
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
        }

        #endregion // OnDrawGizmos

#endif
    }
}
