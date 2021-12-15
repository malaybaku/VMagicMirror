namespace Baku.VMagicMirrorConfig
{

    public struct Vector3
    {
        public Vector3(float x, float y, float z) : this()
        {
            (X, Y, Z) = (x, y, z);
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3 WithX(float v) => new Vector3(v, Y, Z);
        public Vector3 WithY(float v) => new Vector3(X, v, Z);
        public Vector3 WithZ(float v) => new Vector3(X, Y, v);

        public static Vector3 Zero() => new Vector3();
        public static Vector3 One() => new Vector3 { X = 1f, Y = 1f, Z = 1f, };
    }
}
