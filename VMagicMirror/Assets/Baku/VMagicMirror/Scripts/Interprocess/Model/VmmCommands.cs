using System;

namespace Baku.VMagicMirror
{
    public static class VmmCommands
    {
        //NOTE: 順番をほぼVMagicMirrorConfigのMessageFactoryクラスに合わせているが、そこまで重要ではない

        public const string Language = nameof(Language);

        // Input 
        //public const string KeyDown = nameof(KeyDown);
        //public const string MouseMoved = nameof(MouseMoved);
        public const string MouseButton = nameof(MouseButton);

        // Load VRM
        public const string OpenVrmPreview = nameof(OpenVrmPreview);
        public const string OpenVrm = nameof(OpenVrm);
        public const string CancelLoadVrm = nameof(CancelLoadVrm);
        public const string RequestAutoAdjust = nameof(RequestAutoAdjust);

        // Window
        public const string Chromakey = nameof(Chromakey);
        public const string WindowFrameVisibility = nameof(WindowFrameVisibility);
        public const string IgnoreMouse = nameof(IgnoreMouse);
        public const string TopMost = nameof(TopMost);
        public const string WindowDraggable = nameof(WindowDraggable);
        public const string SetBackgroundImagePath = nameof(SetBackgroundImagePath);
        public const string MoveWindow = nameof(MoveWindow);
        public const string ResetWindowSize = nameof(ResetWindowSize);
        public const string SetWholeWindowTransparencyLevel = nameof(SetWholeWindowTransparencyLevel);
        public const string SetAlphaValueOnTransparent = nameof(SetAlphaValueOnTransparent);

        // Motion

        // Motion, Enable
        
        // Motion, Hand
        public const string LengthFromWristToTip = nameof(LengthFromWristToTip);
        public const string HandYOffsetBasic = nameof(HandYOffsetBasic);
        public const string HandYOffsetAfterKeyDown = nameof(HandYOffsetAfterKeyDown);

        // Motion, Arm
        public const string EnableHidRandomTyping = nameof(EnableHidRandomTyping);
        public const string EnableShoulderMotionModify = nameof(EnableShoulderMotionModify);
        public const string EnableTypingHandDownTimeout = nameof(EnableTypingHandDownTimeout);
        public const string SetWaistWidth = nameof(SetWaistWidth);
        public const string SetElbowCloseStrength = nameof(SetElbowCloseStrength);
        public const string EnableFpsAssumedRightHand = nameof(EnableFpsAssumedRightHand);
        public const string PresentationArmRadiusMin = nameof(PresentationArmRadiusMin);

        public const string SetKeyboardAndMouseMotionMode = nameof(SetKeyboardAndMouseMotionMode);
        public const string SetGamepadMotionMode = nameof(SetGamepadMotionMode);

        // Motion, Wait
        public const string EnableWaitMotion = nameof(EnableWaitMotion);
        public const string WaitMotionScale = nameof(WaitMotionScale);
        public const string WaitMotionPeriod = nameof(WaitMotionPeriod);
        
        // Motion, Body
        public const string EnableNoHandTrackMode = nameof(EnableNoHandTrackMode);
        public const string EnableBodyLeanZ = nameof(EnableBodyLeanZ);

        // Motion, Face
        public const string EnableFaceTracking = nameof(EnableFaceTracking);
        public const string SetCameraDeviceName = nameof(SetCameraDeviceName);
        public const string EnableWebCamHighPowerMode = nameof(EnableWebCamHighPowerMode);
        public const string AutoBlinkDuringFaceTracking = nameof(AutoBlinkDuringFaceTracking);
        public const string EnableHeadRotationBasedBlinkAdjust = nameof(EnableHeadRotationBasedBlinkAdjust);
        public const string EnableLipSyncBasedBlinkAdjust = nameof(EnableLipSyncBasedBlinkAdjust);
        public const string EnableVoiceBasedMotion = nameof(EnableVoiceBasedMotion);
        public const string DisableFaceTrackingHorizontalFlip = nameof(DisableFaceTrackingHorizontalFlip);
        public const string CalibrateFace = nameof(CalibrateFace);
        public const string SetCalibrateFaceData = nameof(SetCalibrateFaceData);
        public const string FaceDefaultFun = nameof(FaceDefaultFun);
        public const string FaceNeutralClip = nameof(FaceNeutralClip);
        public const string FaceOffsetClip = nameof(FaceOffsetClip);

        // Motion, Mouth
        public const string EnableLipSync = nameof(EnableLipSync);
        public const string SetMicrophoneDeviceName = nameof(SetMicrophoneDeviceName);
        public const string SetMicrophoneSensitivity = nameof(SetMicrophoneSensitivity);
        public const string SetMicrophoneVolumeVisibility = nameof(SetMicrophoneVolumeVisibility);

        // Motion, Eye
        public const string LookAtStyle = nameof(LookAtStyle);
        public const string SetEyeBoneRotationScale = nameof(SetEyeBoneRotationScale);

        // Motion, Image-based Hand
        public const string EnableImageBasedHandTracking = nameof(EnableImageBasedHandTracking);
        public const string DisableHandTrackingHorizontalFlip = nameof(DisableHandTrackingHorizontalFlip);
        public const string EnableSendHandTrackingResult = nameof(EnableSendHandTrackingResult);

        //public const string EnableTouchTyping = nameof(EnableTouchTyping);

        // Layout, camera
        public const string SetCustomCameraPosition = nameof(SetCustomCameraPosition);
        public const string QuickLoadViewPoint = nameof(QuickLoadViewPoint);
        public const string EnableFreeCameraMode = nameof(EnableFreeCameraMode);
        public const string ResetCameraPosition = nameof(ResetCameraPosition);
        public const string CameraFov = nameof(CameraFov);

        // Layout, device layouts
        public const string SetDeviceLayout = nameof(SetDeviceLayout);
        public const string ResetDeviceLayout = nameof(ResetDeviceLayout);
        
        // Layout, HID (keyboard and mousepad)
        public const string HidVisibility = nameof(HidVisibility);
        public const string SetPenVisibility = nameof(SetPenVisibility);
        public const string SetKeyboardTypingEffectType = nameof(SetKeyboardTypingEffectType);
        public const string EnableDeviceFreeLayout = nameof(EnableDeviceFreeLayout);
        
        // Layout, MIDI Controller
        public const string MidiControllerVisibility = nameof(MidiControllerVisibility);
        public const string EnableMidiRead = nameof(EnableMidiRead);

        // Layout, Gamepad
        public const string EnableGamepad = nameof(EnableGamepad);
        public const string PreferDirectInputGamepad = nameof(PreferDirectInputGamepad);

        public const string GamepadHeight = nameof(GamepadHeight);
        public const string GamepadHorizontalScale = nameof(GamepadHorizontalScale);
        public const string GamepadVisibility = nameof(GamepadVisibility);

        public const string GamepadLeanMode = nameof(GamepadLeanMode);
        public const string GamepadLeanReverseHorizontal = nameof(GamepadLeanReverseHorizontal);
        public const string GamepadLeanReverseVertical = nameof(GamepadLeanReverseVertical);

        // Image Quality
        public const string SetImageQuality = nameof(SetImageQuality);
        public const string SetHalfFpsMode = nameof(SetHalfFpsMode);

        // Lighting 
        public const string LightIntensity = nameof(LightIntensity);
        public const string LightColor = nameof(LightColor);
        public const string LightYaw = nameof(LightYaw);
        public const string LightPitch = nameof(LightPitch);
        public const string UseDesktopLightAdjust = nameof(UseDesktopLightAdjust);

        public const string ShadowEnable = nameof(ShadowEnable);
        public const string ShadowIntensity = nameof(ShadowIntensity);
        public const string ShadowYaw = nameof(ShadowYaw);
        public const string ShadowPitch = nameof(ShadowPitch);
        public const string ShadowDepthOffset = nameof(ShadowDepthOffset);

        public const string BloomIntensity = nameof(BloomIntensity);
        public const string BloomThreshold = nameof(BloomThreshold);
        public const string BloomColor = nameof(BloomColor);

        public const string ShowEffectDuringHandTracking = nameof(ShowEffectDuringHandTracking);

        public const string WindEnable = nameof(WindEnable);
        public const string WindStrength = nameof(WindStrength);
        public const string WindInterval = nameof(WindInterval);
        public const string WindYaw = nameof(WindYaw);
        
        // Word to Motion
        public const string ReloadMotionRequests = nameof(ReloadMotionRequests);

        public const string PlayWordToMotionItem = nameof(PlayWordToMotionItem);
        public const string EnableWordToMotionPreview = nameof(EnableWordToMotionPreview);
        public const string SendWordToMotionPreviewInfo = nameof(SendWordToMotionPreviewInfo);

        public const string SetDeviceTypeToStartWordToMotion = nameof(SetDeviceTypeToStartWordToMotion);

        public const string RequestCustomMotionDoctor = nameof(RequestCustomMotionDoctor);

        public const string LoadMidiNoteToMotionMap = nameof(LoadMidiNoteToMotionMap);
        public const string RequireMidiNoteOnMessage = nameof(RequireMidiNoteOnMessage);

        // Screenshot
        public const string TakeScreenshot = nameof(TakeScreenshot);
        public const string OpenScreenshotFolder = nameof(OpenScreenshotFolder);
        
        // External Tracker
        public const string ExTrackerEnable = nameof(ExTrackerEnable);
        public const string ExTrackerEnableLipSync = nameof(ExTrackerEnableLipSync);
        public const string ExTrackerEnablePerfectSync = nameof(ExTrackerEnablePerfectSync);
        public const string ExTrackerCalibrate = nameof(ExTrackerCalibrate);
        public const string ExTrackerSetCalibrateData = nameof(ExTrackerSetCalibrateData);
        public const string ExTrackerSetSource = nameof(ExTrackerSetSource);
        /// <summary> 今のとこ使ってません (アプリ固有設定をいちいち記憶しない設計なので) </summary>
        public const string ExTrackerSetApplicationValue = nameof(ExTrackerSetApplicationValue);
        public const string ExTrackerSetFaceSwitchSetting = nameof(ExTrackerSetFaceSwitchSetting);
        
        // Accessory
        public const string SetSingleAccessoryLayout = nameof(SetSingleAccessoryLayout);
        public const string SetAccessoryLayout = nameof(SetAccessoryLayout);
        public const string RequestResetAllAccessoryLayout = nameof(RequestResetAllAccessoryLayout);
        public const string RequestResetAccessoryLayout = nameof(RequestResetAccessoryLayout);

        // VRoidHub
        public const string OpenVRoidSdkUi = nameof(OpenVRoidSdkUi);
        public const string RequestLoadVRoidWithId = nameof(RequestLoadVRoidWithId);
        
        // Meta message
        public const string CommandArray = nameof(CommandArray);
    }
}

