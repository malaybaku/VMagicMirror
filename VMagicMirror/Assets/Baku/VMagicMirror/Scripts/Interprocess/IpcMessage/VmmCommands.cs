namespace Baku.VMagicMirror
{
    // NOTE:
    // - ここのenumの値はWPF/Unityで実行時に揃ってることが必要 ≒ メッセージを追加定義した時点でWPF側はリビルドが必須。
    // - 値は永続化されず、アプリケーションの実行中にのみ使うので、途中に値を追加定義するぶんにはOK
    public enum VmmCommands : ushort
    {
        // NOTE: Queryに対する返信のCommandsは0扱いになる
        Unknown = 0,
        
        #region Query用のコマンドのID
        
        // Microphone
        CurrentMicrophoneDeviceName,
        MicrophoneDeviceNames,

        // Camera
        CurrentCameraPosition,
        
        // Web Camera
        CameraDeviceNames,
        
        // Image Quality
        GetQualitySettingsInfo,
        ApplyDefaultImageQuality,

        // Word to Motion / GameInput
        GetAvailableCustomMotionClipNames,
        
        #endregion 
        
        // ここから下はクエリではないID
        
        Language,

        // Input 
        //KeyDown,
        //MouseMoved,
        MouseButton,

        // Load VRM
        OpenVrmPreview,
        OpenVrm,
        CancelLoadVrm,
        RequestAutoAdjust,

        // Window
        Chromakey,
        WindowFrameVisibility,
        IgnoreMouse,
        TopMost,
        WindowDraggable,
        SetBackgroundImagePath,
        MoveWindow,
        ResetWindowSize,
        SetWholeWindowTransparencyLevel,
        SetAlphaValueOnTransparent,
        //NOTE: 「GUIの初期読み込みが終了した」という、1回だけ呼ばれるタイプのやつ
        StartupEnded,

        // Motion

        // Motion, Enable
        
        // Motion, Hand
        LengthFromWristToTip,
        HandYOffsetBasic,
        HandYOffsetAfterKeyDown,

        // Motion, Arm
        EnableHidRandomTyping,
        EnableShoulderMotionModify,
        EnableTypingHandDownTimeout,
        SetWaistWidth,
        SetElbowCloseStrength,
        EnableFpsAssumedRightHand,
        PresentationArmRadiusMin,

        SetKeyboardAndMouseMotionMode,
        SetGamepadMotionMode,
        EnableCustomHandDownPose,
        SetHandDownModeCustomPose,
        ResetCustomHandDownPose,
            
        // Motion, Wait
        EnableWaitMotion,
        WaitMotionScale,
        WaitMotionPeriod,
        
        // Motion, Body
        EnableNoHandTrackMode,
        EnableGameInputLocomotionMode,
        EnableTwistBodyMotion,
        EnableBodyLeanZ,

        // Motion, Face
        EnableFaceTracking,
        SetCameraDeviceName,
        AutoBlinkDuringFaceTracking,
        EnableHeadRotationBasedBlinkAdjust,
        EnableLipSyncBasedBlinkAdjust,
        EnableVoiceBasedMotion,
        // NOTE: HorizontalFlipControllerのみからこの値を参照すること (ゲーム入力モードの状態とかも踏まえて最終的な反転on/offを計算するため)
        DisableFaceTrackingHorizontalFlip,
        CalibrateFace,
        SetCalibrateFaceData,
        SetCalibrateFaceDataHighPower,

        FaceDefaultFun,
        FaceNeutralClip,
        FaceOffsetClip,
        EnableEyeMotionDuringClipApplied,
        DisableBlendShapeInterpolate,

        // Motion, Face, WebCam high power mode
        EnableWebCamHighPowerMode,
        EnableWebCameraHighPowerModeLipSync,
        EnableWebCameraHighPowerModeMoveZ,

        SetWebCamEyeOpenBlinkValue,
        SetWebCamEyeCloseBlinkValue,
        SetEyeBlendShapePreviewActive,
        SetWebCamEyeApplySameBlinkBothEye,
        SetWebCamEyeApplyBlinkCorrectionToPerfectSync,

        // Motion, Mouth
        EnableLipSync,
        SetMicrophoneDeviceName,
        SetMicrophoneSensitivity,
        SetMicrophoneVolumeVisibility,
        AdjustLipSyncByVolume,

        // Motion, Eye
        LookAtStyle,
        SetUseAvatarEyeBoneMap,
        SetEyeBoneRotationScale,
        SetEyeBoneRotationScaleWithMap,

        // Motion, Image-based Hand
        EnableImageBasedHandTracking,
        // NOTE: HorizontalFlipControllerのみからこの値を参照すること (ゲーム入力モードの状態とかも踏まえて最終的な反転on/offを計算するため)
        DisableHandTrackingHorizontalFlip,
        EnableSendHandTrackingResult,
        SetHandTrackingMotionScale,
        // +X: 手を体の横に広げる、+Y: 手を上げる
        SetHandTrackingOffsetX,
        SetHandTrackingOffsetY,

        // Motion, GameInput
        UseGamepadForGameInput,
        UseKeyboardForGameInput,
        SetGamepadGameInputKeyAssign,
        SetKeyboardGameInputKeyAssign,
        SetGameInputLocomotionStyle,
        EnableAlwaysRunGameInput,
        EnableWasdMoveGameInput,
        EnableArrowKeyMoveGameInput,
        UseShiftRunGameInput,
        UseSpaceJumpGameInput,
        UseMouseMoveForLookAroundGameInput,

        // Layout, camera
        SetCustomCameraPosition,
        QuickLoadViewPoint,
        EnableFreeCameraMode,
        ResetCameraPosition,
        CameraFov,

        // Layout, device layouts
        HideUnusedDevices,
        SetDeviceLayout,
        ResetDeviceLayout,
        
        // Layout, HID (keyboard and mousepad)
        HidVisibility,
        SetPenVisibility,
        SetKeyboardTypingEffectType,
        EnableDeviceFreeLayout,
        
        // Layout, MIDI Controller
        MidiControllerVisibility,
        EnableMidiRead,

        // Layout, Gamepad
        EnableGamepad,
        PreferDirectInputGamepad,

        GamepadVisibility,

        GamepadLeanMode,
        GamepadLeanReverseHorizontal,
        GamepadLeanReverseVertical,

        // Image Quality
        SetImageQuality,
        SetAntiAliasStyle,
        SetHalfFpsMode,

        // Lighting 
        //NOTE: フレームリダクションはモーションよりはエフェクトかな～という事でこっち。
        UseFrameReductionEffect,

        LightIntensity,
        LightColor,
        LightYaw,
        LightPitch,
        UseDesktopLightAdjust,

        ShadowEnable,
        ShadowIntensity,
        ShadowYaw,
        ShadowPitch,
        ShadowDepthOffset,

        BloomIntensity,
        BloomThreshold,
        BloomColor,

        AmbientOcclusionEnable,
        AmbientOcclusionIntensity,
        AmbientOcclusionColor,

        OutlineEffectEnable,
        OutlineEffectThickness,
        OutlineEffectColor,
        OutlineEffectHighQualityMode,
        
        ShowEffectDuringHandTracking,

        WindEnable,
        WindStrength,
        WindInterval,
        WindYaw,
        
        // Word to Motion
        ReloadMotionRequests,

        PlayWordToMotionItem,
        EnableWordToMotionPreview,
        SendWordToMotionPreviewInfo,

        SetDeviceTypeToStartWordToMotion,

        RequestCustomMotionDoctor,

        LoadMidiNoteToMotionMap,
        RequireMidiNoteOnMessage,

        // Screenshot
        TakeScreenshot,
        OpenScreenshotFolder,
        
        // External Tracker
        ExTrackerEnable,
        ExTrackerEnableLipSync,
        ExTrackerEnablePerfectSync,
        ExTrackerCalibrate,
        ExTrackerSetCalibrateData,
        ExTrackerSetSource,
        /// <summary> 今のとこ使ってません (アプリ固有設定をいちいち記憶しない設計なので) </summary>
        ExTrackerSetApplicationValue,
        ExTrackerSetFaceSwitchSetting,
        
        // Accessory
        SetSingleAccessoryLayout,
        SetAccessoryLayout,
        RequestResetAllAccessoryLayout,
        RequestResetAccessoryLayout,
        ReloadAccessoryFiles,
        
        // External Video Sharing
        EnableSpoutOutput,
        SetSpoutOutputResolution,
        
        // VMCP (recv)
        EnableVMCP,
        SetVMCPSources,
        SetVMCPNaiveBoneTransfer,
        SetDisableCameraDuringVMCPActive,

        // VMCP (send)
        EnableVMCPSend,
        SetVMCPSendSettings,
        ShowEffectDuringVMCPSendEnabled,
        
        // Buddy
        BuddySetMainAvatarOutputActive,
        BuddyEnable,
        BuddyDisable,
        BuddyRefreshData,
        BuddySetProperty,
        BuddyInvokeAction,

        // Buddy (開発者モード)
        BuddySetDeveloperModeActive,
        BuddySetDeveloperModeLogLevel,
        
        // VRoidHub
        OpenVRoidSdkUi,
        RequestLoadVRoidWithId,
        
        // Debug
        DebugSendLargeData,
        
        // Unused: 取りうるコマンドIDの最大値が分かるように定義している
        LastCommandId,
    }
}

