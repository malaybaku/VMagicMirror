using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace mattatz.TransformControl {

	public class TransformControl : MonoBehaviour {

	    [System.Serializable]
	    class TransformData {
	        public Vector3 position;
	        public Quaternion rotation;
	        public Vector3 scale;
			Matrix4x4 matrix;

	        public TransformData(Vector3 p, Quaternion r, Vector3 s) {
	            position = p;
	            rotation = r;
	            scale = s;

				matrix = Matrix4x4.TRS(p, r, s);
	        }

	        public TransformData(Transform tr) : this(tr.position, tr.rotation, tr.localScale) {}

			public Vector3 TransformPoint (Vector3 p) {
				return matrix.MultiplyPoint(p);
				// return matrix * p;
			}
	    }

	    public enum TransformMode {
	        None, Translate, Rotate, Scale
	    };

	    public enum TransformDirection {
	        None, X, Y, Z
	    };

	    protected const string SHADER = "Hidden/Internal-Colored";
	    protected const float THRESHOLD = 10f;
	    protected const float HANDLER_SIZE = 0.15f;

	    protected Material material {
	        get {
	            if (_material == null)
	            {
	                var shader = Shader.Find(SHADER);
	                if (shader == null) Debug.LogErrorFormat("Shader not found : {0}", SHADER);
	                _material = new Material(shader);
	            }
	            return _material;
	        }
	    }

	    public TransformMode mode = TransformMode.Translate;
	    public bool global, useDistance;
        public float distance = 10f;

	    Color[] colors = new Color[]
	    {
		    new Color(0.8f, 0.4f, 0.2f),
		    new Color(0.2f, 0.7f, 0.2f),
		    new Color(0.5f, 0.5f, 0.8f), 
		    new Color(0.8f, 0.8f, 0.2f), 
	    };

		Dictionary<TransformDirection, Vector3> axes = new Dictionary<TransformDirection, Vector3>() {
			{ TransformDirection.X, Vector3.right },
			{ TransformDirection.Y, Vector3.up },
			{ TransformDirection.Z, Vector3.forward }
		};

		Matrix4x4[] matrices = new Matrix4x4[] {
	        Matrix4x4.TRS(Vector3.right, Quaternion.AngleAxis(90f, Vector3.back), Vector3.one),
	        Matrix4x4.TRS(Vector3.up, Quaternion.identity, Vector3.one),
	        Matrix4x4.TRS(Vector3.forward, Quaternion.AngleAxis(90f, Vector3.right), Vector3.one)
	    };

	    Material _material;

	    Vector3 start;
	    bool dragging;
	    TransformData prev;

	    Mesh cone;
	    Mesh cube;

	    TransformDirection selected = TransformDirection.None;

	    #region Circumference

	    const int SPHERE_RESOLUTION = 32;
	    List<Vector3> circumX;
	    List<Vector3> circumY;
	    List<Vector3> circumZ;

	    #endregion

	    void Awake() {
	        cone = CreateCone(5, 0.1f, HANDLER_SIZE);
	        cube = CreateCube(HANDLER_SIZE);

	        GetCircumference(SPHERE_RESOLUTION, out circumX, out circumY, out circumZ);
	    }

        /*
        // Usage: Call Control() method in Update() loop 
	    void Update() {
            Control();
	    }
        */

        public void Control () {
	        if (Input.GetMouseButtonDown(0)) {
	            dragging = true;
	            start = Input.mousePosition;
	            prev = new TransformData(transform);
                Pick();
	        } else if (Input.GetMouseButtonUp(0)) {
	            dragging = false;
				selected = TransformDirection.None;
	        }

            if(dragging) {
                Drag();
            }
        }

	    public bool Pick () {
	        return Pick(Input.mousePosition);
	    }

	    public bool Pick (Vector3 mouse) {
	        selected = TransformDirection.None;

	        switch(mode) {
	            case TransformMode.Translate:
	            case TransformMode.Scale:
	                return PickOrthogonal(mouse);
	            case TransformMode.Rotate:
	                return PickSphere(mouse);
	        }

	        return false;
	    }

        Matrix4x4 GetTranform()
        {
            float scale = 1f;
            if(useDistance)
            {
                var d = (Camera.main.transform.position - transform.position).magnitude;
                scale = d / distance;
            }
			return Matrix4x4.TRS(transform.position, global ? Quaternion.identity : transform.rotation, Vector3.one * scale);
        }

	    bool PickOrthogonal (Vector3 mouse) {
	        var cam = Camera.main;

			var matrix = GetTranform();

			var origin = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.zero)).xy();
	        var right = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.right)).xy() - origin;
	        var rightHead = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.right * (1f + HANDLER_SIZE))).xy() - origin;
	        var up = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.up)).xy() - origin;
	        var upHead = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.up * (1f + HANDLER_SIZE))).xy() - origin;
	        var forward = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.forward)).xy() - origin;
	        var forwardHead = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.forward * (1f + HANDLER_SIZE))).xy() - origin;
	        var v = mouse.xy() - origin;
	        var vl = v.magnitude;

	        // Add THRESHOLD to each magnitude to ignore a direction.

	        var xl = v.Orth(right).magnitude;
			if(Vector2.Dot(v, right) <= -float.Epsilon || vl > rightHead.magnitude) xl += THRESHOLD;

	        var yl = v.Orth(up).magnitude;
	        if(Vector2.Dot(v, up) <= -float.Epsilon || vl > upHead.magnitude) yl += THRESHOLD;

	        var zl = v.Orth(forward).magnitude;
			if(Vector2.Dot(v, forward) <= -float.Epsilon || vl > forwardHead.magnitude) zl += THRESHOLD;

	        if (xl < yl && xl < zl && xl < THRESHOLD) {
	            selected = TransformDirection.X;
	        } else if (yl < xl && yl < zl && yl < THRESHOLD) {
	            selected = TransformDirection.Y;
	        } else if (zl < xl && zl < yl && zl < THRESHOLD) {
	            selected = TransformDirection.Z;
	        }

	        return selected != TransformDirection.None;
	    }

	    bool PickSphere(Vector3 mouse) {
	        var cam = Camera.main;

			var matrix = GetTranform();

	        var v = mouse.xy();
			var x = circumX.Select(p => cam.WorldToScreenPoint(matrix.MultiplyPoint(p)).xy()).ToList();
	        var y = circumY.Select(p => cam.WorldToScreenPoint(matrix.MultiplyPoint(p)).xy()).ToList();
	        var z = circumZ.Select(p => cam.WorldToScreenPoint(matrix.MultiplyPoint(p)).xy()).ToList();

	        float xl, yl, zl;
	        xl = yl = zl = float.MaxValue;
	        for(int i = 0; i < SPHERE_RESOLUTION; i++) {
	            xl = Mathf.Min(xl, (v - x[i]).magnitude);
	            yl = Mathf.Min(yl, (v - y[i]).magnitude);
	            zl = Mathf.Min(zl, (v - z[i]).magnitude);
	        }

	        if (xl < yl && xl < zl && xl < THRESHOLD) {
	            selected = TransformDirection.X;
	        } else if (yl < xl && yl < zl && yl < THRESHOLD) {
	            selected = TransformDirection.Y;
	        } else if (zl < xl && zl < yl && zl < THRESHOLD) {
	            selected = TransformDirection.Z;
	        }

	        return selected != TransformDirection.None;
	    }

	    void GetCircumference (int resolution, out List<Vector3> x, out List<Vector3> y, out List<Vector3> z) {
	        x = new List<Vector3>();
	        y = new List<Vector3>();
	        z = new List<Vector3>();

	        var pi2 = Mathf.PI * 2f;
	        for(int i = 0; i < resolution; i++) {
	            var r = (float)i / resolution * pi2;
	            x.Add(new Vector3(0f, Mathf.Cos(r), Mathf.Sin(r)));
	            y.Add(new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r)));
	            z.Add(new Vector3(Mathf.Cos(r), Mathf.Sin(r), 0f));
	        }
	    }

		bool GetStartProj (out Vector3 proj) {
			proj = default(Vector3);

			var plane = new Plane((Camera.main.transform.position - prev.position).normalized, prev.position);
			var ray = Camera.main.ScreenPointToRay(start);
			float distance;
			if(plane.Raycast(ray, out distance)) {
				var point = ray.GetPoint(distance);
				var axis = global ? axes[selected] : prev.rotation * axes[selected];
				var dir = point - prev.position;
				proj = Vector3.Project(dir, axis.normalized);
				return true;
			}
			return false;
		}

		float GetStartDistance () {
			Vector3 proj;
			if(GetStartProj(out proj)) {
				return proj.magnitude;
			}
			return 0f;
		}

        void Drag() {
	        switch (mode) {
	            case TransformMode.Translate:
	                Translate();
	                break;
	            case TransformMode.Rotate:
	                Rotate();
	                break;
	            case TransformMode.Scale:
	                Scale();
	                break;
	        }
        }

	    void Translate() {
	        if (selected == TransformDirection.None) return;

			var plane = new Plane((Camera.main.transform.position - prev.position).normalized, prev.position);
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			float distance;
			if(plane.Raycast(ray, out distance)) {
				var point = ray.GetPoint(distance);
				var axis = global ? axes[selected] : prev.rotation * axes[selected];
				var dir = point - prev.position;
				var proj = Vector3.Project(dir, axis.normalized);

				Vector3 start;
				if(GetStartProj(out start)) {
					var offset = start.magnitude;
					var cur = proj.magnitude;
					if(Vector3.Dot(start, proj) >= 0f) {
						proj = (cur - offset) * proj.normalized;
					} else {
						proj = (cur + offset) * proj.normalized;
					}
				}

				transform.position = prev.position + proj;
			}
		}

	    void Rotate() {
			if (selected == TransformDirection.None) return;

			var matrix = Matrix4x4.TRS(prev.position, global ? Quaternion.identity : prev.rotation,  Vector3.one);

			var cur = Input.mousePosition.xy();
			var cam = Camera.main;
			var origin = cam.WorldToScreenPoint(matrix.MultiplyPoint(Vector3.zero)).xy();
			var axis = cam.WorldToScreenPoint(matrix.MultiplyPoint(axes[selected])).xy();
			var perp = (origin - axis).Perp().normalized;
			var dir = (cur - start.xy());
			var proj = dir.Project(perp);

			var rotateAxis = axes[selected];
			if(global) rotateAxis = Quaternion.Inverse(prev.rotation) * rotateAxis;
			transform.rotation = prev.rotation * Quaternion.AngleAxis(proj.magnitude * (Vector2.Dot(dir, perp) > 0f ? 1f : -1f), rotateAxis);
		}

	    void Scale() {
	        if (selected == TransformDirection.None) return;

			var plane = new Plane((Camera.main.transform.position - transform.position).normalized, prev.position);
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			float distance;
			if(plane.Raycast(ray, out distance)) {
				var point = ray.GetPoint(distance);
				var axis = global ? axes[selected] : prev.rotation * axes[selected];
				var dir = point - prev.position;
				var proj = Vector3.Project(dir, axis.normalized);
				var offset = GetStartDistance();

				var mag = 0f;
				if(proj.magnitude < offset) {
					mag = 1f - (offset - proj.magnitude) / offset;
				} else {
					mag = proj.magnitude / offset;
				}

				var scale = transform.localScale;
				switch(selected) {
				case TransformDirection.X:
					scale.x = prev.scale.x * mag;
					break;
				case TransformDirection.Y:
					scale.y = prev.scale.y * mag;
					break;
				case TransformDirection.Z:
					scale.z = prev.scale.z * mag;
					break;
				}
				transform.localScale = scale;
			}

	    }

	    void OnRenderObject() {
	        if (mode == TransformMode.None) return;

	        GL.PushMatrix();

            GL.MultMatrix(GetTranform());

	        switch (mode) {
	            case TransformMode.Translate:
	                DrawTranslate();
	                break;

	            case TransformMode.Rotate:
	                DrawRotate();
	                break;

	            case TransformMode.Scale:
	                DrawScale();
	                break;
	        }

	        GL.PopMatrix();
	    }

	    void DrawLine (Vector3 start, Vector3 end, Color color) {
	        GL.Begin(GL.LINES);
	        GL.Color(color);
	        GL.Vertex(start);
	        GL.Vertex(end);
	        GL.End();
	    }

	    void DrawMesh (Mesh mesh, Matrix4x4 m, Color color) {
	        GL.Begin(GL.TRIANGLES);
	        GL.Color(color);

	        var vertices = mesh.vertices;
	        for (int i = 0, n = vertices.Length; i < n; i++) {
	            vertices[i] = m.MultiplyPoint(vertices[i]);
	        }

	        var triangles = mesh.triangles;
	        for (int i = 0, n = triangles.Length; i < n; i += 3) {
	            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
	            GL.Vertex(vertices[a]);
	            GL.Vertex(vertices[b]);
	            GL.Vertex(vertices[c]);
	        }

	        GL.End();
	    }

	    void DrawTranslate () {
			material.SetInt("_ZTest", (int)CompareFunction.Always);
	        material.SetPass(0);

	        // x axis
	        var color = selected == TransformDirection.X ? colors[3] : colors[0];
	        DrawLine(Vector3.zero, Vector3.right, color);
	        DrawMesh(cone, matrices[0], color);

	        // y axis
	        color = selected == TransformDirection.Y ? colors[3] : colors[1];
	        DrawLine(Vector3.zero, Vector3.up, color);
	        DrawMesh(cone, matrices[1], color);

	        // z axis
	        color = selected == TransformDirection.Z ? colors[3] : colors[2];
	        DrawLine(Vector3.zero, Vector3.forward, color);
	        DrawMesh(cone, matrices[2], color);
	    }

	    void DrawRotate () {
			material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
	        material.SetPass(0);

	        // x axis
	        GL.Begin(GL.LINES);
	        var color = selected == TransformDirection.X ? colors[3] : colors[0];
	        GL.Color(color);
	        for(int i = 0; i < SPHERE_RESOLUTION; i++) {
	            var cur = circumX[i];
	            var next = circumX[(i + 1) % SPHERE_RESOLUTION];
	            GL.Vertex(cur);
	            GL.Vertex(next);
	        }
	        GL.End();

	        GL.Begin(GL.LINES);
	        color = selected == TransformDirection.Y ? colors[3] : colors[1];
	        GL.Color(color);
	        material.SetPass(0);
	        for(int i = 0; i < SPHERE_RESOLUTION; i++) {
	            var cur = circumY[i];
	            var next = circumY[(i + 1) % SPHERE_RESOLUTION];
	            GL.Vertex(cur);
	            GL.Vertex(next);
	        }
	        GL.End();

	        GL.Begin(GL.LINES);
	        color = selected == TransformDirection.Z ? colors[3] : colors[2];
	        GL.Color(color);
	        material.SetPass(0);
	        for(int i = 0; i < SPHERE_RESOLUTION; i++) {
	            var cur = circumZ[i];
	            var next = circumZ[(i + 1) % SPHERE_RESOLUTION];
	            GL.Vertex(cur);
	            GL.Vertex(next);
	        }
	        GL.End();
	    }

	    void DrawScale () {
			material.SetInt("_ZTest", (int)CompareFunction.Always);
	        material.SetPass(0);

	        // x axis
	        var color = selected == TransformDirection.X ? colors[3] : colors[0];
	        DrawLine(Vector3.zero, Vector3.right, color);
	        DrawMesh(cube, matrices[0], color);

	        // y axis
	        color = selected == TransformDirection.Y ? colors[3] : colors[1];
	        DrawLine(Vector3.zero, Vector3.up, color);
	        DrawMesh(cube, matrices[1], color);

	        // z axis
	        color = selected == TransformDirection.Z ? colors[3] : colors[2];
	        DrawLine(Vector3.zero, Vector3.forward, color);
	        DrawMesh(cube, matrices[2], color);
	    }

	    #region Mesh

	    Mesh CreateCone(int subdivisions, float radius, float height)
	    {
	        Mesh mesh = new Mesh();

	        Vector3[] vertices = new Vector3[subdivisions + 2];
	        int[] triangles = new int[(subdivisions * 2) * 3];

	        vertices[0] = Vector3.zero;
	        for (int i = 0, n = subdivisions - 1; i < subdivisions; i++)
	        {
	            float ratio = (float)i / n;
	            float r = ratio * (Mathf.PI * 2f);
	            float x = Mathf.Cos(r) * radius;
	            float z = Mathf.Sin(r) * radius;
	            vertices[i + 1] = new Vector3(x, 0f, z);
	        }
	        vertices[subdivisions + 1] = new Vector3(0f, height, 0f);

	        // construct bottom
	        for (int i = 0, n = subdivisions - 1; i < n; i++)
	        {
	            int offset = i * 3;
	            triangles[offset] = 0;
	            triangles[offset + 1] = i + 1;
	            triangles[offset + 2] = i + 2;
	        }

	        // construct sides
	        int bottomOffset = subdivisions * 3;
	        for (int i = 0, n = subdivisions - 1; i < n; i++)
	        {
	            int offset = i * 3 + bottomOffset;
	            triangles[offset] = i + 1;
	            triangles[offset + 1] = subdivisions + 1;
	            triangles[offset + 2] = i + 2;
	        }

	        mesh.vertices = vertices;
	        mesh.triangles = triangles;
	        return mesh;
	    }

	    Mesh CreateCube(float size) {
	        var mesh = new Mesh();

	        var hsize = size * 0.5f;
	        mesh.vertices = new Vector3[] {
	            new Vector3 (-hsize, -hsize, -hsize),
	            new Vector3 ( hsize, -hsize, -hsize),
	            new Vector3 ( hsize,  hsize, -hsize),
	            new Vector3 (-hsize,  hsize, -hsize),
	            new Vector3 (-hsize,  hsize,  hsize),
	            new Vector3 ( hsize,  hsize,  hsize),
	            new Vector3 ( hsize, -hsize,  hsize),
	            new Vector3 (-hsize, -hsize,  hsize),
	        };

	        mesh.triangles = new int[] {
	            0, 2, 1, //face front
				0, 3, 2,
	            2, 3, 4, //face top
				2, 4, 5,
	            1, 2, 5, //face right
				1, 5, 6,
	            0, 7, 4, //face left
				0, 4, 3,
	            5, 4, 7, //face back
				5, 7, 6,
	            0, 6, 7, //face bottom
				0, 1, 6
	        };

	        return mesh;
	    }

	    #endregion

	}

}

