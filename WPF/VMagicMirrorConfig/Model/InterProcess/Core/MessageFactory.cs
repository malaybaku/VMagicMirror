using Baku.VMagicMirror;
using System.Collections.Generic;

namespace Baku.VMagicMirrorConfig
{
    // NOTE: できればこのクラスを削除したい。VmmCommandsをenum化したことによってFactoryを使う意義がだいぶ無くなっている
    // ただし、引数を絞れるのはメリットではあるし、削除しなくても破綻はしない
    class MessageFactory
    {
        private static MessageFactory? _instance;
        public static MessageFactory Instance
            => _instance ??= new MessageFactory();
        private MessageFactory() { }

        private static Message NoArg(VmmCommands command) => Message.None(command);

        private static Message WithArg(VmmCommands command, string content) => Message.String(command, content);

        private static Message WithArg(VmmCommands command, bool content) 
            => Message.Bool(command, content);

        private static Message WithArg(VmmCommands command, int content) => Message.Int(VmmCommands.Unknown, content);

        public Message Language(string langName) => WithArg(VmmCommands.Language, langName);

        #region HID Input

        //public Message KeyDown(string keyName) => WithArg(VmmCommands.KeyDown, keyName);
        public Message MouseButton(string info) => WithArg(VmmCommands.MouseButton, info);
        //public Message MouseMoved(int x, int y) => WithArg(VmmCommands.MouseMoved, $"{x},{y}");

        #endregion

        #region VRM Load

        public Message OpenVrmPreview(string filePath) => WithArg(VmmCommands.OpenVrmPreview, filePath);
        public Message OpenVrm(string filePath) => WithArg(VmmCommands.OpenVrm, filePath);
        //public Message AccessToVRoidHub() => NoArg(VmmCommands.AccessToVRoidHub);

        public Message CancelLoadVrm() => NoArg(VmmCommands.CancelLoadVrm);

        public Message RequestAutoAdjust() => NoArg(VmmCommands.RequestAutoAdjust);

        #endregion

        #region ウィンドウ

        public Message Chromakey(int a, int r, int g, int b) => WithArg(VmmCommands.Chromakey, $"{a},{r},{g},{b}");

        public Message WindowFrameVisibility(bool v) => WithArg(VmmCommands.WindowFrameVisibility, v);
        public Message IgnoreMouse(bool v) => WithArg(VmmCommands.IgnoreMouse, v);
        public Message TopMost(bool v) => WithArg(VmmCommands.TopMost, v);
        public Message WindowDraggable(bool v) => WithArg(VmmCommands.WindowDraggable, v);

        /// <summary>
        /// NOTE: 空文字列なら背景画像を外す処理をやる。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Message SetBackgroundImagePath(string path) => WithArg(VmmCommands.SetBackgroundImagePath, path);

        public Message MoveWindow(int x, int y) => WithArg(VmmCommands.MoveWindow, $"{x},{y}");
        public Message ResetWindowSize() => NoArg(VmmCommands.ResetWindowSize);

        public Message SetWholeWindowTransparencyLevel(int level) => WithArg(VmmCommands.SetWholeWindowTransparencyLevel, level);

        public Message SetAlphaValueOnTransparent(int alpha) => WithArg(VmmCommands.SetAlphaValueOnTransparent, alpha);

        public Message EnableSpoutOutput(bool enable) => WithArg(VmmCommands.EnableSpoutOutput, enable);
        public Message SetSpoutOutputResolution(int type) => WithArg(VmmCommands.SetSpoutOutputResolution, type);


        public Message StartupEnded() => NoArg(VmmCommands.StartupEnded);

        #endregion

        #region モーション

        public Message EnableNoHandTrackMode(bool enable) => WithArg(VmmCommands.EnableNoHandTrackMode, enable);
        public Message EnableGameInputLocomotionMode(bool enable) => WithArg(VmmCommands.EnableGameInputLocomotionMode, enable);
        public Message EnableTwistBodyMotion(bool enable) => WithArg(VmmCommands.EnableTwistBodyMotion, enable);

        public Message EnableCustomHandDownPose(bool enable) => WithArg(VmmCommands.EnableCustomHandDownPose, enable);
        public Message SetHandDownModeCustomPose(string poseJson) => WithArg(VmmCommands.SetHandDownModeCustomPose, poseJson);
        public Message ResetCustomHandDownPose() => NoArg(VmmCommands.ResetCustomHandDownPose);


        public Message LengthFromWristToTip(int lengthCentimeter) => WithArg(VmmCommands.LengthFromWristToTip, lengthCentimeter);

        public Message HandYOffsetBasic(int offsetCentimeter) => WithArg(VmmCommands.HandYOffsetBasic, offsetCentimeter);
        public Message HandYOffsetAfterKeyDown(int offsetCentimeter) => WithArg(VmmCommands.HandYOffsetAfterKeyDown, offsetCentimeter);

        public Message EnableHidRandomTyping(bool enable) => WithArg(VmmCommands.EnableHidRandomTyping, enable);
        public Message EnableShoulderMotionModify(bool enable) => WithArg(VmmCommands.EnableShoulderMotionModify, enable);
        public Message EnableTypingHandDownTimeout(bool enable) => WithArg(VmmCommands.EnableTypingHandDownTimeout, enable);
        public Message SetWaistWidth(int waistWidthCentimeter) => WithArg(VmmCommands.SetWaistWidth, waistWidthCentimeter);
        public Message SetElbowCloseStrength(int strengthPercent) => WithArg(VmmCommands.SetElbowCloseStrength, strengthPercent);

        public Message EnableFpsAssumedRightHand(bool enable) => WithArg(VmmCommands.EnableFpsAssumedRightHand, enable);
        public Message PresentationArmRadiusMin(int radiusMinCentimeter) => WithArg(VmmCommands.PresentationArmRadiusMin, radiusMinCentimeter);

        public Message SetKeyboardAndMouseMotionMode(int modeIndex) => WithArg(VmmCommands.SetKeyboardAndMouseMotionMode, modeIndex);
        public Message SetGamepadMotionMode(int modeIndex) => WithArg(VmmCommands.SetGamepadMotionMode, modeIndex);

        public Message EnableWaitMotion(bool enable) => WithArg(VmmCommands.EnableWaitMotion, enable);
        public Message WaitMotionScale(int scalePercent) => WithArg(VmmCommands.WaitMotionScale, scalePercent);
        public Message WaitMotionPeriod(int periodSec) => WithArg(VmmCommands.WaitMotionPeriod, periodSec);

        // NOTE: Unity側の状態によって実際に行うキャリブレーションは変わる(低負荷/高負荷では別々のキャリブレーションを行う)
        public Message CalibrateFace() => NoArg(VmmCommands.CalibrateFace);
        public Message SetCalibrateFaceData(string data) => WithArg(VmmCommands.SetCalibrateFaceData, data);
        public Message SetCalibrateFaceDataHighPower(string data) => WithArg(VmmCommands.SetCalibrateFaceDataHighPower, data);

        public Message EnableFaceTracking(bool enable) => WithArg(VmmCommands.EnableFaceTracking, enable);
        public Message SetCameraDeviceName(string deviceName) => WithArg(VmmCommands.SetCameraDeviceName, deviceName);
        public Message AutoBlinkDuringFaceTracking(bool enable) => WithArg(VmmCommands.AutoBlinkDuringFaceTracking, enable);
        public Message EnableBodyLeanZ(bool enable) => WithArg(VmmCommands.EnableBodyLeanZ, enable);
        public Message EnableLipSyncBasedBlinkAdjust(bool enable) => WithArg(VmmCommands.EnableLipSyncBasedBlinkAdjust, enable);
        public Message EnableHeadRotationBasedBlinkAdjust(bool enable) => WithArg(VmmCommands.EnableHeadRotationBasedBlinkAdjust, enable);
        public Message EnableVoiceBasedMotion(bool enable) => WithArg(VmmCommands.EnableVoiceBasedMotion, enable);
        //NOTE: falseのほうが普通だよ、という状態にするため、disable云々というやや面倒な言い方になってる事に注意
        public Message DisableFaceTrackingHorizontalFlip(bool disable) => WithArg(VmmCommands.DisableFaceTrackingHorizontalFlip, disable);

        public Message EnableImageBasedHandTracking(bool enable) => WithArg(VmmCommands.EnableImageBasedHandTracking, enable);
        public Message ShowEffectDuringHandTracking(bool enable) => WithArg(VmmCommands.ShowEffectDuringHandTracking, enable);
        //Faceと同じく、disableという言い回しに注意
        public Message DisableHandTrackingHorizontalFlip(bool disable) => WithArg(VmmCommands.DisableHandTrackingHorizontalFlip, disable);
        public Message EnableSendHandTrackingResult(bool enable) => WithArg(VmmCommands.EnableSendHandTrackingResult, enable);


        public Message EnableWebCamHighPowerMode(bool enable) => WithArg(VmmCommands.EnableWebCamHighPowerMode, enable);

        public Message FaceDefaultFun(int percentage) => WithArg(VmmCommands.FaceDefaultFun, percentage);
        public Message FaceNeutralClip(string clipName) => WithArg(VmmCommands.FaceNeutralClip, clipName);
        public Message FaceOffsetClip(string clipName) => WithArg(VmmCommands.FaceOffsetClip, clipName);

        public Message DisableBlendShapeInterpolate(bool enable) => WithArg(VmmCommands.DisableBlendShapeInterpolate, enable);
        
        public Message UsePerfectSyncWithWebCamera(bool enable) => WithArg(VmmCommands.UsePerfectSyncWithWebCamera, enable);
        
        public Message EnableWebCameraHighPowerModeBlink(bool enable) => WithArg(VmmCommands.EnableWebCameraHighPowerModeBlink, enable);
        public Message EnableWebCameraHighPowerModeLipSync(bool enable) => WithArg(VmmCommands.EnableWebCameraHighPowerModeLipSync, enable);
        public Message EnableWebCameraHighPowerModeMoveZ(bool enable) => WithArg(VmmCommands.EnableWebCameraHighPowerModeMoveZ, enable);

        public Message SetWebCamEyeOpenBlinkValue(int value) => WithArg(VmmCommands.SetWebCamEyeOpenBlinkValue, value);
        public Message SetWebCamEyeCloseBlinkValue(int value) => WithArg(VmmCommands.SetWebCamEyeCloseBlinkValue, value);

        public Message SetEyeBlendShapePreviewActive(bool active) => WithArg(VmmCommands.SetEyeBlendShapePreviewActive, active);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message CameraDeviceNames() => NoArg(VmmCommands.CameraDeviceNames);

        //public Message EnableTouchTyping(bool enable) => WithArg(VmmCommands.EnableTouchTyping, enable);

        public Message EnableLipSync(bool enable) => WithArg(VmmCommands.EnableLipSync, enable);

        public Message SetMicrophoneDeviceName(string deviceName) => WithArg(VmmCommands.SetMicrophoneDeviceName, deviceName);
        public Message SetMicrophoneSensitivity(int sensitivity) => WithArg(VmmCommands.SetMicrophoneSensitivity, sensitivity);
        public Message SetMicrophoneVolumeVisibility(bool isVisible) => WithArg(VmmCommands.SetMicrophoneVolumeVisibility, isVisible);
        public Message AdjustLipSyncByVolume(bool adjust) => WithArg(VmmCommands.AdjustLipSyncByVolume, adjust);
        
        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message MicrophoneDeviceNames() => NoArg(VmmCommands.MicrophoneDeviceNames);

        public Message LookAtStyle(string v) => WithArg(VmmCommands.LookAtStyle, v);
        public Message EnableEyeMotionDuringClipApplied(bool enable) => WithArg(VmmCommands.EnableEyeMotionDuringClipApplied, enable);
        public Message SetUseAvatarEyeBoneMap(bool use) => WithArg(VmmCommands.SetUseAvatarEyeBoneMap, use);
        public Message SetEyeBoneRotationScale(int percent) => WithArg(VmmCommands.SetEyeBoneRotationScale, percent);
        public Message SetEyeBoneRotationScaleWithMap(int percent) => WithArg(VmmCommands.SetEyeBoneRotationScaleWithMap, percent);

        #endregion

        #region Game Input Locomotion

        public Message UseGamepadForGameInput(bool use) => WithArg(VmmCommands.UseGamepadForGameInput, use);
        public Message UseKeyboardForGameInput(bool use) => WithArg(VmmCommands.UseKeyboardForGameInput, use);
        public Message SetGamepadGameInputKeyAssign(string json) => WithArg(VmmCommands.SetGamepadGameInputKeyAssign, json);
        public Message SetKeyboardGameInputKeyAssign(string json) => WithArg(VmmCommands.SetKeyboardGameInputKeyAssign, json);

        public Message SetGameInputLocomotionStyle(int value) => WithArg(VmmCommands.SetGameInputLocomotionStyle, value);
        public Message EnableAlwaysRunGameInput(bool enable) => WithArg(VmmCommands.EnableAlwaysRunGameInput, enable);

        //NOTE: 以下はKeyboardKeyAssignの一部に帰着させるかもしれない。
        //これらを単独で送信する場合、これらのオプションはKeyAssignのjsonよりも優先されるイメージ
        public Message EnableWasdMoveGameInput(bool enable) => WithArg(VmmCommands.EnableWasdMoveGameInput, enable);
        public Message EnableArrowKeyMoveGameInput(bool enable) => WithArg(VmmCommands.EnableArrowKeyMoveGameInput, enable);
        public Message UseShiftRunGameInput(bool use) => WithArg(VmmCommands.UseShiftRunGameInput, use);
        public Message UseSpaceJumpGameInput(bool use) => WithArg(VmmCommands.UseSpaceJumpGameInput, use);
        public Message UseMouseMoveForLookAroundGameInput(bool use) => WithArg(VmmCommands.UseMouseMoveForLookAroundGameInput, use);

        #endregion

        #region カメラの配置

        public Message CameraFov(int cameraFov) => WithArg(VmmCommands.CameraFov, cameraFov);
        public Message SetCustomCameraPosition(string posData) => WithArg(VmmCommands.SetCustomCameraPosition, posData);
        public Message QuickLoadViewPoint(string posData) => WithArg(VmmCommands.QuickLoadViewPoint, posData);

        public Message EnableFreeCameraMode(bool enable) => WithArg(VmmCommands.EnableFreeCameraMode, enable);

        public Message ResetCameraPosition() => NoArg(VmmCommands.ResetCameraPosition);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message CurrentCameraPosition() => NoArg(VmmCommands.CurrentCameraPosition);

        #endregion

        #region キーボード・マウスパッド

        public Message HidVisibility(bool visible) => WithArg(VmmCommands.HidVisibility, visible);

        public Message SetPenVisibility(bool visible) => WithArg(VmmCommands.SetPenVisibility, visible);

        public Message MidiControllerVisibility(bool visible) => WithArg(VmmCommands.MidiControllerVisibility, visible);

        public Message SetKeyboardTypingEffectType(int typeIndex) => WithArg(VmmCommands.SetKeyboardTypingEffectType, typeIndex);

        // NOTE: Gamepadとかにも全般的に影響する想定のフラグ
        public Message HideUnusedDevices(bool hide) => WithArg(VmmCommands.HideUnusedDevices, hide);

        public Message EnableDeviceFreeLayout(bool enable) => WithArg(VmmCommands.EnableDeviceFreeLayout, enable);

        public Message SetDeviceLayout(string data) => WithArg(VmmCommands.SetDeviceLayout, data);

        public Message ResetDeviceLayout() => NoArg(VmmCommands.ResetDeviceLayout);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        //public Message CurrentDeviceLayout() => NoArg(VmmCommands.CurrentDeviceLayout);

        #endregion

        #region MIDI

        public Message EnableMidiRead(bool enable) => WithArg(VmmCommands.EnableMidiRead, enable);

        #endregion

        #region ゲームパッド

        public Message EnableGamepad(bool enable) => WithArg(VmmCommands.EnableGamepad, enable);
        public Message PreferDirectInputGamepad(bool preferDirectInput) => WithArg(VmmCommands.PreferDirectInputGamepad, preferDirectInput);
        //public Message GamepadHeight(int height) => WithArg(VmmCommands.GamepadHeight, height);
        //public Message GamepadHorizontalScale(int scale) => WithArg(VmmCommands.GamepadHorizontalScale, scale);

        public Message GamepadVisibility(bool visibility) => WithArg(VmmCommands.GamepadVisibility, visibility);

        public Message GamepadLeanMode(string v) => WithArg(VmmCommands.GamepadLeanMode, v);

        public Message GamepadLeanReverseHorizontal(bool reverse) => WithArg(VmmCommands.GamepadLeanReverseHorizontal, reverse);
        public Message GamepadLeanReverseVertical(bool reverse) => WithArg(VmmCommands.GamepadLeanReverseVertical, reverse);

        #endregion

        #region Light Setting

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message GetQualitySettingsInfo() => NoArg(VmmCommands.GetQualitySettingsInfo);
        public Message SetImageQuality(string name) => WithArg(VmmCommands.SetImageQuality, name);
        public Message SetAntiAliasStyle(int style) => WithArg(VmmCommands.SetAntiAliasStyle, style);
        public Message SetHalfFpsMode(bool enable) => WithArg(VmmCommands.SetHalfFpsMode, enable);
        public Message UseFrameReductionEffect(bool enable) => WithArg(VmmCommands.UseFrameReductionEffect, enable);

        /// <summary>
        /// Query
        /// </summary>
        /// <returns></returns>
        public Message ApplyDefaultImageQuality() => NoArg(VmmCommands.ApplyDefaultImageQuality);

        public Message LightColor(int r, int g, int b) => WithArg(VmmCommands.LightColor, $"{r},{g},{b}");
        public Message LightIntensity(int intensityPercent) => WithArg(VmmCommands.LightIntensity, intensityPercent);
        public Message LightYaw(int angleDeg) => WithArg(VmmCommands.LightYaw, angleDeg);
        public Message LightPitch(int angleDeg) => WithArg(VmmCommands.LightPitch, angleDeg);
        public Message UseDesktopLightAdjust(bool use) => WithArg(VmmCommands.UseDesktopLightAdjust, use);

        public Message ShadowEnable(bool enable) => WithArg(VmmCommands.ShadowEnable, enable);
        public Message ShadowIntensity(int intensityPercent) => WithArg(VmmCommands.ShadowIntensity, intensityPercent);
        public Message ShadowYaw(int angleDeg) => WithArg(VmmCommands.ShadowYaw, angleDeg);
        public Message ShadowPitch(int angleDeg) => WithArg(VmmCommands.ShadowPitch, angleDeg);
        public Message ShadowDepthOffset(int depthCentimeter) => WithArg(VmmCommands.ShadowDepthOffset, depthCentimeter);

        public Message BloomColor(int r, int g, int b) => WithArg(VmmCommands.BloomColor, $"{r},{g},{b}");
        public Message BloomIntensity(int intensityPercent) => WithArg(VmmCommands.BloomIntensity, intensityPercent);
        public Message BloomThreshold(int thresholdPercent) => WithArg(VmmCommands.BloomThreshold, thresholdPercent);

        public Message AmbientOcclusionEnable(bool enable) => WithArg(VmmCommands.AmbientOcclusionEnable, enable);
        public Message AmbientOcclusionIntensity(int intensityPercent) => WithArg(VmmCommands.AmbientOcclusionIntensity, intensityPercent);
        public Message AmbientOcclusionColor(int r, int g, int b) => WithArg(VmmCommands.AmbientOcclusionColor, $"{r},{g},{b}");

        public Message OutlineEffectEnable(bool active) => WithArg(VmmCommands.OutlineEffectEnable, active);
        public Message OutlineEffectThickness(int thickness) => WithArg(VmmCommands.OutlineEffectThickness, thickness);
        public Message OutlineEffectColor(int r, int g, int b) => WithArg(VmmCommands.OutlineEffectColor, $"{r},{g},{b}");
        public Message OutlineEffectHighQualityMode(bool enable) => WithArg(VmmCommands.OutlineEffectHighQualityMode, enable);
        
        public Message WindEnable(bool enableWind) => WithArg(VmmCommands.WindEnable, enableWind);
        public Message WindStrength(int strength) => WithArg(VmmCommands.WindStrength, strength);
        public Message WindInterval(int percentage) => WithArg(VmmCommands.WindInterval, percentage);
        public Message WindYaw(int windYaw) => WithArg(VmmCommands.WindYaw, windYaw);

        #endregion

        #region Word To Motion

        public Message ReloadMotionRequests(string content) => WithArg(VmmCommands.ReloadMotionRequests, content);

        //NOTE: 以下の3つはユーザーが動作チェックに使う
        public Message PlayWordToMotionItem(string word) => WithArg(VmmCommands.PlayWordToMotionItem, word);
        public Message EnableWordToMotionPreview(bool enable) => WithArg(VmmCommands.EnableWordToMotionPreview, enable);
        public Message SendWordToMotionPreviewInfo(string json) => WithArg(VmmCommands.SendWordToMotionPreviewInfo, json);
        public Message SetDeviceTypeToStartWordToMotion(int deviceType) => WithArg(VmmCommands.SetDeviceTypeToStartWordToMotion, deviceType);

        public Message LoadMidiNoteToMotionMap(string content) => WithArg(VmmCommands.LoadMidiNoteToMotionMap, content);
        public Message RequireMidiNoteOnMessage(bool require) => WithArg(VmmCommands.RequireMidiNoteOnMessage, require);

        public Message RequestCustomMotionDoctor() => NoArg(VmmCommands.RequestCustomMotionDoctor);

        /// <summary>
        /// Query : 引数をtrueにすると .vrma 形式になってるものだけ返却してくれる
        /// </summary>
        /// <returns></returns>
        public Message GetAvailableCustomMotionClipNames(bool vrmaOnly) => WithArg(VmmCommands.GetAvailableCustomMotionClipNames, vrmaOnly);

        #endregion

        #region External Tracker

        //共通: 基本操作のオン/オフ + キャリブレーション
        public Message ExTrackerEnable(bool enable) => WithArg(VmmCommands.ExTrackerEnable, enable);
        public Message ExTrackerEnableLipSync(bool enable) => WithArg(VmmCommands.ExTrackerEnableLipSync, enable);
        public Message ExTrackerEnablePerfectSync(bool enable) => WithArg(VmmCommands.ExTrackerEnablePerfectSync, enable);
        //public Message ExTrackerUseVRoidDefaultForPerfectSync(bool enable) => WithArg(VmmCommands.ExTrackerUseVRoidDefaultForPerfectSync, enable);
        public Message ExTrackerCalibrate() => NoArg(VmmCommands.ExTrackerCalibrate);
        //NOTE: このdataについて詳細
        // - Unityが送ってくるのをまるごと保持してたデータを返すだけで、WPF側では中身に関知しない
        // - 想定する内部構成としては、全トラッカータイプのデータが1つの文字列に入ったものを想定してます
        //   (連携先がたかだか5アプリくらいを見込んでるので、これでも手に負えるハズ)
        public Message ExTrackerSetCalibrateData(string data) => WithArg(VmmCommands.ExTrackerSetCalibrateData, data);

        //連携先の切り替え + アプリ固有設定の送信
        public Message ExTrackerSetSource(int sourceType) => WithArg(VmmCommands.ExTrackerSetSource, sourceType);
        public Message ExTrackerSetApplicationValue(ExternalTrackerSettingData data) => WithArg(VmmCommands.ExTrackerSetApplicationValue, data.ToJsonString());

        //共通: 表情スイッチ機能
        //NOTE: 設計を安全にするため、全設定をガッと送る機能しか認めていません。
        public Message ExTrackerSetFaceSwitchSetting(string settingJson) => WithArg(VmmCommands.ExTrackerSetFaceSwitchSetting, settingJson);

        #endregion

        #region Accessory

        public Message SetSingleAccessoryLayout(string json) => WithArg(VmmCommands.SetSingleAccessoryLayout, json);
        public Message SetAccessoryLayout(string json) => WithArg(VmmCommands.SetAccessoryLayout, json);
        public Message RequestResetAllAccessoryLayout() => NoArg(VmmCommands.RequestResetAllAccessoryLayout);
        public Message RequestResetAccessoryLayout(string fileNamesJson) => WithArg(VmmCommands.RequestResetAccessoryLayout, fileNamesJson);
        public Message ReloadAccessoryFiles() => NoArg(VmmCommands.ReloadAccessoryFiles);

        #endregion

        #region VMCP

        public Message EnableVMCP(bool enable) => WithArg(VmmCommands.EnableVMCP, enable);
        public Message SetVMCPSources(string json) => WithArg(VmmCommands.SetVMCPSources, json);
        public Message SetDisableCameraDuringVMCPActive(bool disable) => WithArg(VmmCommands.SetDisableCameraDuringVMCPActive, disable);
        public Message SetVMCPNaiveBoneTransfer(bool enable) => WithArg(VmmCommands.SetVMCPNaiveBoneTransfer, enable);

        public Message EnableVMCPSend(bool enable) => WithArg(VmmCommands.EnableVMCPSend, enable);
        public Message SetVMCPSendSettings(string json) => WithArg(VmmCommands.SetVMCPSendSettings, json);
        public Message ShowEffectDuringVMCPSendEnabled(bool enable) => WithArg(VmmCommands.ShowEffectDuringVMCPSendEnabled, enable);

        #endregion

        #region その他

        public Message TakeScreenshot() => NoArg(VmmCommands.TakeScreenshot);
        public Message OpenScreenshotFolder() => NoArg(VmmCommands.OpenScreenshotFolder);

        #endregion

        #region VRoid SDK

        public Message OpenVRoidSdkUi() => NoArg(VmmCommands.OpenVRoidSdkUi);
        public Message RequestLoadVRoidWithId(string modelId) => WithArg(VmmCommands.RequestLoadVRoidWithId, modelId);

        #endregion

        #region Debug

        public Message DebugSendLargeData(string data) => WithArg(VmmCommands.DebugSendLargeData, data);

        #endregion

        #region メタメッセージ

        /// <summary>
        /// クエリでないコマンドをひとまとめにしたコマンド。
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        /// <remarks>
        /// 狙い: 投げっぱなしのコマンドを集約してひとまとめで送る。
        /// クエリは個別にawaitしてほしい関係でココに混ぜるのはちょっと難しい
        /// </remarks>
        public Message CommandArray(IEnumerable<Message> commands)
            => WithArg(VmmCommands.CommandArray, CommandArrayBuilder.BuildCommandArrayString(commands));

        #endregion
    }
}