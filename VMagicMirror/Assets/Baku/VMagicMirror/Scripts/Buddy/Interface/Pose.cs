namespace VMagicMirror.Buddy
{
    /// <summary>
    /// 位置と回転のペアを表すデータです。
    /// </summary>
    /// <remarks>
    /// UnityEngineのPose型に類似していますが、データ定義以外の機能はとくに含んでいません。
    /// </remarks>
    public struct Pose
    {
        /// <summary>
        /// 位置と回転を指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="rotation">回転</param>
        public Pose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        /// <summary> 位置を取得、設定します。 </summary>
        public Vector3 position;

        /// <summary> 回転を取得、設定します。 </summary>
        public Quaternion rotation;

        /// <summary>
        /// 位置が <see cref="Vector3.zero"/>、かつ回転が <see cref="Quaternion.identity"/> であるような値を取得します。
        /// </summary>
        public static Pose identity => new(Vector3.zero, Quaternion.identity);
    }
}

