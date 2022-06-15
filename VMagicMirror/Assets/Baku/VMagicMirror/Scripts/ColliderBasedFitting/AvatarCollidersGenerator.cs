using System;
using System.Collections.Generic;
using UniGLTF.MeshUtility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror
{
    public class AvatarColliders : IDisposable
    {
        private AvatarColliders(MeshCollider[] colliders, Mesh[] bakedMeshes)
        {
            Colliders = colliders;
            _bakedMeshes = bakedMeshes;
        }

        public MeshCollider[] Colliders { get; private set; }
        //NOTE: bakedじゃないメッシュのライフサイクルは参照元のモデルに準ずるので、勝手に捨てちゃいけないことに注意
        private Mesh[] _bakedMeshes;

        public void Dispose()
        {
            if (Colliders != null)
            {
                foreach (var collider in Colliders)
                {
                    Object.Destroy(collider.gameObject);
                }
            }

            Colliders = Array.Empty<MeshCollider>();

            if (_bakedMeshes != null)
            {
                foreach (var mesh in _bakedMeshes)
                {
                    Object.Destroy(mesh);
                }
            }
            _bakedMeshes = null;
        }
        
        public static AvatarColliders LoadMeshColliders(GameObject root, MeshCollider colliderPrefab, Transform prefabParent)
        {
            var colliders = new List<MeshCollider>();
            var bakedMeshes = new List<Mesh>();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                var collider = Object.Instantiate(colliderPrefab, prefabParent);
                collider.sharedMesh = null;
                //TODO: 位置、調整しないとダメかもね
                collider.sharedMesh = renderer.GetMesh();
                
                colliders.Add(collider);
            }

            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var collider = Object.Instantiate(colliderPrefab, prefabParent);
                var baked = new Mesh();
                renderer.BakeMesh(baked);
                //TODO: 位置、調整しないとダメかも
                collider.sharedMesh = baked;

                colliders.Add(collider);
                bakedMeshes.Add(baked);
            }

            return new AvatarColliders(colliders.ToArray(), bakedMeshes.ToArray());
        }
    }
}
