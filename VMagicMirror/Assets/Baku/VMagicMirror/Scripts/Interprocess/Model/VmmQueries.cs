namespace Baku.VMagicMirror
{
    public static class VmmQueries
    {
        // Microphone
        public const string CurrentMicrophoneDeviceName = nameof(CurrentMicrophoneDeviceName);
        public const string MicrophoneDeviceNames = nameof(MicrophoneDeviceNames);

        // Camera
        public const string CurrentCameraPosition = nameof(CurrentCameraPosition);
        
        // Web Camera
        public const string CameraDeviceNames = nameof(CameraDeviceNames);
        
        // Image Quality
        public const string GetQualitySettingsInfo = nameof(GetQualitySettingsInfo);
        public const string ApplyDefaultImageQuality = nameof(ApplyDefaultImageQuality);

        // Word to Motion / GameInput
        public const string GetAvailableCustomMotionClipNames = nameof(GetAvailableCustomMotionClipNames);
    }
}

