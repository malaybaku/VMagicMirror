namespace VRM.Optimize.Jobs
{
    using UnityEngine;
    using Unity.Collections;
    using Unity.Mathematics;
    using IDisposable = System.IDisposable;

    public sealed class VRMSpringBoneColliderGroupJob : MonoBehaviour, IDisposable
    {
        // ------------------------------

        #region // Defines

        [System.Serializable]
        public class SphereCollider
        {
            public Vector3 Offset;
            [Range(0, 1.0f)] public float Radius;
        }

        /// <summary>
        /// アンマネージドメモリ上で運用する想定の値
        /// </summary>
        public struct BlittableFields
        {
            public float3 Offset;
            public float Radius;
        }

        #endregion // Defines

        // ------------------------------

        #region // Fields(Editable)

        public SphereCollider[] Colliders = new SphereCollider[] {new SphereCollider {Radius = 0.1f}};

        #endregion // Fields(Editable)

        // ------------------------------

        #region // Fields

        public NativeArray<BlittableFields> BlittableFieldsArray;
        
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
            if (!this.BlittableFieldsArray.IsCreated)
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
            if (this.Colliders == null 
                || this.Colliders.Length <= 0
                || this.BlittableFieldsArray.IsCreated)
            {
                return;
            }

            // コライダー用のNativeArrayを確保
            this.BlittableFieldsArray = new NativeArray<BlittableFields>(this.Colliders.Length, Allocator.Persistent);
            this.CopyBlittableFields();
        }

        public void Dispose()
        {
            if (this.BlittableFieldsArray.IsCreated)
            {
                this.BlittableFieldsArray.Dispose();
            }
        }

        #endregion // Public Methods

        // ----------------------------------------------------

        #region // Private Methods

        void CopyBlittableFields()
        {
            for (var i = 0; i < this.Colliders.Length; i++)
            {
                var collider = this.Colliders[i];
                this.BlittableFieldsArray[i] = new BlittableFields
                {
                    Offset = collider.Offset,
                    Radius = collider.Radius,
                };
            }
        }

        #endregion // Private Methods
    }
}
