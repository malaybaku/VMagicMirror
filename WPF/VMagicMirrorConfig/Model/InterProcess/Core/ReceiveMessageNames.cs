namespace Baku.VMagicMirrorConfig
{
    static class ReceiveMessageNames
    {
        public const string ModelNameConfirmedOnLoad = nameof(ModelNameConfirmedOnLoad);

        public const string RequestShowError = nameof(RequestShowError);
        public const string SetUnityProcessId = nameof(SetUnityProcessId);
        public const string CloseConfigWindow = nameof(CloseConfigWindow);
        public const string SetCalibrationFaceData = nameof(SetCalibrationFaceData);
        public const string AutoAdjustResults = nameof(AutoAdjustResults);
        public const string UpdateDeviceLayout = nameof(UpdateDeviceLayout);
        public const string UpdateHandDownPose = nameof(UpdateHandDownPose);
        public const string MicrophoneVolumeLevel = nameof(MicrophoneVolumeLevel);

        public const string ExtraBlendShapeClipNames = nameof(ExtraBlendShapeClipNames);

        public const string MidiNoteOn = nameof(MidiNoteOn);

        public const string ExTrackerCalibrateComplete = nameof(ExTrackerCalibrateComplete);
        public const string ExTrackerSetPerfectSyncMissedClipNames = nameof(ExTrackerSetPerfectSyncMissedClipNames);
        public const string ExTrackerSetIFacialMocapTroubleMessage = nameof(ExTrackerSetIFacialMocapTroubleMessage);

        public const string SetHandTrackingResult = nameof(SetHandTrackingResult);

        public const string SetModelDoesNotSupportPen = nameof(SetModelDoesNotSupportPen);

        public const string UpdateAccessoryLayouts = nameof(UpdateAccessoryLayouts);

        public const string VRoidModelLoadCompleted = nameof(VRoidModelLoadCompleted);
        public const string VRoidModelLoadCanceled = nameof(VRoidModelLoadCanceled);

        public const string VRM10SpecifiedButNotSupported = nameof(VRM10SpecifiedButNotSupported);
    }
}
