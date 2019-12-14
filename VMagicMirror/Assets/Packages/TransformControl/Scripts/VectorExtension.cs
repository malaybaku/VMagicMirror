using UnityEngine;

namespace mattatz.TransformControl {

    public static class VectorExtension {

        public static Vector2 xy (this Vector3 v) {
            return new Vector2(v.x, v.y);
        }

        // orthogonal vector v2 to v1
        public static Vector2 Orth (this Vector2 from, Vector2 to) {
            return from.Project(to) - from;
        }

        // projection vector v2 to v1
        public static Vector2 Project(this Vector2 from, Vector2 to) {
            return to * (Vector2.Dot(to, from) / Mathf.Pow(to.magnitude, 2f));
        }

		public static Vector2 Perp (this Vector2 v) {
			return new Vector2(v.y, -v.x);
		}
        
    }

}


