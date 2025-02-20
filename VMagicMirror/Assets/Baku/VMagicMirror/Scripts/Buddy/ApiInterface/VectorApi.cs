namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// UnityEngineのVector2とほぼ同等のことが出来るようなデータ。
    /// </summary>
    public struct Vector2
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float x;
        public float y;
        
        // TODO: operator含めて色々と定義する
    }
    
    /// <summary>
    /// UnityEngineのVector3とほぼ同等のことが出来るようなデータ。
    /// </summary>
    public struct Vector3
    {
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x;
        public float y;
        public float z;

        // TODO: operator含めて色々と定義する
    }

    /// <summary>
    /// UnityEngineのQuaternionとほぼ同等のことが出来るようなデータ。
    /// </summary>
    public struct Quaternion
    {
        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public float x;
        public float y;
        public float z;
        public float w;

        // TODO: operator含めて色々と定義する
    }

    /// <summary>
    /// UnityEngineのPoseに似ているが、データ定義だけを持っているようなデータ。
    /// </summary>
    public struct Pose
    {
        public Pose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public Vector3 position;
        public Quaternion rotation;
    }
}

