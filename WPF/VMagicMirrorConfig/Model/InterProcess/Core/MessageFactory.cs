using Baku.VMagicMirror;

// NOTE: 「static memberにできます」の警告が出るが、ここは意図してinstance memberなので無視しとく
#pragma warning disable CA1822

namespace Baku.VMagicMirrorConfig
{
    // NOTE: できればこのクラスを削除したい。VmmCommandsをenum化したことによってFactoryを使う意義が薄まっているため。
    // 引数をこのクラスで一回絞っておくメリットを取る場合は残すのもアリ
    static class MessageFactory
    {
        private static Message None(VmmCommands command) => Message.None(command);

        private static Message StringContent(VmmCommands command, string content) => Message.String(command, content);

        private static Message BoolContent(VmmCommands command, bool content) => Message.Bool(command, content);

        private static Message IntContent(VmmCommands command, int content) => Message.Int(command, content);

        private static Message IntArrayContent(VmmCommands command, int[] content) => Message.IntArray(command, content);



        public static Message Language(string langName) => StringContent(VmmCommands.Language, langName);


        #region HID Input

        //public static Message KeyDown(string keyName) => StringContent(VmmCommands.KeyDown, keyName);
        public static Message MouseButton(string info) => StringContent(VmmCommands.MouseButton, info);
        //public static Message MouseMoved(int x, int y) => StringContent(VmmCommands.MouseMoved, $"{x},{y}");

        #endregion

        #region VRM Load

        public static Message OpenVrmPreview(string filePath) => StringContent(VmmCommands.OpenVrmPreview, filePath);
        public static Message OpenVrm(string filePath) => StringContent(VmmCommands.OpenVrm, filePath);
        //public static Message AccessToVRoidHub() => NoArg(VmmCommands.AccessToVRoidHub);

        public static Message CancelLoadVrm() => None(VmmCommands.CancelLoadVrm);

        public static Message RequestAutoAdjust() => None(VmmCommands.RequestAutoAdjust);

        #endregion

        #region ウィンドウ

        public static Message Chromakey(int a, int r, int g, int b) => IntArrayContent(VmmCommands.Chromakey, [a, r, g, b]);

        public static Message WindowFrameVisibility(bool v) => BoolContent(VmmCommands.WindowFrameVisibility, v);
        public static Message IgnoreMouse(bool v) => BoolContent(VmmCommands.IgnoreMouse, v);
        public static Message TopMost(bool v) => BoolContent(VmmCommands.TopMost, v);
        public static Message WindowDraggable(bool v) => BoolContent(VmmCommands.WindowDraggable, v);

        /// <summary>
        /// NOTE: 空文字列なら背景画像を外す処理をやる。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Message SetBackgroundImagePath(string path) => StringContent(VmmCommands.SetBackgroundImagePath, path);

        public static Message MoveWindow(int x, int y) => IntArrayContent(VmmCommands.MoveWindow, [x, y]);
        public static Message ResetWindowSize() => None(VmmCommands.ResetWindowSize);

        public static Message SetWholeWindowTransparencyLevel(int level) => IntContent(VmmCommands.SetWholeWindowTransparencyLevel, level);

        public static Message SetAlphaValueOnTransparent(int alpha) => IntContent(VmmCommands.SetAlphaValueOnTransparent, alpha);

        public static Message EnableSpoutOutput(bool enable) => BoolContent(VmmCommands.EnableSpoutOutput, enable);
        public static Message SetSpoutOutputResolution(int type) => IntContent(VmmCommands.SetSpoutOutputResolution, type);


        public static Message StartupEnded() => None(VmmCommands.StartupEnded);

        #endregion

        #region モーション

        public static Message EnableNoHandTrackMode(bool enable) => BoolContent(VmmCommands.EnableNoHandTrackMode, enable);
        public static Message EnableGameInputLocomotionMode(bool enable) => BoolContent(VmmCommands.EnableGameInputLocomotionMode, enable);
        public static Message EnableTwistBodyMotion(bool enable) => BoolContent(VmmCommands.EnableTwistBodyMotion, enable);

        public static Message EnableCustomHandDownPose(bool enable) => BoolContent(VmmCommands.EnableCustomHandDownPose, enable);
        public static Message SetHandDownModeCustomPose(string poseJson) => StringContent(VmmCommands.SetHandDownModeCustomPose, poseJson);
        public static Message ResetCustomHandDownPose() => None(VmmCommands.ResetCustomHandDownPose);


        public static Message LengthFromWristToTip(int lengthCentimeter) => IntContent(VmmCommands.LengthFromWristToTip, lengthCentimeter);

        public static Message HandYOffsetBasic(int offsetCentimeter) => IntContent(VmmCommands.HandYOffsetBasic, offsetCentimeter);
        public static Message HandYOffsetAfterKeyDown(int offsetCentimeter) => IntContent(VmmCommands.HandYOffsetAfterKeyDown, offsetCentimeter);

        public static Message EnableHidRandomTyping(bool enable) => BoolContent(VmmCommands.EnableHidRandomTyping, enable);
        public static Message EnableShoulderMotionModify(bool enable) => BoolContent(VmmCommands.EnableShoulderMotionModify, enable);
        public static Message EnableTypingHandDownTimeout(bool enable) => BoolContent(VmmCommands.EnableTypingHandDownTimeout, enable);
        public static Message SetWaistWidth(int waistWidthCentimeter) => IntContent(VmmCommands.SetWaistWidth, waistWidthCentimeter);
        public static Message SetElbowCloseStrength(int strengthPercent) => IntContent(VmmCommands.SetElbowCloseStrength, strengthPercent);

        public static Message EnableFpsAssumedRightHand(bool enable) => BoolContent(VmmCommands.EnableFpsAssumedRightHand, enable);
        public static Message PresentationArmRadiusMin(int radiusMinCentimeter) => IntContent(VmmCommands.PresentationArmRadiusMin, radiusMinCentimeter);

        public static Message SetKeyboardAndMouseMotionMode(int modeIndex) => IntContent(VmmCommands.SetKeyboardAndMouseMotionMode, modeIndex);
        public static Message SetGamepadMotionMode(int modeIndex) => IntContent(VmmCommands.SetGamepadMotionMode, modeIndex);

        public static Message EnableWaitMotion(bool enable) => BoolContent(VmmCommands.EnableWaitMotion, enable);
        public static Message WaitMotionScale(int scalePercent) => IntContent(VmmCommands.WaitMotionScale, scalePercent);
        public static Message WaitMotionPeriod(int periodSec) => IntContent(VmmCommands.WaitMotionPeriod, periodSec);

        // NOTE: Unity側の状態によって実際に行うキャリブレーションは変わる(低負荷/高負荷では別々のキャリブレーションを行う)
        public static Message CalibrateFace() => None(VmmCommands.CalibrateFace);
        public static Message SetCalibrateFaceData(string data) => StringContent(VmmCommands.SetCalibrateFaceData, data);
        public static Message SetCalibrateFaceDataHighPower(string data) => StringContent(VmmCommands.SetCalibrateFaceDataHighPower, data);

        public static Message EnableFaceTracking(bool enable) => BoolContent(VmmCommands.EnableFaceTracking, enable);
        public static Message SetCameraDeviceName(string deviceName) => StringContent(VmmCommands.SetCameraDeviceName, deviceName);
        public static Message AutoBlinkDuringFaceTracking(bool enable) => BoolContent(VmmCommands.AutoBlinkDuringFaceTracking, enable);
        public static Message EnableBodyLeanZ(bool enable) => BoolContent(VmmCommands.EnableBodyLeanZ, enable);
        public static Message EnableLipSyncBasedBlinkAdjust(bool enable) => BoolContent(VmmCommands.EnableLipSyncBasedBlinkAdjust, enable);
        public static Message EnableHeadRotationBasedBlinkAdjust(bool enable) => BoolContent(VmmCommands.EnableHeadRotationBasedBlinkAdjust, enable);
        public static Message EnableVoiceBasedMotion(bool enable) => BoolContent(VmmCommands.EnableVoiceBasedMotion, enable);
        //NOTE: falseのほうが普通だよ、という状態にするため、disable云々というやや面倒な言い方になってる事に注意
        public static Message DisableFaceTrackingHorizontalFlip(bool disable) => BoolContent(VmmCommands.DisableFaceTrackingHorizontalFlip, disable);

        public static Message EnableImageBasedHandTracking(bool enable) => BoolContent(VmmCommands.EnableImageBasedHandTracking, enable);
        public static Message ShowEffectDuringHandTracking(bool enable) => BoolContent(VmmCommands.ShowEffectDuringHandTracking, enable);
        //Faceと同じく、disableという言い回しに注意
        public static Message DisableHandTrackingHorizontalFlip(bool disable) => BoolContent(VmmCommands.DisableHandTrackingHorizontalFlip, disable);
        public static Message EnableSendHandTrackingResult(bool enable) => BoolContent(VmmCommands.EnableSendHandTrackingResult, enable);
        // NOTE: 名称に含まないが、xy軸のみのスケールを指す。z軸はそもそもMediaPipeのHandTrackingではあんまり取れないので
        public static Message SetHandTrackingMotionScale(int percent) => IntContent(VmmCommands.SetHandTrackingMotionScale, percent);
        public static Message SetHandTrackingMotionOffsetX(int offset) => IntContent(VmmCommands.SetHandTrackingOffsetX, offset);
        public static Message SetHandTrackingMotionOffsetY(int offset) => IntContent(VmmCommands.SetHandTrackingOffsetY, offset);



        public static Message EnableWebCamHighPowerMode(bool enable) => BoolContent(VmmCommands.EnableWebCamHighPowerMode, enable);

        public static Message FaceDefaultFun(int percentage) => IntContent(VmmCommands.FaceDefaultFun, percentage);
        public static Message FaceNeutralClip(string clipName) => StringContent(VmmCommands.FaceNeutralClip, clipName);
        public static Message FaceOffsetClip(string clipName) => StringContent(VmmCommands.FaceOffsetClip, clipName);

        public static Message DisableBlendShapeInterpolate(bool enable) => BoolContent(VmmCommands.DisableBlendShapeInterpolate, enable);
        
        public static Message EnableWebCameraHighPowerModeLipSync(bool enable) => BoolContent(VmmCommands.EnableWebCameraHighPowerModeLipSync, enable);

        public static Message SetWebCamEyeOpenBlinkValue(int value) => IntContent(VmmCommands.SetWebCamEyeOpenBlinkValue, value);
        public static Message SetWebCamEyeCloseBlinkValue(int value) => IntContent(VmmCommands.SetWebCamEyeCloseBlinkValue, value);
        public static Message SetEyeBlendShapePreviewActive(bool active) => BoolContent(VmmCommands.SetEyeBlendShapePreviewActive, active);
        public static Message SetWebCamEyeApplySameBlinkBothEye(bool enable) => BoolContent(VmmCommands.SetWebCamEyeApplySameBlinkBothEye, enable);
        public static Message SetWebCamEyeApplyCorrectionToPerfectSync(bool enable) 
            => BoolContent(VmmCommands.SetWebCamEyeApplyBlinkCorrectionToPerfectSync, enable);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public static Message CameraDeviceNames() => None(VmmCommands.CameraDeviceNames);

        //public static Message EnableTouchTyping(bool enable) => WithArg(VmmCommands.EnableTouchTyping, enable);

        public static Message EnableLipSync(bool enable) => BoolContent(VmmCommands.EnableLipSync, enable);

        public static Message SetMicrophoneDeviceName(string deviceName) => StringContent(VmmCommands.SetMicrophoneDeviceName, deviceName);
        public static Message SetMicrophoneSensitivity(int sensitivity) => IntContent(VmmCommands.SetMicrophoneSensitivity, sensitivity);
        public static Message SetMicrophoneVolumeVisibility(bool isVisible) => BoolContent(VmmCommands.SetMicrophoneVolumeVisibility, isVisible);
        public static Message AdjustLipSyncByVolume(bool adjust) => BoolContent(VmmCommands.AdjustLipSyncByVolume, adjust);
        
        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public static Message MicrophoneDeviceNames() => None(VmmCommands.MicrophoneDeviceNames);

        public static Message LookAtStyle(string v) => StringContent(VmmCommands.LookAtStyle, v);
        public static Message EnableEyeMotionDuringClipApplied(bool enable) => BoolContent(VmmCommands.EnableEyeMotionDuringClipApplied, enable);
        public static Message SetUseAvatarEyeBoneMap(bool use) => BoolContent(VmmCommands.SetUseAvatarEyeBoneMap, use);
        public static Message SetEyeBoneRotationScale(int percent) => IntContent(VmmCommands.SetEyeBoneRotationScale, percent);
        public static Message SetEyeBoneRotationScaleWithMap(int percent) => IntContent(VmmCommands.SetEyeBoneRotationScaleWithMap, percent);

        #endregion

        #region Game Input Locomotion

        public static Message UseGamepadForGameInput(bool use) => BoolContent(VmmCommands.UseGamepadForGameInput, use);
        public static Message UseKeyboardForGameInput(bool use) => BoolContent(VmmCommands.UseKeyboardForGameInput, use);
        public static Message SetGamepadGameInputKeyAssign(string json) => StringContent(VmmCommands.SetGamepadGameInputKeyAssign, json);
        public static Message SetKeyboardGameInputKeyAssign(string json) => StringContent(VmmCommands.SetKeyboardGameInputKeyAssign, json);

        public static Message SetGameInputLocomotionStyle(int value) => IntContent(VmmCommands.SetGameInputLocomotionStyle, value);
        public static Message EnableAlwaysRunGameInput(bool enable) => BoolContent(VmmCommands.EnableAlwaysRunGameInput, enable);

        //NOTE: 以下はKeyboardKeyAssignの一部に帰着させるかもしれない。
        //これらを単独で送信する場合、これらのオプションはKeyAssignのjsonよりも優先されるイメージ
        public static Message EnableWasdMoveGameInput(bool enable) => BoolContent(VmmCommands.EnableWasdMoveGameInput, enable);
        public static Message EnableArrowKeyMoveGameInput(bool enable) => BoolContent(VmmCommands.EnableArrowKeyMoveGameInput, enable);
        public static Message UseShiftRunGameInput(bool use) => BoolContent(VmmCommands.UseShiftRunGameInput, use);
        public static Message UseSpaceJumpGameInput(bool use) => BoolContent(VmmCommands.UseSpaceJumpGameInput, use);
        public static Message UseMouseMoveForLookAroundGameInput(bool use) => BoolContent(VmmCommands.UseMouseMoveForLookAroundGameInput, use);

        #endregion

        #region カメラの配置

        public static Message CameraFov(int cameraFov) => IntContent(VmmCommands.CameraFov, cameraFov);
        public static Message SetCustomCameraPosition(string posData) => StringContent(VmmCommands.SetCustomCameraPosition, posData);
        public static Message QuickLoadViewPoint(string posData) => StringContent(VmmCommands.QuickLoadViewPoint, posData);

        public static Message EnableFreeCameraMode(bool enable) => BoolContent(VmmCommands.EnableFreeCameraMode, enable);

        public static Message ResetCameraPosition() => None(VmmCommands.ResetCameraPosition);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public static Message CurrentCameraPosition() => None(VmmCommands.CurrentCameraPosition);

        #endregion

        #region キーボード・マウスパッド

        public static Message HidVisibility(bool visible) => BoolContent(VmmCommands.HidVisibility, visible);

        public static Message SetPenVisibility(bool visible) => BoolContent(VmmCommands.SetPenVisibility, visible);

        public static Message MidiControllerVisibility(bool visible) => BoolContent(VmmCommands.MidiControllerVisibility, visible);

        public static Message SetKeyboardTypingEffectType(int typeIndex) => IntContent(VmmCommands.SetKeyboardTypingEffectType, typeIndex);

        // NOTE: Gamepadとかにも全般的に影響する想定のフラグ
        public static Message HideUnusedDevices(bool hide) => BoolContent(VmmCommands.HideUnusedDevices, hide);

        public static Message EnableDeviceFreeLayout(bool enable) => BoolContent(VmmCommands.EnableDeviceFreeLayout, enable);

        public static Message SetDeviceLayout(string data) => StringContent(VmmCommands.SetDeviceLayout, data);

        public static Message ResetDeviceLayout() => None(VmmCommands.ResetDeviceLayout);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        //public static Message CurrentDeviceLayout() => NoArg(VmmCommands.CurrentDeviceLayout);

        #endregion

        #region MIDI

        public static Message EnableMidiRead(bool enable) => BoolContent(VmmCommands.EnableMidiRead, enable);

        #endregion

        #region ゲームパッド

        public static Message EnableGamepad(bool enable) => BoolContent(VmmCommands.EnableGamepad, enable);
        public static Message PreferDirectInputGamepad(bool preferDirectInput) => BoolContent(VmmCommands.PreferDirectInputGamepad, preferDirectInput);
        //public static Message GamepadHeight(int height) => WithArg(VmmCommands.GamepadHeight, height);
        //public static Message GamepadHorizontalScale(int scale) => WithArg(VmmCommands.GamepadHorizontalScale, scale);

        public static Message GamepadVisibility(bool visibility) => BoolContent(VmmCommands.GamepadVisibility, visibility);

        public static Message GamepadLeanMode(string v) => StringContent(VmmCommands.GamepadLeanMode, v);

        public static Message GamepadLeanReverseHorizontal(bool reverse) => BoolContent(VmmCommands.GamepadLeanReverseHorizontal, reverse);
        public static Message GamepadLeanReverseVertical(bool reverse) => BoolContent(VmmCommands.GamepadLeanReverseVertical, reverse);

        #endregion

        #region Light Setting

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public static Message GetQualitySettingsInfo() => None(VmmCommands.GetQualitySettingsInfo);
        public static Message SetImageQuality(string name) => StringContent(VmmCommands.SetImageQuality, name);
        public static Message SetAntiAliasStyle(int style) => IntContent(VmmCommands.SetAntiAliasStyle, style);
        public static Message SetHalfFpsMode(bool enable) => BoolContent(VmmCommands.SetHalfFpsMode, enable);
        public static Message UseFrameReductionEffect(bool enable) => BoolContent(VmmCommands.UseFrameReductionEffect, enable);

        /// <summary>
        /// Query
        /// </summary>
        /// <returns></returns>
        public static Message ApplyDefaultImageQuality() => None(VmmCommands.ApplyDefaultImageQuality);

        public static Message LightColor(int r, int g, int b) => IntArrayContent(VmmCommands.LightColor, [r, g, b]);
        public static Message LightIntensity(int intensityPercent) => IntContent(VmmCommands.LightIntensity, intensityPercent);
        public static Message LightYaw(int angleDeg) => IntContent(VmmCommands.LightYaw, angleDeg);
        public static Message LightPitch(int angleDeg) => IntContent(VmmCommands.LightPitch, angleDeg);
        public static Message UseDesktopLightAdjust(bool use) => BoolContent(VmmCommands.UseDesktopLightAdjust, use);

        public static Message ShadowEnable(bool enable) => BoolContent(VmmCommands.ShadowEnable, enable);
        public static Message ShadowIntensity(int intensityPercent) => IntContent(VmmCommands.ShadowIntensity, intensityPercent);
        public static Message ShadowYaw(int angleDeg) => IntContent(VmmCommands.ShadowYaw, angleDeg);
        public static Message ShadowPitch(int angleDeg) => IntContent(VmmCommands.ShadowPitch, angleDeg);
        public static Message ShadowDepthOffset(int depthCentimeter) => IntContent(VmmCommands.ShadowDepthOffset, depthCentimeter);

        public static Message FixedShadowAlwaysEnable(bool enable) => BoolContent(VmmCommands.FixedShadowAlwaysEnable, enable);
        public static Message FixedShadowWhenLocomotionActiveEnable(bool enable) 
            => BoolContent(VmmCommands.FixedShadowWhenLocomotionActiveEnable, enable);
        public static Message FixedShadowYaw(int angleDeg) => IntContent(VmmCommands.FixedShadowYaw, angleDeg);
        public static Message FixedShadowPitch(int angleDeg) => IntContent(VmmCommands.FixedShadowPitch, angleDeg);

        public static Message BloomColor(int r, int g, int b) => IntArrayContent(VmmCommands.BloomColor, [r, g, b]);
        public static Message BloomIntensity(int intensityPercent) => IntContent(VmmCommands.BloomIntensity, intensityPercent);
        public static Message BloomThreshold(int thresholdPercent) => IntContent(VmmCommands.BloomThreshold, thresholdPercent);

        public static Message AmbientOcclusionEnable(bool enable) => BoolContent(VmmCommands.AmbientOcclusionEnable, enable);
        public static Message AmbientOcclusionIntensity(int intensityPercent) => IntContent(VmmCommands.AmbientOcclusionIntensity, intensityPercent);
        public static Message AmbientOcclusionColor(int r, int g, int b) => IntArrayContent(VmmCommands.AmbientOcclusionColor, [r, g, b]);

        public static Message OutlineEffectEnable(bool active) => BoolContent(VmmCommands.OutlineEffectEnable, active);
        public static Message OutlineEffectThickness(int thickness) => IntContent(VmmCommands.OutlineEffectThickness, thickness);
        public static Message OutlineEffectColor(int r, int g, int b) => IntArrayContent(VmmCommands.OutlineEffectColor, [r, g, b]);
        public static Message OutlineEffectHighQualityMode(bool enable) => BoolContent(VmmCommands.OutlineEffectHighQualityMode, enable);
        
        public static Message WindEnable(bool enableWind) => BoolContent(VmmCommands.WindEnable, enableWind);
        public static Message WindStrength(int strength) => IntContent(VmmCommands.WindStrength, strength);
        public static Message WindInterval(int percentage) => IntContent(VmmCommands.WindInterval, percentage);
        public static Message WindYaw(int windYaw) => IntContent(VmmCommands.WindYaw, windYaw);

        #endregion

        #region Word To Motion

        public static Message ReloadMotionRequests(string content) => StringContent(VmmCommands.ReloadMotionRequests, content);

        //NOTE: 以下の3つはユーザーが動作チェックに使う
        public static Message PlayWordToMotionItem(string word) => StringContent(VmmCommands.PlayWordToMotionItem, word);
        public static Message EnableWordToMotionPreview(bool enable) => BoolContent(VmmCommands.EnableWordToMotionPreview, enable);
        public static Message SendWordToMotionPreviewInfo(string json) => StringContent(VmmCommands.SendWordToMotionPreviewInfo, json);
        public static Message SetDeviceTypeToStartWordToMotion(int deviceType) => IntContent(VmmCommands.SetDeviceTypeToStartWordToMotion, deviceType);

        public static Message LoadMidiNoteToMotionMap(string content) => StringContent(VmmCommands.LoadMidiNoteToMotionMap, content);
        public static Message RequireMidiNoteOnMessage(bool require) => BoolContent(VmmCommands.RequireMidiNoteOnMessage, require);

        public static Message RequestCustomMotionDoctor() => None(VmmCommands.RequestCustomMotionDoctor);

        /// <summary>
        /// Query : 引数をtrueにすると .vrma 形式になってるものだけ返却してくれる
        /// </summary>
        /// <returns></returns>
        public static Message GetAvailableCustomMotionClipNames(bool vrmaOnly) => BoolContent(VmmCommands.GetAvailableCustomMotionClipNames, vrmaOnly);

        #endregion

        #region External Tracker

        //共通: 基本操作のオン/オフ + キャリブレーション
        public static Message ExTrackerEnable(bool enable) => BoolContent(VmmCommands.ExTrackerEnable, enable);
        public static Message ExTrackerEnableLipSync(bool enable) => BoolContent(VmmCommands.ExTrackerEnableLipSync, enable);
        public static Message ExTrackerEnablePerfectSync(bool enable) => BoolContent(VmmCommands.ExTrackerEnablePerfectSync, enable);
        //public static Message ExTrackerUseVRoidDefaultForPerfectSync(bool enable) => WithArg(VmmCommands.ExTrackerUseVRoidDefaultForPerfectSync, enable);
        public static Message ExTrackerCalibrate() => None(VmmCommands.ExTrackerCalibrate);
        //NOTE: このdataについて詳細
        // - Unityが送ってくるのをまるごと保持してたデータを返すだけで、WPF側では中身に関知しない
        // - 想定する内部構成としては、全トラッカータイプのデータが1つの文字列に入ったものを想定してます
        //   (連携先がたかだか5アプリくらいを見込んでるので、これでも手に負えるハズ)
        public static Message ExTrackerSetCalibrateData(string data) => StringContent(VmmCommands.ExTrackerSetCalibrateData, data);

        //連携先の切り替え + アプリ固有設定の送信
        public static Message ExTrackerSetSource(int sourceType) => IntContent(VmmCommands.ExTrackerSetSource, sourceType);
        public static Message ExTrackerSetApplicationValue(ExternalTrackerSettingData data) => StringContent(VmmCommands.ExTrackerSetApplicationValue, data.ToJsonString());

        //共通: 表情スイッチ機能
        //NOTE: 設計を安全にするため、全設定をガッと送る機能しか認めていません。
        public static Message ExTrackerSetFaceSwitchSetting(string settingJson) => StringContent(VmmCommands.ExTrackerSetFaceSwitchSetting, settingJson);

        #endregion

        #region Accessory

        public static Message SetSingleAccessoryLayout(string json) => StringContent(VmmCommands.SetSingleAccessoryLayout, json);
        public static Message SetAccessoryLayout(string json) => StringContent(VmmCommands.SetAccessoryLayout, json);
        public static Message RequestResetAllAccessoryLayout() => None(VmmCommands.RequestResetAllAccessoryLayout);
        public static Message RequestResetAccessoryLayout(string fileNamesJson) => StringContent(VmmCommands.RequestResetAccessoryLayout, fileNamesJson);
        public static Message ReloadAccessoryFiles() => None(VmmCommands.ReloadAccessoryFiles);

        #endregion

        #region Buddy

        public static Message BuddySetInteractionApiEnabled(bool active) => BoolContent(VmmCommands.BuddySetInteractionApiEnabled, active);
        public static Message BuddySetSyncShadowToMainAvatar(bool enabled) => BoolContent(VmmCommands.BuddySetSyncShadowEnabled, enabled);
        public static Message BuddySetDeveloperModeActive(bool active) => BoolContent(VmmCommands.BuddySetDeveloperModeActive, active);
        public static Message BuddySetDeveloperModeLogLevel(int level) => IntContent(VmmCommands.BuddySetDeveloperModeLogLevel, level);

        // NOTE: 単にon/offすることに加え、同一folderを立て続けにDisable => Enableすると実質リロードとして動くのも期待してる
        public static Message BuddyDisable(string folder) => StringContent(VmmCommands.BuddyDisable, folder);
        public static Message BuddyEnable(string folder) => StringContent(VmmCommands.BuddyEnable, folder);

        /// <summary>
        /// BuddySetPropertyと違い、1Buddyぶんの全プロパティ値をリフレッシュ指示として送る
        /// プロパティを空配列で渡すと実質的にデータ削除みたいなことも可能
        /// </summary>
        /// <param name="valueJson"></param>
        /// <returns></returns>
        public static Message BuddyRefreshData(string valueJson) => StringContent(VmmCommands.BuddyRefreshData, valueJson);
        
        /// <summary>
        /// 特定のBuddyの1プロパティだけ編集したときの送信に使うやつ
        /// </summary>
        /// <param name="valueJson"></param>
        /// <returns></returns>
        public static Message BuddySetProperty(string valueJson) => StringContent(VmmCommands.BuddySetProperty, valueJson);

        /// <summary>
        /// Actionを指すBuddyのプロパティ(UI上はボタン)の押下をUnityに通知するやつ
        /// </summary>
        /// <param name="valueJson"></param>
        /// <returns></returns>
        public static Message BuddyInvokeAction(string valueJson) => StringContent(VmmCommands.BuddyInvokeAction, valueJson);

        #endregion

        #region VMCP

        public static Message EnableVMCP(bool enable) => BoolContent(VmmCommands.EnableVMCP, enable);
        public static Message SetVMCPSources(string json) => StringContent(VmmCommands.SetVMCPSources, json);
        public static Message EnableVMCPReceiveLerp(bool enable) => BoolContent(VmmCommands.EnableVMCPPoseLerp, enable);
        public static Message EnableVMCPUpperBodyAdditionalMove(bool enable) => BoolContent(VmmCommands.EnableVMCPUpperBodyAdditionalMove, enable);

        public static Message EnableVMCPSend(bool enable) => BoolContent(VmmCommands.EnableVMCPSend, enable);
        public static Message SetVMCPSendSettings(string json) => StringContent(VmmCommands.SetVMCPSendSettings, json);
        public static Message ShowEffectDuringVMCPSendEnabled(bool enable) => BoolContent(VmmCommands.ShowEffectDuringVMCPSendEnabled, enable);

        #endregion

        #region その他

        public static Message TakeScreenshot() => None(VmmCommands.TakeScreenshot);
        public static Message OpenScreenshotFolder() => None(VmmCommands.OpenScreenshotFolder);

        #endregion

        #region VRoid SDK

        public static Message OpenVRoidSdkUi() => None(VmmCommands.OpenVRoidSdkUi);
        public static Message RequestLoadVRoidWithId(string modelId) => StringContent(VmmCommands.RequestLoadVRoidWithId, modelId);

        #endregion

        #region Debug

        public static Message DebugSendLargeData(string data) => StringContent(VmmCommands.DebugSendLargeData, data);

        #endregion
    }
}

#pragma warning restore CA1822