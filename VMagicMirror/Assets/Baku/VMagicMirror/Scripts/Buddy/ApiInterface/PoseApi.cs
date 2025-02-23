namespace Baku.VMagicMirror.Buddy.Api.Interface
{
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

