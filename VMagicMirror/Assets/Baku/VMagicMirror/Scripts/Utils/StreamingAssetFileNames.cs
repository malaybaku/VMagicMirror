namespace Baku.VMagicMirror
{
    public static class StreamingAssetFileNames
    {
        /// <summary> DlibFaceLandmarkDetectorで使うモデルファイル名 </summary>
        public const string DlibFaceTrackingDataFileName = "sp_human_face_17.dat";
        /// <summary>
        /// VRMLoaderUIのローカライズファイル。メインのコードからは直接見に行かないが、ビルドには含める
        /// </summary>
        public const string LoaderUiFolder = "VRMLoaderUI";

        public const string MediaPipeTrackerFolder = "MediaPipeTracker";

    }
}
