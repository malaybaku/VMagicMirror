using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Baku.VMagicMirrorConfig
{
    class MessageFactory
    {
        private static MessageFactory? _instance;
        public static MessageFactory Instance
            => _instance ??= new MessageFactory();
        private MessageFactory() { }

        //メッセージのCommandには呼び出した関数の名前が入る: もともとnameof(Hoge)のように関数名を入れていたが、その必要が無くなった
        private static Message NoArg([CallerMemberName] string command = "")
            => new Message(command);

        private static Message WithArg(string content, [CallerMemberName] string command = "")
            => new Message(command, content);

        private static Message WithArg(bool content, [CallerMemberName] string command = "")
            => WithArg(content.ToString(), command);

        private static Message WithArg(int content, [CallerMemberName] string command = "")
            => WithArg(content.ToString(), command);

        public Message Language(string langName) => WithArg(langName);

        #region HID Input

        public Message KeyDown(string keyName) => WithArg(keyName);
        public Message MouseButton(string info) => WithArg(info);
        public Message MouseMoved(int x, int y) => WithArg($"{x},{y}");

        #endregion

        #region VRM Load

        public Message OpenVrmPreview(string filePath) => WithArg(filePath);
        public Message OpenVrm(string filePath) => WithArg(filePath);
        public Message AccessToVRoidHub() => NoArg();

        public Message CancelLoadVrm() => NoArg();

        public Message RequestAutoAdjust() => NoArg();

        #endregion

        #region ウィンドウ

        public Message Chromakey(int a, int r, int g, int b) => WithArg($"{a},{r},{g},{b}");

        public Message WindowFrameVisibility(bool v) => WithArg(v);
        public Message IgnoreMouse(bool v) => WithArg(v);
        public Message TopMost(bool v) => WithArg(v);
        public Message WindowDraggable(bool v) => WithArg(v);

        /// <summary>
        /// NOTE: 空文字列なら背景画像を外す処理をやる。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Message SetBackgroundImagePath(string path) => WithArg(path);

        public Message MoveWindow(int x, int y) => WithArg($"{x},{y}");
        public Message ResetWindowSize() => NoArg();

        public Message SetWholeWindowTransparencyLevel(int level) => WithArg(level);

        public Message SetAlphaValueOnTransparent(int alpha) => WithArg(alpha);

        #endregion

        #region モーション

        public Message EnableNoHandTrackMode(bool enable) => WithArg(enable);
        public Message EnableTwistBodyMotion(bool enable) => WithArg(enable);

        public Message LengthFromWristToTip(int lengthCentimeter) => WithArg(lengthCentimeter);

        public Message HandYOffsetBasic(int offsetCentimeter) => WithArg(offsetCentimeter);
        public Message HandYOffsetAfterKeyDown(int offsetCentimeter) => WithArg(offsetCentimeter);

        public Message EnableHidRandomTyping(bool enable) => WithArg(enable);
        public Message EnableShoulderMotionModify(bool enable) => WithArg(enable);
        public Message EnableTypingHandDownTimeout(bool enable) => WithArg(enable);
        public Message SetWaistWidth(int waistWidthCentimeter) => WithArg(waistWidthCentimeter);
        public Message SetElbowCloseStrength(int strengthPercent) => WithArg(strengthPercent);

        public Message EnableFpsAssumedRightHand(bool enable) => WithArg(enable);
        public Message PresentationArmRadiusMin(int radiusMinCentimeter) => WithArg(radiusMinCentimeter);

        public Message SetKeyboardAndMouseMotionMode(int modeIndex) => WithArg(modeIndex);
        public Message SetGamepadMotionMode(int modeIndex) => WithArg(modeIndex);

        public Message EnableWaitMotion(bool enable) => WithArg(enable);
        public Message WaitMotionScale(int scalePercent) => WithArg(scalePercent);
        public Message WaitMotionPeriod(int periodSec) => WithArg(periodSec);

        public Message CalibrateFace() => NoArg();
        public Message SetCalibrateFaceData(string data) => WithArg(data);

        public Message EnableFaceTracking(bool enable) => WithArg(enable);
        public Message SetCameraDeviceName(string deviceName) => WithArg(deviceName);
        public Message AutoBlinkDuringFaceTracking(bool enable) => WithArg(enable);
        public Message EnableBodyLeanZ(bool enable) => WithArg(enable);
        public Message EnableLipSyncBasedBlinkAdjust(bool enable) => WithArg(enable);
        public Message EnableHeadRotationBasedBlinkAdjust(bool enable) => WithArg(enable);
        public Message EnableVoiceBasedMotion(bool enable) => WithArg(enable);
        //NOTE: falseのほうが普通だよ、という状態にするため、disable云々というやや面倒な言い方になってる事に注意
        public Message DisableFaceTrackingHorizontalFlip(bool disable) => WithArg(disable);

        public Message EnableImageBasedHandTracking(bool enable) => WithArg(enable);
        public Message ShowEffectDuringHandTracking(bool enable) => WithArg(enable);
        //Faceと同じく、disableという言い回しに注意
        public Message DisableHandTrackingHorizontalFlip(bool disable) => WithArg(disable);
        public Message EnableSendHandTrackingResult(bool enable) => WithArg(enable);


        public Message EnableWebCamHighPowerMode(bool enable) => WithArg(enable);

        public Message FaceDefaultFun(int percentage) => WithArg(percentage);
        public Message FaceNeutralClip(string clipName) => WithArg(clipName);
        public Message FaceOffsetClip(string clipName) => WithArg(clipName);


        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message CameraDeviceNames() => NoArg();

        public Message EnableTouchTyping(bool enable) => WithArg(enable);

        public Message EnableLipSync(bool enable) => WithArg(enable);

        public Message SetMicrophoneDeviceName(string deviceName) => WithArg(deviceName);
        public Message SetMicrophoneSensitivity(int sensitivity) => WithArg(sensitivity);
        public Message SetMicrophoneVolumeVisibility(bool isVisible) => WithArg(isVisible);

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message MicrophoneDeviceNames() => NoArg();

        public Message LookAtStyle(string v) => WithArg(v);
        public Message SetEyeBoneRotationScale(int percent) => WithArg(percent);

        #endregion

        #region カメラの配置

        public Message CameraFov(int cameraFov) => WithArg(cameraFov);
        public Message SetCustomCameraPosition(string posData) => WithArg(posData);
        public Message QuickLoadViewPoint(string posData) => WithArg(posData);

        public Message EnableFreeCameraMode(bool enable) => WithArg(enable);

        public Message ResetCameraPosition() => NoArg();

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message CurrentCameraPosition() => NoArg();

        #endregion

        #region キーボード・マウスパッド

        public Message HidVisibility(bool visible) => WithArg(visible);

        public Message SetPenVisibility(bool visible) => WithArg(visible);

        public Message MidiControllerVisibility(bool visible) => WithArg(visible);

        public Message SetKeyboardTypingEffectType(int typeIndex) => WithArg(typeIndex);

        public Message EnableDeviceFreeLayout(bool enable) => WithArg(enable);

        public Message SetDeviceLayout(string data) => WithArg(data);

        public Message ResetDeviceLayout() => NoArg();

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message CurrentDeviceLayout() => NoArg();

        #endregion

        #region MIDI

        public Message EnableMidiRead(bool enable) => WithArg(enable);

        #endregion

        #region ゲームパッド

        public Message EnableGamepad(bool enable) => WithArg(enable);
        public Message PreferDirectInputGamepad(bool preferDirectInput) => WithArg(preferDirectInput);
        public Message GamepadHeight(int height) => WithArg(height);
        public Message GamepadHorizontalScale(int scale) => WithArg(scale);

        public Message GamepadVisibility(bool visibility) => WithArg(visibility);

        public Message GamepadLeanMode(string v) => WithArg(v);

        public Message GamepadLeanReverseHorizontal(bool reverse) => WithArg(reverse);
        public Message GamepadLeanReverseVertical(bool reverse) => WithArg(reverse);

        #endregion

        #region Light Setting

        /// <summary>
        /// Query.
        /// </summary>
        /// <returns></returns>
        public Message GetQualitySettingsInfo() => NoArg();
        public Message SetImageQuality(string name) => WithArg(name);
        public Message SetHalfFpsMode(bool enable) => WithArg(enable);

        /// <summary>
        /// Query
        /// </summary>
        /// <returns></returns>
        public Message ApplyDefaultImageQuality() => NoArg();

        public Message LightColor(int r, int g, int b) => WithArg($"{r},{g},{b}");
        public Message LightIntensity(int intensityPercent) => WithArg(intensityPercent);
        public Message LightYaw(int angleDeg) => WithArg(angleDeg);
        public Message LightPitch(int angleDeg) => WithArg(angleDeg);
        public Message UseDesktopLightAdjust(bool use) => WithArg(use);

        public Message ShadowEnable(bool enable) => WithArg(enable);
        public Message ShadowIntensity(int intensityPercent) => WithArg(intensityPercent);
        public Message ShadowYaw(int angleDeg) => WithArg(angleDeg);
        public Message ShadowPitch(int angleDeg) => WithArg(angleDeg);
        public Message ShadowDepthOffset(int depthCentimeter) => WithArg(depthCentimeter);

        public Message BloomColor(int r, int g, int b) => WithArg($"{r},{g},{b}");
        public Message BloomIntensity(int intensityPercent) => WithArg(intensityPercent);
        public Message BloomThreshold(int thresholdPercent) => WithArg(thresholdPercent);

        public Message WindEnable(bool enableWind) => WithArg(enableWind);
        public Message WindStrength(int strength) => WithArg(strength);
        public Message WindInterval(int percentage) => WithArg(percentage);
        public Message WindYaw(int windYaw) => WithArg(windYaw);

        #endregion

        #region Word To Motion

        public Message ReloadMotionRequests(string content) => WithArg(content);

        //NOTE: 以下の3つはユーザーが動作チェックに使う
        public Message PlayWordToMotionItem(string word) => WithArg(word);
        public Message EnableWordToMotionPreview(bool enable) => WithArg(enable);
        public Message SendWordToMotionPreviewInfo(string json) => WithArg(json);
        public Message SetDeviceTypeToStartWordToMotion(int deviceType) => WithArg(deviceType);

        public Message LoadMidiNoteToMotionMap(string content) => WithArg(content);
        public Message RequireMidiNoteOnMessage(bool require) => WithArg(require);

        public Message RequestCustomMotionDoctor() => NoArg();

        /// <summary>
        /// Query 
        /// </summary>
        /// <returns></returns>
        public Message GetAvailableCustomMotionClipNames() => NoArg();

        #endregion

        #region External Tracker

        //共通: 基本操作のオン/オフ + キャリブレーション
        public Message ExTrackerEnable(bool enable) => WithArg(enable);
        public Message ExTrackerEnableLipSync(bool enable) => WithArg(enable);
        public Message ExTrackerEnableEmphasizeExpression(bool enable) => WithArg(enable);
        public Message ExTrackerEnablePerfectSync(bool enable) => WithArg(enable);
        public Message ExTrackerUseVRoidDefaultForPerfectSync(bool enable) => WithArg(enable);
        public Message ExTrackerCalibrate() => NoArg();
        //NOTE: このdataについて詳細
        // - Unityが送ってくるのをまるごと保持してたデータを返すだけで、WPF側では中身に関知しない
        // - 想定する内部構成としては、全トラッカータイプのデータが1つの文字列に入ったものを想定してます
        //   (連携先がたかだか5アプリくらいを見込んでるので、これでも手に負えるハズ)
        public Message ExTrackerSetCalibrateData(string data) => WithArg(data);

        //連携先の切り替え + アプリ固有設定の送信
        public Message ExTrackerSetSource(int sourceType) => WithArg(sourceType);
        public Message ExTrackerSetApplicationValue(ExternalTrackerSettingData data) => WithArg(data.ToJsonString());

        //共通: 表情スイッチ機能
        //NOTE: 設計を安全にするため、全設定をガッと送る機能しか認めていません。
        public Message ExTrackerSetFaceSwitchSetting(string settingJson) => WithArg(settingJson);

        #endregion

        #region Accessory

        public Message SetSingleAccessoryLayout(string json) => WithArg(json);
        public Message SetAccessoryLayout(string json) => WithArg(json);
        public Message RequestResetAllAccessoryLayout() => NoArg();
        public Message RequestResetAccessoryLayout(string fileNamesJson) => WithArg(fileNamesJson);
        public Message ReloadAccessoryFiles() => NoArg();

        #endregion

        #region その他

        public Message TakeScreenshot() => NoArg();
        public Message OpenScreenshotFolder() => NoArg();

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
            => WithArg(CommandArrayBuilder.BuildCommandArrayString(commands));

        #endregion

        #region VRoid SDK

        public Message OpenVRoidSdkUi() => NoArg();
        public Message RequestLoadVRoidWithId(string modelId) => WithArg(modelId);

        #endregion

    }
}