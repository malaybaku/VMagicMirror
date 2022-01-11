using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace mattatz.TransformControl
{
    public class TransformControl : MonoBehaviour
    {
        static class ConflictResolver
        {
            private static readonly HashSet<TransformControl> _controls = new HashSet<TransformControl>();

            public static void Register(TransformControl tc) => _controls.Add(tc);
            public static void Unregister(TransformControl tc) => _controls.Remove(tc);

            //return true, when other control is already being dragged
            public static bool DraggingOtherControl(TransformControl tc) => _controls.Any(
                c =>
                    c != null && c != tc &&
                    c.enabled && c._selected != TransformDirection.None
            );
        }

        public enum TransformMode
        {
            None,
            Translate,
            Rotate,
            Scale,
        }

        public enum TransformDirection
        {
            None,
            X,
            Y,
            Z,
        }

        [Serializable]
        private class TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public TransformData(Vector3 p, Quaternion r, Vector3 s)
            {
                position = p;
                rotation = r;
                scale = s;
            }

            public TransformData(Transform tr) : this(tr.position, tr.rotation, tr.localScale)
            {
            }
        }

        private const float PickThreshold = 20f;
        private const float HandlerSize = 0.15f;
        private const int SphereResolution = 32;

        private static readonly List<Vector3> CircleX = new List<Vector3>(SphereResolution);
        private static readonly List<Vector3> CircleY = new List<Vector3>(SphereResolution);
        private static readonly List<Vector3> CircleZ = new List<Vector3>(SphereResolution);
        private static bool _circleInitialized = false;        

        private static readonly IReadOnlyDictionary<TransformDirection, Vector3> Axes = new Dictionary<TransformDirection, Vector3>
        {
            [TransformDirection.X] = Vector3.right,
            [TransformDirection.Y] = Vector3.up,
            [TransformDirection.Z] = Vector3.forward,
        };

        public TransformMode mode = TransformMode.Translate;
        public bool global;
        public bool useDistance;
        public float distance = 12f;
        [SerializeField] private bool xyPlaneMode;
        [SerializeField] private GizmoRenderer gizmoRenderer;
        public bool XyPlaneMode 
        {
            get => xyPlaneMode;
            set => xyPlaneMode = value;
        }

        private readonly Vector3[] _xBuffer = new Vector3[SphereResolution];
        private readonly Vector3[] _yBuffer = new Vector3[SphereResolution];
        private readonly Vector3[] _zBuffer = new Vector3[SphereResolution];

        private Camera _cam;
        private Vector3 _start;
        private bool _dragging;
        private TransformData _prev;
        private TransformDirection _selected = TransformDirection.None;
        private TransformDirection _hover = TransformDirection.None;

        public bool IsDragging => _selected != TransformDirection.None && _dragging;
        public bool HasMouseOverContent => _hover != TransformDirection.None && !_dragging;

        /// <summary>
        /// Set true to manually update gizmo, in the case control target moves on non-standard timing
        /// </summary>
        public bool AutoUpdateGizmo { get; set; } = true;

        /// <summary>
        /// Fire when drag operation has ended
        /// </summary>
        public Action<TransformMode> DragEnded;

        private void InitializeCircumferences()
        {
            if (_circleInitialized)
            {
                return;
            }

            var pi2 = Mathf.PI * 2f;
            for (int i = 0; i < SphereResolution; i++)
            {
                var r = (float) i / SphereResolution * pi2;
                CircleX.Add(new Vector3(0f, Mathf.Cos(r), Mathf.Sin(r)));
                CircleY.Add(new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r)));
                CircleZ.Add(new Vector3(Mathf.Cos(r), Mathf.Sin(r), 0f));
            }
            _circleInitialized = true;
        }

        private void Awake() => InitializeCircumferences();

        private void Start()
        {
            _cam = Camera.main;
            EnsureGizmoRenderer();
            gizmoRenderer.Target = transform;
            gizmoRenderer.TargetCamera = _cam;
        }

        private void Update()
        {
            if (AutoUpdateGizmo)
            {
                UpdateGizmo();
            }
        }

        private void OnEnable()
        {
            ConflictResolver.Register(this);
            EnsureGizmoRenderer();
            gizmoRenderer.enabled = true;
            UpdateGizmo();
        }

        private void OnDisable()
        {
            _hover = TransformDirection.None;
            ConflictResolver.Unregister(this);
            if (gizmoRenderer != null)
            {
                gizmoRenderer.enabled = false;
                gizmoRenderer.SetMode(TransformMode.None);
            }
        }

        private void OnDestroy()
        {
            ConflictResolver.Unregister(this);
            if (gizmoRenderer != null)
            {
                Destroy(gizmoRenderer.gameObject);
                gizmoRenderer = null;
            }
        }

        public void Control()
        {
            CheckMouseOver(Input.mousePosition);
            if (Input.GetMouseButtonDown(0))
            {
                _dragging = true;
                _start = Input.mousePosition;
                _prev = new TransformData(transform);
                Pick(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
                if (mode != TransformMode.None && _selected != TransformDirection.None)
                {
                    DragEnded?.Invoke(mode);
                }

                _selected = TransformDirection.None;
            }

            if (_dragging)
            {
                Drag();
            }
        }

        public void RequestUpdateGizmo() => UpdateGizmo();

        private void UpdateGizmo()
        {
            gizmoRenderer.SetMode(mode);
            if (mode == TransformMode.None)
            {
                return;
            }

            if (HasMouseOverContent)
            {
                gizmoRenderer.SetDirection(_hover, true);
            }
            else
            {
                gizmoRenderer.SetDirection(_selected, false);
            }
            
            gizmoRenderer.SetUseWorldCoord(global);
            gizmoRenderer.SetXyPlaneMode(XyPlaneMode);

            if (useDistance)
            {
                gizmoRenderer.SetDistance(distance);
            }
            else
            {
                gizmoRenderer.UnsetDistance();
            }
        }

        private void EnsureGizmoRenderer()
        {
            if (gizmoRenderer == null)
            {
                var prefab = Resources.Load<GizmoRenderer>("TransformControlGizmoRenderer");
                gizmoRenderer = Instantiate(prefab);
                gizmoRenderer.Target = transform;

                //ここは一応やってるが深い意味はない: 表示するときは逐次正しい位置に移動させるため
                var gt = gizmoRenderer.transform;
                gt.localPosition = Vector3.zero;
                gt.localRotation = Quaternion.identity;
                gt.localScale = Vector3.one;
            }
        }

        private void CheckMouseOver(Vector3 mouse)
        {
            _hover = TransformDirection.None;
            if (IsDragging || ConflictResolver.DraggingOtherControl(this))
            {
                return;
            }

            if (!gizmoRenderer.CheckBoundingBox(mouse))
            {
                return;
            }

            switch (mode)
            {
                case TransformMode.Translate:
                case TransformMode.Scale:
                    _hover = PickOrthogonal(mouse);
                    return;
                case TransformMode.Rotate:
                    _hover = PickSphere(mouse);
                    return;
                case TransformMode.None:
                default:
                    return;
            }

        }
        
        private void Pick(Vector3 mouse)
        {
            _selected = TransformDirection.None;
            if (ConflictResolver.DraggingOtherControl(this))
            {
                //avoid pick, when other control is being controlled
                return;
            }

            switch (mode)
            {
                case TransformMode.Translate:
                case TransformMode.Scale:
                    _selected = PickOrthogonal(mouse);
                    return;
                case TransformMode.Rotate:
                    _selected = PickSphere(mouse);
                    return;
                case TransformMode.None:
                default:
                    return;
            }
        }

        private Matrix4x4 GetTransform()
        {
            float scale = 1f;
            if (useDistance)
            {
                var d = (_cam.transform.position - transform.position).magnitude;
                scale = d / distance;
            }

            return Matrix4x4.TRS(
                transform.position, 
                global ? Quaternion.identity : transform.rotation,
                Vector3.one * scale);
        }

        private TransformDirection PickOrthogonal(Vector3 mouse)
        {
            var cam = _cam;

            var matrix = GetTransform();

            var origin = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.zero)).Xy();
            var right = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.right)).Xy() - origin;
            var rightHead = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.right * (1f + HandlerSize))).Xy() -
                            origin;
            var up = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.up)).Xy() - origin;
            var upHead = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.up * (1f + HandlerSize))).Xy() - origin;
            var forward = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.forward)).Xy() - origin;
            var forwardHead = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.forward * (1f + HandlerSize))).Xy() -
                              origin;
            var v = mouse.Xy() - origin;
            var vl = v.magnitude;

            // Add THRESHOLD to each magnitude to ignore a direction.

            var xl = v.Orthogonal(right).magnitude;
            if (Vector2.Dot(v, right) <= -float.Epsilon || vl > rightHead.magnitude) xl += PickThreshold;

            var yl = v.Orthogonal(up).magnitude;
            if (Vector2.Dot(v, up) <= -float.Epsilon || vl > upHead.magnitude) yl += PickThreshold;

            var zl = v.Orthogonal(forward).magnitude;
            if (Vector2.Dot(v, forward) <= -float.Epsilon || vl > forwardHead.magnitude) zl += PickThreshold;

            if (xl < yl && xl < zl && xl < PickThreshold)
            {
                return TransformDirection.X;
            }
            
            if (yl < xl && yl < zl && yl < PickThreshold)
            {
                return TransformDirection.Y;
            }
            
            if (zl < xl && zl < yl && zl < PickThreshold)
            {
                return TransformDirection.Z;
            }

            return TransformDirection.None;
        }

        private TransformDirection PickSphere(Vector3 mouse)
        {
            var cam = _cam;

            var matrix = GetTransform();

            var v = mouse.Xy();
            for (int i = 0; i < SphereResolution; i++)
            {
                _xBuffer[i] = cam.WorldToScreenPoint(matrix.MultiplyPoint(CircleX[i]));
                _yBuffer[i] = cam.WorldToScreenPoint(matrix.MultiplyPoint(CircleY[i]));
                _zBuffer[i] = cam.WorldToScreenPoint(matrix.MultiplyPoint(CircleZ[i]));
            }

            var xDepthMean = _xBuffer.Sum(a => a.z) / _xBuffer.Length;
            var yDepthMean = _yBuffer.Sum(a => a.z) / _yBuffer.Length;
            var zDepthMean = _zBuffer.Sum(a => a.z) / _zBuffer.Length;
            //NOTE: Add margin for the case where gizmo circle normal matches to camera ray
            const float DepthMargin = 0.2f;

            var xl = float.MaxValue;
            var yl = float.MaxValue;
            var zl = float.MaxValue;
            for (var i = 0; i < SphereResolution; i++)
            {
                if (_xBuffer[i].z < xDepthMean + DepthMargin)
                {
                    xl = Mathf.Min(xl, (v - _xBuffer[i].Xy()).magnitude);
                }
                
                if (_yBuffer[i].z < yDepthMean + DepthMargin)
                {
                    yl = Mathf.Min(yl, (v - _yBuffer[i].Xy()).magnitude);
                }

                if (_zBuffer[i].z < zDepthMean + DepthMargin)
                {
                    zl = Mathf.Min(zl, (v - _zBuffer[i].Xy()).magnitude);
                }
            }

            if (xl < yl && xl < zl && xl < PickThreshold)
            {
                return TransformDirection.X;
            }
            
            if (yl < xl && yl < zl && yl < PickThreshold)
            {
                return TransformDirection.Y;
            }
            
            if (zl < xl && zl < yl && zl < PickThreshold)
            {
                // _selected = TransformDirection.Z;
                return TransformDirection.Z;
            }

            return TransformDirection.None;
        }

        private bool GetStartProj(out Vector3 proj)
        {
            proj = default;

            var plane = new Plane((_cam.transform.position - _prev.position).normalized, _prev.position);
            var ray = _cam.ScreenPointToRay(_start);
            if (plane.Raycast(ray, out var planeDistance))
            {
                var point = ray.GetPoint(planeDistance);
                var axis = global ? Axes[_selected] : _prev.rotation * Axes[_selected];
                var dir = point - _prev.position;
                proj = Vector3.Project(dir, axis.normalized);
                return true;
            }

            return false;
        }

        private float GetStartDistance() => GetStartProj(out var proj) ? proj.magnitude : 0f;

        private void Drag()
        {
            switch (mode)
            {
                case TransformMode.Translate:
                    Translate();
                    break;
                case TransformMode.Rotate:
                    Rotate();
                    break;
                case TransformMode.Scale:
                    Scale();
                    break;
                case TransformMode.None:
                default:
                    //何もしない
                    break;
            }
        }

        private void Translate()
        {
            if (_selected == TransformDirection.None)
            {
                return;
            }

            if (XyPlaneMode && _selected == TransformDirection.Z)
            {
                return;
            }

            var plane = new Plane((_cam.transform.position - _prev.position).normalized, _prev.position);
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (!plane.Raycast(ray, out var planeDistance))
            {
                return;
            }

            var point = ray.GetPoint(planeDistance);
            var axis = global ? Axes[_selected] : _prev.rotation * Axes[_selected];
            var dir = point - _prev.position;
            var proj = Vector3.Project(dir, axis.normalized);

            if (GetStartProj(out var startProj))
            {
                var offset = startProj.magnitude;
                var cur = proj.magnitude;
                if (Vector3.Dot(startProj, proj) >= 0f)
                {
                    proj = (cur - offset) * proj.normalized;
                }
                else
                {
                    proj = (cur + offset) * proj.normalized;
                }
            }

            transform.position = _prev.position + proj;
        }

        private void Rotate()
        {
            if (_selected == TransformDirection.None)
            {
                return;
            }

            if (XyPlaneMode && _selected != TransformDirection.Z)
            {
                return;
            }

            var matrix = Matrix4x4.TRS(_prev.position, global ? Quaternion.identity : _prev.rotation, Vector3.one);

            var cur = Input.mousePosition.Xy();
            var origin = _cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.zero)).Xy();
            var axis = _cam.WorldToScreenPoint(matrix.MultiplyPoint(Axes[_selected])).Xy();
            var perp = (origin - axis).Perp().normalized;
            var dir = (cur - _start.Xy());
            var proj = dir.Project(perp);

            var rotateAxis = Axes[_selected];
            if (global) rotateAxis = Quaternion.Inverse(_prev.rotation) * rotateAxis;
            transform.rotation = _prev.rotation *
                                 Quaternion.AngleAxis(proj.magnitude * (Vector2.Dot(dir, perp) > 0f ? 1f : -1f),
                                     rotateAxis);
        }

        private void Scale()
        {
            if (_selected == TransformDirection.None)
            {
                return;
            }

            if (XyPlaneMode && _selected == TransformDirection.Z)
            {
                return;
            }

            var plane = new Plane((_cam.transform.position - transform.position).normalized, _prev.position);
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (!plane.Raycast(ray, out var planeDistance))
            {
                return;
            }
            
            var point = ray.GetPoint(planeDistance);
            var axis = global ? Axes[_selected] : _prev.rotation * Axes[_selected];
            var dir = point - _prev.position;
            var proj = Vector3.Project(dir, axis.normalized);
            var offset = GetStartDistance();

            var mag = 0f;
            if (proj.magnitude < offset)
            {
                mag = 1f - (offset - proj.magnitude) / offset;
            }
            else
            {
                mag = proj.magnitude / offset;
            }

            var scale = transform.localScale;
            switch (_selected)
            {
                case TransformDirection.X:
                    scale.x = _prev.scale.x * mag;
                    break;
                case TransformDirection.Y:
                    scale.y = _prev.scale.y * mag;
                    break;
                case TransformDirection.Z:
                    scale.z = _prev.scale.z * mag;
                    break;
                case TransformDirection.None:
                default:
                    //do nothing
                    break;
            }

            transform.localScale = scale;
        }
    }
}