using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class LandmarksVisualizer : MonoBehaviour
    {
        [SerializeField] private LandmarkGizmo gizmoPrefab;
        [SerializeField] private LandmarkVisualizer2D visualizer2D;
        [SerializeField] private Transform singleVisualizeCube;
        [SerializeField] private Transform gizmoParent;

        [SerializeField] private Vector3 gizmoPositionScale = Vector3.one;
        [SerializeField] private Vector3 gizmoLocalScale = Vector3.one;

        [SerializeField] private LineRenderer lineRenderer;
        
        private readonly List<LandmarkGizmo> _gizmos = new();

        private readonly object _positionsLock = new();
        private readonly List<Vector3> _positions = new();

        private Vector3[] _lineRendererPositions = Array.Empty<Vector3>();
        
        // NOTE: matrix/poseはどっちか片方しか使われない(poseのほうが優先される)
        private bool _hasPose;
        private Pose _pose;
        private bool _hasMatrix;
        private Matrix4x4 _matrix;

        public LandmarkVisualizer2D Visualizer2D => visualizer2D;
        
        public void SetPositions(IEnumerable<Vector3> positions)
        {
            lock (_positionsLock)
            {
                _positions.Clear();
                _positions.AddRange(positions);
            }
        }

        public void SetSingleMatrix(Matrix4x4 mat)
        {
            lock (_positionsLock)
            {
                _matrix = mat;
                _hasMatrix = true;
            }
        }
        
        public void UnsetSingleMatrix()
        {
            lock (_positionsLock)
            {
                _hasMatrix = false;
            }
        }

        public void ClearPositions()
        {
            lock (_positionsLock)
            {
                _positions.Clear();
            }
        }

        public void SetLinePositions(Vector3[] positions)
        {
            lock (_positionsLock)
            {
                if (_lineRendererPositions.Length != positions.Length)
                {
                    _lineRendererPositions = new Vector3[positions.Length];       
                }
                Array.Copy(positions, _lineRendererPositions, positions.Length);
            }
        }

        public void ClearLinePositions()
        {
            lock (_positionsLock)
            {
                _lineRendererPositions = Array.Empty<Vector3>();
            }
        }
        

        public void SetPose(Pose pose)
        {
            lock (_positionsLock)
            {
                _pose = pose;
                _hasPose = true;
            }
        }

        public void UnsetPose()
        {
            lock (_positionsLock)
            {
                _hasPose = false;
            }
        }

        
        private void Update()
        {
            lock (_positionsLock)
            {
                UpdatePositionsInternal();
                UpdateSingleCubeInternal();
                UpdatePositionsLineInternal();
            }
        }

        private void UpdatePositionsLineInternal()
        {
            if (_lineRendererPositions.Length == 0)
            {
                lineRenderer.gameObject.SetActive(false);
                return;
            }
            lineRenderer.gameObject.SetActive(true);
            lineRenderer.positionCount = _lineRendererPositions.Length;
            lineRenderer.SetPositions(_lineRendererPositions);
        }

        private void UpdatePositionsInternal()
        {
            if (_gizmos.Count != _positions.Count)
            {
                RefreshGizmos();
            }

            for (var i = 0; i < _positions.Count; i++)
            {
                var gizmo = _gizmos[i];
                gizmo.SetPosition(Vector3.Scale(_positions[i], gizmoPositionScale));
                gizmo.SetLocalPosition(gizmoLocalScale);
            }
        }

        private void UpdateSingleCubeInternal()
        {
            if (_hasPose)
            {
                singleVisualizeCube.gameObject.SetActive(true);
                singleVisualizeCube.localPosition = _pose.position;
                singleVisualizeCube.localRotation = _pose.rotation;
                // 諸事情でテキトーに縮めておきます
                singleVisualizeCube.localScale = Vector3.one * 0.2f;
            }
            else if (_hasMatrix)
            {
                singleVisualizeCube.gameObject.SetActive(true);
                singleVisualizeCube.localPosition = _matrix.MultiplyPoint3x4(Vector3.zero);
                singleVisualizeCube.localRotation = _matrix.rotation;
                singleVisualizeCube.localScale = _matrix.lossyScale;
            }
            else
            {
                singleVisualizeCube.gameObject.SetActive(false);
            }
        }

        private void RefreshGizmos()
        {
            foreach (var gizmo in _gizmos)
            {
                Destroy(gizmo.gameObject);
            }
            _gizmos.Clear();

            for (var i = 0; i < _positions.Count; i++)
            {
                var gizmo = Instantiate(gizmoPrefab, gizmoParent);
                gizmo.SetIndex(i, _positions.Count);
                _gizmos.Add(gizmo);
            }
        }
    }
}
