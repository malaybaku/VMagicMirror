namespace Baku.VMagicMirror
{
    public static class MessageCommandNames
    {
        //NOTE: 順番をほぼVMagicMirrorConfigのMessageFactoryクラスに合わせているが、そこまで重要ではない

        public const string Language = nameof(Language);

        // Input 
        public const string KeyDown = nameof(KeyDown);
        public const string MouseMoved = nameof(MouseMoved);
        public const string MouseButton = nameof(MouseButton);

        // Load VRM
        public const string OpenVrmPreview = nameof(OpenVrmPreview);
        public const string OpenVrm = nameof(OpenVrm);
        public const string CancelLoadVrm = nameof(CancelLoadVrm);
        public const string AccessToVRoidHub = nameof(AccessToVRoidHub);

        // Window
        public const string Chromakey = nameof(Chromakey);
        public const string WindowFrameVisibility = nameof(WindowFrameVisibility);
        public const string IgnoreMouse = nameof(IgnoreMouse);
        public const string TopMost = nameof(TopMost);
        public const string WindowDraggable = nameof(WindowDraggable);
        public const string MoveWindow = nameof(MoveWindow);

        // Motion

        // Motion, Hand
        public const string LengthFromWristToTip = nameof(LengthFromWristToTip);
        public const string LengthFromWristToPalm = nameof(LengthFromWristToPalm);
        public const string HandYOffsetBasic = nameof(HandYOffsetBasic);
        public const string HandYOffsetAfterKeyDown = nameof(HandYOffsetAfterKeyDown);

        // Motion, Arm
        public const string SetWaistWidth = nameof(SetWaistWidth);
        public const string SetElbowCloseStrength = nameof(SetElbowCloseStrength);
        public const string EnablePresenterMotion = nameof(EnablePresenterMotion);
        public const string PresentationArmMotionScale = nameof(PresentationArmMotionScale);
        public const string PresentationArmRadiusMin = nameof(PresentationArmRadiusMin);

        // Motion, Wait
        public const string EnableWaitMotion = nameof(EnableWaitMotion);
        public const string WaitMotionScale = nameof(WaitMotionScale);
        public const string WaitMotionPeriod = nameof(WaitMotionPeriod);

        // Motion, Face
        public const string EnableFaceTracking = nameof(EnableFaceTracking);
        public const string SetCameraDeviceName = nameof(SetCameraDeviceName);
        public const string CalibrateFace = nameof(CalibrateFace);
        public const string SetCalibrateFaceData = nameof(SetCalibrateFaceData);
        public const string FaceDefaultFun = nameof(FaceDefaultFun);

        // Motion, Mouth
        public const string EnableLipSync = nameof(EnableLipSync);
        public const string SetMicrophoneDeviceName = nameof(SetMicrophoneDeviceName);

        // Motion, Eye
        public const string LookAtStyle = nameof(LookAtStyle);

        //public const string EnableTouchTyping = nameof(EnableTouchTyping);

        // Layout, camera
        public const string SetCustomCameraPosition = nameof(SetCustomCameraPosition);
        public const string EnableFreeCameraMode = nameof(EnableFreeCameraMode);
        public const string ResetCameraPosition = nameof(ResetCameraPosition);

        // Layout, HID (keyboard and mousepad)
        public const string HidHeight = nameof(HidHeight);
        public const string HidHorizontalScale = nameof(HidHorizontalScale);
        public const string HidVisibility = nameof(HidVisibility);

        // Layout, Gamepad
        public const string EnableGamepad = nameof(EnableGamepad);

        public const string GamepadHeight = nameof(GamepadHeight);
        public const string GamepadHorizontalScale = nameof(GamepadHorizontalScale);
        public const string GamepadVisibility = nameof(GamepadVisibility);

        public const string GamepadLeanMode = nameof(GamepadLeanMode);
        public const string GamepadLeanReverseHorizontal = nameof(GamepadLeanReverseHorizontal);
        public const string GamepadLeanReverseVertical = nameof(GamepadLeanReverseVertical);

        //Layout, Free Layout
        public const string EnableCustomDevicePositions = nameof(EnableCustomDevicePositions);
        public const string SetCustomDevicePositions = nameof(SetCustomDevicePositions);
        public const string EnableFreeDevicesMode = nameof(EnableFreeDevicesMode);

        // Lighting 
        public const string LightIntensity = nameof(LightIntensity);
        public const string LightColor = nameof(LightColor);
        public const string BloomIntensity = nameof(BloomIntensity);
        public const string BloomThreshold = nameof(BloomThreshold);
        public const string BloomColor = nameof(BloomColor);


    }
}

