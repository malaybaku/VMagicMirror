﻿namespace Baku.VMagicMirror
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
        //NOTE: 「GUIの初期読み込みが終了した」という、1回だけ呼ばれるタイプのやつ
        public const string StartupEnded = nameof(StartupEnded);

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
        public const string EnableCustomHandDownPose = nameof(EnableCustomHandDownPose);
        public const string SetHandDownModeCustomPose = nameof(SetHandDownModeCustomPose);
        public const string ResetCustomHandDownPose = nameof(ResetCustomHandDownPose);
            
        // Motion, Wait
        public const string EnableWaitMotion = nameof(EnableWaitMotion);
        public const string WaitMotionScale = nameof(WaitMotionScale);
        public const string WaitMotionPeriod = nameof(WaitMotionPeriod);
        
        // Motion, Body
        public const string EnableNoHandTrackMode = nameof(EnableNoHandTrackMode);
        public const string EnableGameInputLocomotionMode = nameof(EnableGameInputLocomotionMode);
        public const string EnableTwistBodyMotion = nameof(EnableTwistBodyMotion);
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
        public const string EnableEyeMotionDuringClipApplied = nameof(EnableEyeMotionDuringClipApplied);
        public const string DisableBlendShapeInterpolate = nameof(DisableBlendShapeInterpolate);

        // Motion, Mouth
        public const string EnableLipSync = nameof(EnableLipSync);
        public const string SetMicrophoneDeviceName = nameof(SetMicrophoneDeviceName);
        public const string SetMicrophoneSensitivity = nameof(SetMicrophoneSensitivity);
        public const string SetMicrophoneVolumeVisibility = nameof(SetMicrophoneVolumeVisibility);
        public const string AdjustLipSyncByVolume = nameof(AdjustLipSyncByVolume);

        // Motion, Eye
        public const string LookAtStyle = nameof(LookAtStyle);
        public const string SetUseAvatarEyeBoneMap = nameof(SetUseAvatarEyeBoneMap);
        public const string SetEyeBoneRotationScale = nameof(SetEyeBoneRotationScale);
        public const string SetEyeBoneRotationScaleWithMap = nameof(SetEyeBoneRotationScaleWithMap);

        // Motion, Image-based Hand
        public const string EnableImageBasedHandTracking = nameof(EnableImageBasedHandTracking);
        public const string DisableHandTrackingHorizontalFlip = nameof(DisableHandTrackingHorizontalFlip);
        public const string EnableSendHandTrackingResult = nameof(EnableSendHandTrackingResult);

        // Motion, GameInput
        public const string UseGamepadForGameInput = nameof(UseGamepadForGameInput);
        public const string UseKeyboardForGameInput = nameof(UseKeyboardForGameInput);
        public const string SetGamepadGameInputKeyAssign = nameof(SetGamepadGameInputKeyAssign);
        public const string SetKeyboardGameInputKeyAssign = nameof(SetKeyboardGameInputKeyAssign);
        public const string SetGameInputLocomotionStyle = nameof(SetGameInputLocomotionStyle);
        public const string EnableAlwaysRunGameInput = nameof(EnableAlwaysRunGameInput);
        public const string EnableWasdMoveGameInput = nameof(EnableWasdMoveGameInput);
        public const string EnableArrowKeyMoveGameInput = nameof(EnableArrowKeyMoveGameInput);
        public const string UseShiftRunGameInput = nameof(UseShiftRunGameInput);
        public const string UseSpaceJumpGameInput = nameof(UseSpaceJumpGameInput);
        public const string UseMouseMoveForLookAroundGameInput = nameof(UseMouseMoveForLookAroundGameInput);

        // Layout, camera
        public const string SetCustomCameraPosition = nameof(SetCustomCameraPosition);
        public const string QuickLoadViewPoint = nameof(QuickLoadViewPoint);
        public const string EnableFreeCameraMode = nameof(EnableFreeCameraMode);
        public const string ResetCameraPosition = nameof(ResetCameraPosition);
        public const string CameraFov = nameof(CameraFov);

        // Layout, device layouts
        public const string HideUnusedDevices = nameof(HideUnusedDevices);
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

        public const string GamepadVisibility = nameof(GamepadVisibility);

        public const string GamepadLeanMode = nameof(GamepadLeanMode);
        public const string GamepadLeanReverseHorizontal = nameof(GamepadLeanReverseHorizontal);
        public const string GamepadLeanReverseVertical = nameof(GamepadLeanReverseVertical);

        // Image Quality
        public const string SetImageQuality = nameof(SetImageQuality);
        public const string SetAntiAliasStyle = nameof(SetAntiAliasStyle);
        public const string SetHalfFpsMode = nameof(SetHalfFpsMode);

        // Lighting 
        //NOTE: フレームリダクションはモーションよりはエフェクトかな～という事でこっち。
        public const string UseFrameReductionEffect = nameof(UseFrameReductionEffect);

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

        public const string OutlineEffectEnable = nameof(OutlineEffectEnable);
        public const string OutlineEffectThickness = nameof(OutlineEffectThickness);
        public const string OutlineEffectColor = nameof(OutlineEffectColor);
        public const string OutlineEffectHighQualityMode = nameof(OutlineEffectHighQualityMode);
        
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
        public const string ReloadAccessoryFiles = nameof(ReloadAccessoryFiles);
        
        // External Video Sharing
        public const string EnableSpoutOutput = nameof(EnableSpoutOutput);
        public const string SetSpoutOutputResolution = nameof(SetSpoutOutputResolution);
        
        // VMCP
        public const string EnableVMCP = nameof(EnableVMCP);
        public const string SetVMCPSources = nameof(SetVMCPSources);
        public const string SetVMCPNaiveBoneTransfer = nameof(SetVMCPNaiveBoneTransfer);
        public const string SetDisableCameraDuringVMCPActive = nameof(SetDisableCameraDuringVMCPActive);
        
        // VRoidHub
        public const string OpenVRoidSdkUi = nameof(OpenVRoidSdkUi);
        public const string RequestLoadVRoidWithId = nameof(RequestLoadVRoidWithId);
        
        // Meta message
        public const string CommandArray = nameof(CommandArray);
    }
}

