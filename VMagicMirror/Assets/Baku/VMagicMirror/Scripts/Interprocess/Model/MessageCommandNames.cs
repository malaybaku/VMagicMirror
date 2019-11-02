namespace Baku.VMagicMirror
{
    public static class MessageCommandNames
    {
        //NOTE: 順番をほぼVMagicMirrorConfigのMessageFactoryクラスに合わせているが、そこまで重要ではない

        public const string Language = nameof(Language);

        // Input 
        //public const string KeyDown = nameof(KeyDown);
        //public const string MouseMoved = nameof(MouseMoved);
        //public const string MouseButton = nameof(MouseButton);

        // Load VRM
        public const string OpenVrmPreview = nameof(OpenVrmPreview);
        public const string OpenVrm = nameof(OpenVrm);
        public const string CancelLoadVrm = nameof(CancelLoadVrm);
        public const string AccessToVRoidHub = nameof(AccessToVRoidHub);
        public const string RequestAutoAdjust = nameof(RequestAutoAdjust);
        public const string RequestAutoAdjustEyebrow = nameof(RequestAutoAdjustEyebrow);

        // Window
        public const string Chromakey = nameof(Chromakey);
        public const string WindowFrameVisibility = nameof(WindowFrameVisibility);
        public const string IgnoreMouse = nameof(IgnoreMouse);
        public const string TopMost = nameof(TopMost);
        public const string WindowDraggable = nameof(WindowDraggable);
        public const string MoveWindow = nameof(MoveWindow);
        public const string ResetWindowSize = nameof(ResetWindowSize);
        public const string SetWholeWindowTransparencyLevel = nameof(SetWholeWindowTransparencyLevel);
        public const string SetAlphaValueOnTransparent = nameof(SetAlphaValueOnTransparent);

        // Motion

        // Motion, Enable
        public const string EnableHidArmMotion = nameof(EnableHidArmMotion);

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
        public const string AutoBlinkDuringFaceTracking = nameof(AutoBlinkDuringFaceTracking);
        public const string CalibrateFace = nameof(CalibrateFace);
        public const string SetCalibrateFaceData = nameof(SetCalibrateFaceData);
        public const string FaceDefaultFun = nameof(FaceDefaultFun);

        // Moition, Face, Eyebrow
        public const string EyebrowLeftUpKey = nameof(EyebrowLeftUpKey);
        public const string EyebrowLeftDownKey = nameof(EyebrowLeftDownKey);
        public const string UseSeparatedKeyForEyebrow = nameof(UseSeparatedKeyForEyebrow);
        public const string EyebrowRightUpKey = nameof(EyebrowRightUpKey);
        public const string EyebrowRightDownKey = nameof(EyebrowRightDownKey);
        public const string EyebrowUpScale = nameof(EyebrowUpScale);
        public const string EyebrowDownScale = nameof(EyebrowDownScale);


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
        public const string CameraFov = nameof(CameraFov);

        // Layout, HID (keyboard and mousepad)
        public const string HidHeight = nameof(HidHeight);
        public const string HidHorizontalScale = nameof(HidHorizontalScale);
        public const string HidVisibility = nameof(HidVisibility);
        public const string SetKeyboardTypingEffectType = nameof(SetKeyboardTypingEffectType);

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
        public const string LightYaw = nameof(LightYaw);
        public const string LightPitch = nameof(LightPitch);

        public const string ShadowEnable = nameof(ShadowEnable);
        public const string ShadowIntensity = nameof(ShadowIntensity);
        public const string ShadowYaw = nameof(ShadowYaw);
        public const string ShadowPitch = nameof(ShadowPitch);
        public const string ShadowDepthOffset = nameof(ShadowDepthOffset);

        public const string BloomIntensity = nameof(BloomIntensity);
        public const string BloomThreshold = nameof(BloomThreshold);
        public const string BloomColor = nameof(BloomColor);

        // Word to Motion
        public const string EnableWordToMotion = nameof(EnableWordToMotion);
        public const string ReloadMotionRequests = nameof(ReloadMotionRequests);

        public const string PlayWordToMotionItem = nameof(PlayWordToMotionItem);
        public const string EnableWordToMotionPreview = nameof(EnableWordToMotionPreview);
        public const string SendWordToMotionPreviewInfo = nameof(SendWordToMotionPreviewInfo);

        public const string SetDeviceTypeToStartWordToMotion = nameof(SetDeviceTypeToStartWordToMotion);
        

        // Screenshot
        public const string TakeScreenshot = nameof(TakeScreenshot);
        public const string OpenScreenshotFolder = nameof(OpenScreenshotFolder);

        // Meta message
        public const string CommandArray = nameof(CommandArray);
    }
}

