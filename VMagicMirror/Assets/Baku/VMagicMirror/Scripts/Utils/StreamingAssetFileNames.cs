namespace Baku.VMagicMirror
{
    public static class StreamingAssetFileNames
    {
        /// <summary> VRMLoaderUIのローカライズファイル。メインのコードからは直接見に行かないが、ビルドには含める </summary>
        public const string LoaderUiFolder = "VRMLoaderUI";

        /// <summary> デフォルト定義のサブキャラのフォルダ。ユーザー定義サブキャラの VMM_Files/Buddy に相当するようなフォルダ</summary>
        public const string DefaultBuddyFolder = "DefaultBuddy";
        
        /// <summary> サブキャラのAPI定義に関するxml docファイル。実行時にVMM_Files以下にコピーして使う </summary>
        public const string BuddyApiXmlDocFileName = "VMagicMirror.Buddy.xml";

        public const string MediaPipeTrackerFolder = "MediaPipeTracker";
    }
}
