using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class RuntimeGizmo : MonoBehaviour
    {
        [Serializable]
        public struct GizmoSet
        {
            public Collider x;
            public Collider y;
            public Collider z;
        }

        [SerializeField]
        GizmoSet moveGizmo;

        [SerializeField]
        GizmoSet rotateGizmo;

        [SerializeField]
        GizmoSet scaleGizmo;





























































        void Start()
        {

        }

        void Update()
        {

        }
    }
}
