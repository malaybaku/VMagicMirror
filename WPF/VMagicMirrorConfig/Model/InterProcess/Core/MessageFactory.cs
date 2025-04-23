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

        private static Message None(VmmCommands command) => Message.None(command);

        private static Message StringContent(VmmCommands command, string content) => Message.String(command, content);

        private static Message BoolContent(VmmCommands command, bool content) 
            => Message.Bool(command, content);

        private static Message IntContent(VmmCommands command, int content) => Message.Int(VmmCommands.Unknown, content);

        public Message Language(string langName) => StringContent(VmmCommands.Language, langName);

        #region HID Input

        //public Message KeyDown(string keyName) => WithArg(VmmCommands.KeyDown, keyName);
        public Message MouseButton(string info) => StringContent(VmmCommands.MouseButton, info);
        //public Message MouseMoved(int x, int y) => WithArg(VmmCommands.MouseMoved, $"{x},{y}");

        #endregion

        #region VRM Load

        public Message OpenVrmPreview(string filePath) => StringContent(VmmCommands.OpenVrmPreview, filePath);
        public Message OpenVrm(string filePath) => StringContent(VmmCommands.OpenVrm, filePath);
        //public Message AccessToVRoidHub() => NoArg(VmmCommands.AccessToVRoidHub);

        public Message CancelLoadVrm() => None(VmmCommands.CancelLoadVrm);

        public Message RequestAutoAdjust() => None(VmmCommands.RequestAutoAdjust);

        #endregion

        #region ウィンドウ

        public Message Chromakey(int a, int r, int g, int b) => StringContent(VmmCommands.Chromakey, $"{a},{r},{g},{b}");

        public Message WindowFrameVisibility(bool v) => BoolContent(VmmCommands.WindowFrameVisibility, v);
        public Message IgnoreMouse(bool v) => BoolContent(VmmCommands.IgnoreMouse, v);
        public Message TopMost(bool v) => BoolContent(VmmCommands.TopMost, v);
        public Message WindowDraggable(bool v) => BoolContent(VmmCommands.WindowDraggable, v);

        /// <summary>
        /// NOTE: 空文字列なら背景画像を外す処理をやる。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Message SetBackgroundImagePath(string path) => StringContent(VmmCommands.SetBackgroundImagePath, path);

        public Message MoveWindow(int x, int y) => StringContent(VmmCommands.MoveWindow, $"{x},{y}");
        public Message ResetWindowSize() => None(VmmCommands.ResetWindowSize);

        public Message SetWholeWindowTransparencyLevel(int level) => IntContent(VmmCommands.SetWholeWindowTransparencyLevel, level);

        public Message SetAlphaValueOnTransparent(int alpha) => IntContent(VmmCommands.SetAlphaValueOnTransparent, alpha);

        public Message EnableSpoutOutput(bool enable) => BoolContent(VmmCommands.EnableSpoutOutput, enable);
        public Message SetSpoutOutputResolution(int type) => IntContent(VmmCommands.SetSpoutOutputResolution, type);


        public Message StartupEnded() => None(VmmCommands.StartupEnded);

        #endregion

        #region モーション

        public Message EnableNoHandTrackMode(bool enable) => BoolContent(VmmCommands.EnableNoHandTrackMode, enable);
        public Message EnableGameInputLocomotionMode(bool enable) => BoolContent(VmmCommands.EnableGameInputLocomotionMode, enable);
        public Message EnableTwistBodyMotion(bool enable) => BoolContent(VmmCommands.EnableTwistBodyMotion, enable);

        public Message EnableCustomHandDownPose(bool enable) => BoolContent(VmmCommands.EnableCustomHandDownPose, enable);
        public Message SetHandDownModeCustomPose(string poseJson) => StringContent(VmmCommands.SetHandDownModeCustomPose, poseJson);
        public Message ResetCustomHandDownPose() => None(VmmCommands.ResetCustomHandDownPose);


        public Message LengthFromWristToTip(int lengthCentimeter) => IntContent(VmmCommands.LengthFromWristToTip, lengthCentimeter);

        public Message HandYOffsetBasic(int offsetCentimeter) => IntContent(VmmCommands.HandYOffsetBasic, offsetCentimeter);
        public Message HandYOffsetAfterKeyDown(int offsetCentimeter) => IntContent(VmmCommands.HandYOffsetAfterKeyDown, offsetCentimeter);

        public Message EnableHidRandomTyping(bool enable) => BoolContent(VmmCommands.EnableHidRandomTyping, enable);
        public Message EnableShoulderMotionModify(bool enable) => BoolContent(VmmCommands.EnableShoulderMotionModify, enable);
        public Message EnableTypingHandDownTimeout(bool enable) => BoolContent(VmmCommands.EnableTypingHandDownTimeout, enable);
        public Message SetWaistWidth(int waistWidthCentimeter) => IntContent(VmmCommands.SetWaistWidth, waistWidthCentimeter);
        public Message SetElbowCloseStrength(int strengthPercent) => IntContent(VmmCommands.SetElbowCloseStrength, strengthPercent);

        public Message EnableFpsAssumedRightHand(bool enable) => BoolContent(VmmCommands.EnableFpsAssumedRightHand, enable);
        public Message PresentationArmRadiusMin(int radiusMinCentimeter) => IntContent(VmmCommands.PresentationArmRadiusMin, radiusMinCentimeter);

        public Message SetKeyboardAndMouseMotionMode(int modeIndex) => IntContent(VmmCommands.SetKeyboardAndMouseMotionMode, modeIndex);
        public Message SetGamepadMotionMode(int modeIndex) => IntContent(VmmCommands.SetGamepadMotionMode, modeIndex);

        public Message EnableWaitMotion(bool enable) => BoolContent(VmmCommands.EnableWaitMotion, enable);
        public Message WaitMotionScale(int scalePercent) => IntContent(VmmCommands.WaitMotionScale, scalePercent);
        public Message WaitMotionPeriod(int periodSec) => IntContent(VmmCommands.WaitMotionPeriod, periodSec);

        // NOTE: Unity側の状態によって実際に行うキャリブレーションは変わる(低負荷/高負荷では別々のキャリブレーションを行う)
        public Message CalibrateFace() => None(VmmCommands.CalibrateFace);
        public Message SetCalibrateFaceData(string data) => StringContent(VmmCommands.SetCalibrateFaceData, data);
        public Message SetCalibrateFaceDataHighPower(string data) => StringContent(VmmCommands.SetCalibrateFaceDataHighPower, data);

        public Message EnableFaceTracking(bool enable) => BoolContent(VmmCommands.EnableFaceTracking, enable);
        public Message SetCameraDeviceName(string deviceName) => StringContent(VmmCommands.SetCameraDeviceName, deviceName);
        public Message AutoBlinkDuringFaceTracking(bool enable) => BoolContent(VmmCommands.AutoBlinkDuringFaceTracking, enable);
        public Message EnableBodyLeanZ(bool enable) => BoolContent(VmmCommands.EnableBodyLeanZ, enable);
        public Message EnableLipSyncBasedBlinkAdjust(bool enable) => BoolContent(VmmCommands.EnableLipSyncBasedBlinkAdjust, enable);
        public Message EnableHeadRotationBasedBlinkAdjust(bool enable) => BoolContent(VmmCommands.EnableHeadRotationBasedBlinkAdjust, enable);
        public Message EnableVoiceBasedMotion(bool enable) => BoolContent(VmmCommands.EnableVoiceBasedMotion, enable);
        //NOTE: falseのほうが普通だよ、という状態にするため、disable云々というやや面倒な言い方になってる事に注意
        public Message DisableFaceTrackingHorizontalFlip(bool disable) => BoolContent(VmmCommands.DisableFaceTrackingHorizontalFlip, disable);

        public Message EnableImageBasedHandTracking(bool enable) => BoolContent(VmmCommands.EnableImageBasedHandTracking, enable);
        public Message ShowEffectDuringHandTracking(bool enable) => BoolContent(VmmCommands.ShowEffectDuringHandTracking, enable);
        //Faceと同じく、disableという言い回しに注意
        public Message DisableHandTrackingHorizontalFlip(bool disable) => BoolContent(VmmCommands.DisableHandTrackingHorizontalFlip, disable);
        public Message EnableSendHandTrackingResult(bool enable) => BoolContent(VmmCommands.EnableSendHandTrackingResult, enable);


        public Message EnableWebCamHighPowerMode(bool enable) => BoolContent(VmmCommands.EnableWebCamHighPowerMode, enable);

        public Message FaceDefaultFun(int percentage) => IntContent(VmmCommands.FaceDefaultFun, percentage);
        public Message FaceNeutralClip(string clipName) => StringContent(VmmCommands.FaceNeutralClip, clipName);
        public Message FaceOffsetClip(string clipName) => StringContent(VmmCommands.FaceOffsetClip, clipName);

        public Message DisableBlendShapeInterpolate(bool enable) => BoolContent(VmmCommands.DisableBlendShapeInterpolate, enable);
        
        public Message UsePerfectSyncWithWebCamera(bool enable) => BoolContent(VmmCommands.UsePerfectSyncWithWebCamera, enable);
        
        public Message EnableWebCameraHighPowerModeBlink(bool enable) => BoolContent(VmmCommands.EnableWebCameraHighPowerModeBlink, enable);
        public Message EnableWebCameraHighPowerModeLipSync(bool enable) => BoolContent(VmmCommands.EnableWebCameraHighPowerModeLipSync, enable);
        public Message EnableWebCameraHighPowerModeMoveZ(bool enable) => BoolContent(VmmCommands.EnableWebCameraHighPowerModeMoveZ, enable);

        public Message SetWebCamEyeOpenBlinkValue(int value) => IntContent(VmmCommands.SetWebCamEyeOpenBlinkValue, value);
        public Message SetWebCamEyeCloseBlinkValue(int value) => IntContent(VmmCommands.SetWebCamEyeCloseBlinkValue, value);

        public Message SetEyeBlendShapePreviewActive(bool active) => BoolContent(VmmCommands.SetEyeBlendShapePreviewActive, active);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message CameraDeviceNames() => None(VmmCommands.CameraDeviceNames);

        //public Message EnableTouchTyping(bool enable) => WithArg(VmmCommands.EnableTouchTyping, enable);

        public Message EnableLipSync(bool enable) => BoolContent(VmmCommands.EnableLipSync, enable);

        public Message SetMicrophoneDeviceName(string deviceName) => StringContent(VmmCommands.SetMicrophoneDeviceName, deviceName);
        public Message SetMicrophoneSensitivity(int sensitivity) => IntContent(VmmCommands.SetMicrophoneSensitivity, sensitivity);
        public Message SetMicrophoneVolumeVisibility(bool isVisible) => BoolContent(VmmCommands.SetMicrophoneVolumeVisibility, isVisible);
        public Message AdjustLipSyncByVolume(bool adjust) => BoolContent(VmmCommands.AdjustLipSyncByVolume, adjust);
        
        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message MicrophoneDeviceNames() => None(VmmCommands.MicrophoneDeviceNames);

        public Message LookAtStyle(string v) => StringContent(VmmCommands.LookAtStyle, v);
        public Message EnableEyeMotionDuringClipApplied(bool enable) => BoolContent(VmmCommands.EnableEyeMotionDuringClipApplied, enable);
        public Message SetUseAvatarEyeBoneMap(bool use) => BoolContent(VmmCommands.SetUseAvatarEyeBoneMap, use);
        public Message SetEyeBoneRotationScale(int percent) => IntContent(VmmCommands.SetEyeBoneRotationScale, percent);
        public Message SetEyeBoneRotationScaleWithMap(int percent) => IntContent(VmmCommands.SetEyeBoneRotationScaleWithMap, percent);

        #endregion

        #region Game Input Locomotion

        public Message UseGamepadForGameInput(bool use) => BoolContent(VmmCommands.UseGamepadForGameInput, use);
        public Message UseKeyboardForGameInput(bool use) => BoolContent(VmmCommands.UseKeyboardForGameInput, use);
        public Message SetGamepadGameInputKeyAssign(string json) => StringContent(VmmCommands.SetGamepadGameInputKeyAssign, json);
        public Message SetKeyboardGameInputKeyAssign(string json) => StringContent(VmmCommands.SetKeyboardGameInputKeyAssign, json);

        public Message SetGameInputLocomotionStyle(int value) => IntContent(VmmCommands.SetGameInputLocomotionStyle, value);
        public Message EnableAlwaysRunGameInput(bool enable) => BoolContent(VmmCommands.EnableAlwaysRunGameInput, enable);

        //NOTE: 以下はKeyboardKeyAssignの一部に帰着させるかもしれない。
        //これらを単独で送信する場合、これらのオプションはKeyAssignのjsonよりも優先されるイメージ
        public Message EnableWasdMoveGameInput(bool enable) => BoolContent(VmmCommands.EnableWasdMoveGameInput, enable);
        public Message EnableArrowKeyMoveGameInput(bool enable) => BoolContent(VmmCommands.EnableArrowKeyMoveGameInput, enable);
        public Message UseShiftRunGameInput(bool use) => BoolContent(VmmCommands.UseShiftRunGameInput, use);
        public Message UseSpaceJumpGameInput(bool use) => BoolContent(VmmCommands.UseSpaceJumpGameInput, use);
        public Message UseMouseMoveForLookAroundGameInput(bool use) => BoolContent(VmmCommands.UseMouseMoveForLookAroundGameInput, use);

        #endregion

        #region カメラの配置

        public Message CameraFov(int cameraFov) => IntContent(VmmCommands.CameraFov, cameraFov);
        public Message SetCustomCameraPosition(string posData) => StringContent(VmmCommands.SetCustomCameraPosition, posData);
        public Message QuickLoadViewPoint(string posData) => StringContent(VmmCommands.QuickLoadViewPoint, posData);

        public Message EnableFreeCameraMode(bool enable) => BoolContent(VmmCommands.EnableFreeCameraMode, enable);

        public Message ResetCameraPosition() => None(VmmCommands.ResetCameraPosition);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message CurrentCameraPosition() => None(VmmCommands.CurrentCameraPosition);

        #endregion

        #region キーボード・マウスパッド

        public Message HidVisibility(bool visible) => BoolContent(VmmCommands.HidVisibility, visible);

        public Message SetPenVisibility(bool visible) => BoolContent(VmmCommands.SetPenVisibility, visible);

        public Message MidiControllerVisibility(bool visible) => BoolContent(VmmCommands.MidiControllerVisibility, visible);

        public Message SetKeyboardTypingEffectType(int typeIndex) => IntContent(VmmCommands.SetKeyboardTypingEffectType, typeIndex);

        // NOTE: Gamepadとかにも全般的に影響する想定のフラグ
        public Message HideUnusedDevices(bool hide) => BoolContent(VmmCommands.HideUnusedDevices, hide);

        public Message EnableDeviceFreeLayout(bool enable) => BoolContent(VmmCommands.EnableDeviceFreeLayout, enable);

        public Message SetDeviceLayout(string data) => StringContent(VmmCommands.SetDeviceLayout, data);

        public Message ResetDeviceLayout() => None(VmmCommands.ResetDeviceLayout);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        //public Message CurrentDeviceLayout() => NoArg(VmmCommands.CurrentDeviceLayout);

        #endregion

        #region MIDI

        public Message EnableMidiRead(bool enable) => BoolContent(VmmCommands.EnableMidiRead, enable);

        #endregion

        #region ゲームパッド

        public Message EnableGamepad(bool enable) => BoolContent(VmmCommands.EnableGamepad, enable);
        public Message PreferDirectInputGamepad(bool preferDirectInput) => BoolContent(VmmCommands.PreferDirectInputGamepad, preferDirectInput);
        //public Message GamepadHeight(int height) => WithArg(VmmCommands.GamepadHeight, height);
        //public Message GamepadHorizontalScale(int scale) => WithArg(VmmCommands.GamepadHorizontalScale, scale);

        public Message GamepadVisibility(bool visibility) => BoolContent(VmmCommands.GamepadVisibility, visibility);

        public Message GamepadLeanMode(string v) => StringContent(VmmCommands.GamepadLeanMode, v);

        public Message GamepadLeanReverseHorizontal(bool reverse) => BoolContent(VmmCommands.GamepadLeanReverseHorizontal, reverse);
        public Message GamepadLeanReverseVertical(bool reverse) => BoolContent(VmmCommands.GamepadLeanReverseVertical, reverse);

        #endregion

        #region Light Setting

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message GetQualitySettingsInfo() => None(VmmCommands.GetQualitySettingsInfo);
        public Message SetImageQuality(string name) => StringContent(VmmCommands.SetImageQuality, name);
        public Message SetAntiAliasStyle(int style) => IntContent(VmmCommands.SetAntiAliasStyle, style);
        public Message SetHalfFpsMode(bool enable) => BoolContent(VmmCommands.SetHalfFpsMode, enable);
        public Message UseFrameReductionEffect(bool enable) => BoolContent(VmmCommands.UseFrameReductionEffect, enable);

        /// <summary>
        /// Query
        /// </summary>
        /// <returns></returns>
        public Message ApplyDefaultImageQuality() => None(VmmCommands.ApplyDefaultImageQuality);

        public Message LightColor(int r, int g, int b) => StringContent(VmmCommands.LightColor, $"{r},{g},{b}");
        public Message LightIntensity(int intensityPercent) => IntContent(VmmCommands.LightIntensity, intensityPercent);
        public Message LightYaw(int angleDeg) => IntContent(VmmCommands.LightYaw, angleDeg);
        public Message LightPitch(int angleDeg) => IntContent(VmmCommands.LightPitch, angleDeg);
        public Message UseDesktopLightAdjust(bool use) => BoolContent(VmmCommands.UseDesktopLightAdjust, use);

        public Message ShadowEnable(bool enable) => BoolContent(VmmCommands.ShadowEnable, enable);
        public Message ShadowIntensity(int intensityPercent) => IntContent(VmmCommands.ShadowIntensity, intensityPercent);
        public Message ShadowYaw(int angleDeg) => IntContent(VmmCommands.ShadowYaw, angleDeg);
        public Message ShadowPitch(int angleDeg) => IntContent(VmmCommands.ShadowPitch, angleDeg);
        public Message ShadowDepthOffset(int depthCentimeter) => IntContent(VmmCommands.ShadowDepthOffset, depthCentimeter);

        public Message BloomColor(int r, int g, int b) => StringContent(VmmCommands.BloomColor, $"{r},{g},{b}");
        public Message BloomIntensity(int intensityPercent) => IntContent(VmmCommands.BloomIntensity, intensityPercent);
        public Message BloomThreshold(int thresholdPercent) => IntContent(VmmCommands.BloomThreshold, thresholdPercent);

        public Message AmbientOcclusionEnable(bool enable) => BoolContent(VmmCommands.AmbientOcclusionEnable, enable);
        public Message AmbientOcclusionIntensity(int intensityPercent) => IntContent(VmmCommands.AmbientOcclusionIntensity, intensityPercent);
        public Message AmbientOcclusionColor(int r, int g, int b) => StringContent(VmmCommands.AmbientOcclusionColor, $"{r},{g},{b}");

        public Message OutlineEffectEnable(bool active) => BoolContent(VmmCommands.OutlineEffectEnable, active);
        public Message OutlineEffectThickness(int thickness) => IntContent(VmmCommands.OutlineEffectThickness, thickness);
        public Message OutlineEffectColor(int r, int g, int b) => StringContent(VmmCommands.OutlineEffectColor, $"{r},{g},{b}");
        public Message OutlineEffectHighQualityMode(bool enable) => BoolContent(VmmCommands.OutlineEffectHighQualityMode, enable);
        
        public Message WindEnable(bool enableWind) => BoolContent(VmmCommands.WindEnable, enableWind);
        public Message WindStrength(int strength) => IntContent(VmmCommands.WindStrength, strength);
        public Message WindInterval(int percentage) => IntContent(VmmCommands.WindInterval, percentage);
        public Message WindYaw(int windYaw) => IntContent(VmmCommands.WindYaw, windYaw);

        #endregion

        #region Word To Motion

        public Message ReloadMotionRequests(string content) => StringContent(VmmCommands.ReloadMotionRequests, content);

        //NOTE: 以下の3つはユーザーが動作チェックに使う
        public Message PlayWordToMotionItem(string word) => StringContent(VmmCommands.PlayWordToMotionItem, word);
        public Message EnableWordToMotionPreview(bool enable) => BoolContent(VmmCommands.EnableWordToMotionPreview, enable);
        public Message SendWordToMotionPreviewInfo(string json) => StringContent(VmmCommands.SendWordToMotionPreviewInfo, json);
        public Message SetDeviceTypeToStartWordToMotion(int deviceType) => IntContent(VmmCommands.SetDeviceTypeToStartWordToMotion, deviceType);

        public Message LoadMidiNoteToMotionMap(string content) => StringContent(VmmCommands.LoadMidiNoteToMotionMap, content);
        public Message RequireMidiNoteOnMessage(bool require) => BoolContent(VmmCommands.RequireMidiNoteOnMessage, require);

        public Message RequestCustomMotionDoctor() => None(VmmCommands.RequestCustomMotionDoctor);

        /// <summary>
        /// Query : 引数をtrueにすると .vrma 形式になってるものだけ返却してくれる
        /// </summary>
        /// <returns></returns>
        public Message GetAvailableCustomMotionClipNames(bool vrmaOnly) => BoolContent(VmmCommands.GetAvailableCustomMotionClipNames, vrmaOnly);

        #endregion

        #region External Tracker

        //共通: 基本操作のオン/オフ + キャリブレーション
        public Message ExTrackerEnable(bool enable) => BoolContent(VmmCommands.ExTrackerEnable, enable);
        public Message ExTrackerEnableLipSync(bool enable) => BoolContent(VmmCommands.ExTrackerEnableLipSync, enable);
        public Message ExTrackerEnablePerfectSync(bool enable) => BoolContent(VmmCommands.ExTrackerEnablePerfectSync, enable);
        //public Message ExTrackerUseVRoidDefaultForPerfectSync(bool enable) => WithArg(VmmCommands.ExTrackerUseVRoidDefaultForPerfectSync, enable);
        public Message ExTrackerCalibrate() => None(VmmCommands.ExTrackerCalibrate);
        //NOTE: このdataについて詳細
        // - Unityが送ってくるのをまるごと保持してたデータを返すだけで、WPF側では中身に関知しない
        // - 想定する内部構成としては、全トラッカータイプのデータが1つの文字列に入ったものを想定してます
        //   (連携先がたかだか5アプリくらいを見込んでるので、これでも手に負えるハズ)
        public Message ExTrackerSetCalibrateData(string data) => StringContent(VmmCommands.ExTrackerSetCalibrateData, data);

        //連携先の切り替え + アプリ固有設定の送信
        public Message ExTrackerSetSource(int sourceType) => IntContent(VmmCommands.ExTrackerSetSource, sourceType);
        public Message ExTrackerSetApplicationValue(ExternalTrackerSettingData data) => StringContent(VmmCommands.ExTrackerSetApplicationValue, data.ToJsonString());

        //共通: 表情スイッチ機能
        //NOTE: 設計を安全にするため、全設定をガッと送る機能しか認めていません。
        public Message ExTrackerSetFaceSwitchSetting(string settingJson) => StringContent(VmmCommands.ExTrackerSetFaceSwitchSetting, settingJson);

        #endregion

        #region Accessory

        public Message SetSingleAccessoryLayout(string json) => StringContent(VmmCommands.SetSingleAccessoryLayout, json);
        public Message SetAccessoryLayout(string json) => StringContent(VmmCommands.SetAccessoryLayout, json);
        public Message RequestResetAllAccessoryLayout() => None(VmmCommands.RequestResetAllAccessoryLayout);
        public Message RequestResetAccessoryLayout(string fileNamesJson) => StringContent(VmmCommands.RequestResetAccessoryLayout, fileNamesJson);
        public Message ReloadAccessoryFiles() => None(VmmCommands.ReloadAccessoryFiles);

        #endregion

        #region VMCP

        public Message EnableVMCP(bool enable) => BoolContent(VmmCommands.EnableVMCP, enable);
        public Message SetVMCPSources(string json) => StringContent(VmmCommands.SetVMCPSources, json);
        public Message SetDisableCameraDuringVMCPActive(bool disable) => BoolContent(VmmCommands.SetDisableCameraDuringVMCPActive, disable);
        public Message SetVMCPNaiveBoneTransfer(bool enable) => BoolContent(VmmCommands.SetVMCPNaiveBoneTransfer, enable);

        public Message EnableVMCPSend(bool enable) => BoolContent(VmmCommands.EnableVMCPSend, enable);
        public Message SetVMCPSendSettings(string json) => StringContent(VmmCommands.SetVMCPSendSettings, json);
        public Message ShowEffectDuringVMCPSendEnabled(bool enable) => BoolContent(VmmCommands.ShowEffectDuringVMCPSendEnabled, enable);

        #endregion

        #region その他

        public Message TakeScreenshot() => None(VmmCommands.TakeScreenshot);
        public Message OpenScreenshotFolder() => None(VmmCommands.OpenScreenshotFolder);

        #endregion

        #region VRoid SDK

        public Message OpenVRoidSdkUi() => None(VmmCommands.OpenVRoidSdkUi);
        public Message RequestLoadVRoidWithId(string modelId) => StringContent(VmmCommands.RequestLoadVRoidWithId, modelId);

        #endregion

        #region Debug

        public Message DebugSendLargeData(string data) => StringContent(VmmCommands.DebugSendLargeData, data);

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
            => StringContent(VmmCommands.CommandArray, CommandArrayBuilder.BuildCommandArrayString(commands));

        #endregion
    }
}