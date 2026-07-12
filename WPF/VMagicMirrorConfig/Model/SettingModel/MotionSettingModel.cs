using Baku.VMagicMirror;
using Newtonsoft.Json;
using System;

namespace Baku.VMagicMirrorConfig
{
    public enum BodyMotionBaseMode
    {
        Default,
        NoHandTracking, //常に手下げモード (Always Hand Down)のこと
        GameInputLocomotion,
    }

    class MotionSettingModel : SettingModelBase<MotionSetting>
    {
        public MotionSettingModel() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>())
        {
        }


        public MotionSettingModel(IMessageSender sender, IMessageReceiver receiver) : base(sender)
        {
            var setting = MotionSetting.Default;

            //NOTE: 長大になってるのはプロパティの初期化仕様によるもの。半手動でテキスト変換して作ってます

            EnableNoHandTrackMode = new RProperty<bool>(setting.EnableNoHandTrackMode, v => SendMessage(MessageFactory.EnableNoHandTrackMode(v)));
            EnableGameInputLocomotionMode = new RProperty<bool>(
                setting.EnableGameInputLocomotionMode, v => SendMessage(MessageFactory.EnableGameInputLocomotionMode(v))
                );
            EnableTwistBodyMotion = new RProperty<bool>(setting.EnableTwistBodyMotion, v => SendMessage(MessageFactory.EnableTwistBodyMotion(v)));
            SpineAngleOffset = new RProperty<int>(setting.SpineAngleOffset, v => SendMessage(MessageFactory.SetSpineAngleOffset(v)));
            EnableCustomHandDownPose = new RProperty<bool>(setting.EnableCustomHandDownPose, v => SendMessage(MessageFactory.EnableCustomHandDownPose(v)));
            CustomHandDownPose = new RProperty<string>(setting.CustomHandDownPose, v => SendMessage(MessageFactory.SetHandDownModeCustomPose(v)));

            EnableFaceTracking = new RProperty<bool>(setting.EnableFaceTracking, v => SendMessage(MessageFactory.EnableFaceTracking(v)));
            EnableBodyLeanZ = new RProperty<bool>(setting.EnableBodyLeanZ, v => SendMessage(MessageFactory.EnableBodyLeanZ(v)));
            EnableBlinkAdjust = new RProperty<bool>(setting.EnableBlinkAdjust, v =>
            {
                SendMessage(MessageFactory.EnableHeadRotationBasedBlinkAdjust(v));
                SendMessage(MessageFactory.EnableLipSyncBasedBlinkAdjust(v));
            });
            EnableVoiceBasedMotion = new RProperty<bool>(setting.EnableVoiceBasedMotion, v => SendMessage(MessageFactory.EnableVoiceBasedMotion(v)));
            SerializedTrackingLostFaceSwitchSetting = new RProperty<string>(
                setting.SerializedTrackingLostFaceSwitchSetting,
                v => SendMessage(MessageFactory.SetTrackingLostFaceSwitchSetting(v)));
            DisableFaceTrackingHorizontalFlip = new RProperty<bool>(setting.DisableFaceTrackingHorizontalFlip, v => SendMessage(MessageFactory.DisableFaceTrackingHorizontalFlip(v)));

            EnableWebCamHighPowerMode = new RProperty<bool>(setting.EnableWebCamHighPowerMode, v => SendMessage(MessageFactory.EnableWebCamExpressionTracking(v)));
            EnableWebCamApplyBlink = new RProperty<bool>(setting.EnableWebCamApplyBlink, v => SendMessage(MessageFactory.EnableWebCamApplyBlink(v)));
            EnableWebCamQuickMotion = new RProperty<bool>(setting.EnableWebCamQuickMotion, v => SendMessage(MessageFactory.EnableWebCamQuickMotion(v)));
            WebCamMouseLookAtMode = new RProperty<int>(setting.WebCamMouseLookAtMode, v => SendMessage(MessageFactory.SetWebCamMouseLookAtMode(v)));

            EnableImageBasedHandTracking = new RProperty<bool>(
                setting.EnableImageBasedHandTracking,
                v => SendMessage(MessageFactory.EnableImageBasedHandTracking(v)));
            EnableImageBasedElbowTracking = new RProperty<bool>(
                setting.EnableImageBasedElbowTracking,
                v => SendMessage(MessageFactory.EnableImageBasedElbowTracking(v)));
            AlwaysUseSingleMediaPipeTask = new RProperty<bool>(
                setting.AlwaysUseSingleMediaPipeTask,
                v => SendMessage(MessageFactory.EnableAlwaysUseSingleMediaPipeTask(v)));
            ShowEffectDuringHandTracking = new RProperty<bool>(
                setting.ShowEffectDuringHandTracking,
                v => SendMessage(MessageFactory.ShowEffectDuringHandTracking(v)));
            DisableHandTrackingHorizontalFlip = new RProperty<bool>(
                setting.DisableHandTrackingHorizontalFlip,
                v => SendMessage(MessageFactory.DisableHandTrackingHorizontalFlip(v)));
            EnableSendHandTrackingResult = new RProperty<bool>(
                false,
                v => SendMessage(MessageFactory.EnableSendHandTrackingResult(v)));
            HandTrackingMotionScale = new RProperty<int>(setting.HandTrackingMotionScale, v => SendMessage(MessageFactory.SetHandTrackingMotionScale(v)));
            HandTrackingMotionOffsetX = new RProperty<int>(setting.HandTrackingMotionOffsetX, v => SendMessage(MessageFactory.SetHandTrackingMotionOffsetX(v)));
            HandTrackingMotionOffsetY = new RProperty<int>(setting.HandTrackingMotionOffsetY, v => SendMessage(MessageFactory.SetHandTrackingMotionOffsetY(v)));
            HandTrackingHeadPoseAdjustFactor = new RProperty<int>(
                setting.HandTrackingHeadPoseAdjustFactor,
                v => SendMessage(MessageFactory.SetHandTrackingHeadPoseAdjustFactor(v)));

            CameraDeviceName = new RProperty<string>(setting.CameraDeviceName, v => SendMessage(MessageFactory.SetCameraDeviceName(v)));
            CalibrateFaceDataHighPower = new RProperty<string>(setting.CalibrateFaceDataHighPower, v => SendMessage(MessageFactory.SetCalibrateFaceDataHighPower(v)));

            FaceDefaultFun = new RProperty<int>(setting.FaceDefaultFun, v => SendMessage(MessageFactory.FaceDefaultFun(v)));
            FaceNeutralClip = new RProperty<string>(setting.FaceNeutralClip, v => SendMessage(MessageFactory.FaceNeutralClip(v)));
            FaceOffsetClip = new RProperty<string>(setting.FaceOffsetClip, v => SendMessage(MessageFactory.FaceOffsetClip(v)));

            MoveEyesDuringFaceClipApplied = new RProperty<bool>(
                setting.MoveEyesDuringFaceClipApplied, v => SendMessage(MessageFactory.EnableEyeMotionDuringClipApplied(v)));
            DisableBlendShapeInterpolate = new RProperty<bool>(
                setting.DisableBlendShapeInterpolate, v => SendMessage(MessageFactory.DisableBlendShapeInterpolate(v)));
            
            EnableWebCameraHighPowerModeLipSync = new RProperty<bool>(
                setting.EnableWebCameraHighPowerModeLipSync, v => SendMessage(MessageFactory.EnableWebCamApplyLipSync(v)));

            WebCamEyeOpenBlinkValue = new RProperty<int>(
                setting.WebCamEyeOpenBlinkValue, v => SendMessage(MessageFactory.SetWebCamEyeOpenBlinkValue(v)));
            WebCamEyeCloseBlinkValue = new RProperty<int>(
                setting.WebCamEyeCloseBlinkValue, v => SendMessage(MessageFactory.SetWebCamEyeCloseBlinkValue(v)));
            WebCamEyeApplySameBlinkValueBothEye = new RProperty<bool>(
                setting.WebCamEyeApplySameBlinkValueBothEye, v => SendMessage(MessageFactory.SetWebCamEyeApplySameBlinkBothEye(v)));
            WebCamEyeApplyCorrectionToPerfectSync = new RProperty<bool>(
                setting.WebCamEyeApplyCorrectionToPerfectSync, v => SendMessage(MessageFactory.SetWebCamEyeApplyCorrectionToPerfectSync(v)));

            UseAvatarEyeBoneMap = new RProperty<bool>(setting.UseAvatarEyeBoneMap, v => SendMessage(MessageFactory.SetUseAvatarEyeBoneMap(v)));
            EyeBoneRotationScale = new RProperty<int>(setting.EyeBoneRotationScale, v => SendMessage(MessageFactory.SetEyeBoneRotationScale(v)));
            EyeBoneRotationScaleWithMap = new RProperty<int>(setting.EyeBoneRotationScaleWithMap, v => SendMessage(MessageFactory.SetEyeBoneRotationScaleWithMap(v)));

            EnableLipSync = new RProperty<bool>(setting.EnableLipSync, v => SendMessage(MessageFactory.EnableLipSync(v)));
            LipSyncMicrophoneDeviceName = new RProperty<string>(setting.LipSyncMicrophoneDeviceName, v => SendMessage(MessageFactory.SetMicrophoneDeviceName(v)));
            MicrophoneSensitivity = new RProperty<int>(setting.MicrophoneSensitivity, v => SendMessage(MessageFactory.SetMicrophoneSensitivity(v)));
            AdjustLipSyncByVolume = new RProperty<bool>(setting.AdjustLipSyncByVolume, v => SendMessage(MessageFactory.AdjustLipSyncByVolume(v)));

            EnableHidRandomTyping = new RProperty<bool>(setting.EnableHidRandomTyping, v => SendMessage(MessageFactory.EnableHidRandomTyping(v)));
            EnableShoulderMotionModify = new RProperty<bool>(setting.EnableShoulderMotionModify, v => SendMessage(MessageFactory.EnableShoulderMotionModify(v)));
            ShoulderRotationOffset = new RProperty<int>(setting.ShoulderRotationOffset, v => SendMessage(MessageFactory.ShoulderRotationOffset(v)));
            EnableHandDownTimeout = new RProperty<bool>(setting.EnableHandDownTimeout, v => SendMessage(MessageFactory.EnableTypingHandDownTimeout(v)));
            WaistWidth = new RProperty<int>(setting.WaistWidth, v => SendMessage(MessageFactory.SetWaistWidth(v)));
            ElbowCloseStrength = new RProperty<int>(setting.ElbowCloseStrength, v => SendMessage(MessageFactory.SetElbowCloseStrength(v)));

            EnableFpsAssumedRightHand = new RProperty<bool>(setting.EnableFpsAssumedRightHand, v => SendMessage(MessageFactory.EnableFpsAssumedRightHand(v)));

            ShowPresentationPointer = new RProperty<bool>(setting.ShowPresentationPointer);
            PresentationArmRadiusMin = new RProperty<int>(setting.PresentationArmRadiusMin, v => SendMessage(MessageFactory.PresentationArmRadiusMin(v)));

            LengthFromWristToTip = new RProperty<int>(setting.LengthFromWristToTip, v => SendMessage(MessageFactory.LengthFromWristToTip(v)));
            HandYOffsetBasic = new RProperty<int>(setting.HandYOffsetBasic, v => SendMessage(MessageFactory.HandYOffsetBasic(v)));
            HandYOffsetAfterKeyDown = new RProperty<int>(setting.HandYOffsetAfterKeyDown, v => SendMessage(MessageFactory.HandYOffsetAfterKeyDown(v)));

            EnableWaitMotion = new RProperty<bool>(setting.EnableWaitMotion, v => SendMessage(MessageFactory.EnableWaitMotion(v)));
            WaitMotionScale = new RProperty<int>(setting.WaitMotionScale, v => SendMessage(MessageFactory.WaitMotionScale(v)));
            WaitMotionPeriod = new RProperty<int>(setting.WaitMotionPeriod, v => SendMessage(MessageFactory.WaitMotionPeriod(v)));

            KeyboardAndMouseMotionMode = new RProperty<int>(
                setting.KeyboardAndMouseMotionMode, v => SendMessage(MessageFactory.SetKeyboardAndMouseMotionMode(v))
                );
            GamepadMotionMode = new RProperty<int>(
                setting.GamepadMotionMode, v => SendMessage(MessageFactory.SetGamepadMotionMode(v))
                );

            receiver.ReceivedCommand += OnReceiveCommand;
        }


        private void OnReceiveCommand(CommandReceivedData e)
        {
            if (e.Command is VmmServerCommands.UpdateCustomHandDownPose)
            {
                CustomHandDownPose.SilentSet(e.GetStringValue());
            }
        }

        public RProperty<int> KeyboardAndMouseMotionMode { get; }
        public RProperty<int> GamepadMotionMode { get; }

        #region Full Body 

        public RProperty<bool> EnableNoHandTrackMode { get; }
        public RProperty<bool> EnableGameInputLocomotionMode { get; }

        public RProperty<bool> EnableTwistBodyMotion { get; }

        public RProperty<int> SpineAngleOffset { get; }

        public RProperty<bool> EnableCustomHandDownPose { get; }

        public RProperty<string> CustomHandDownPose { get; }

        public void ResetCustomHandDownPose() => SendMessage(MessageFactory.ResetCustomHandDownPose());

        #endregion

        #region Face

        public RProperty<bool> EnableFaceTracking { get; }

        public RProperty<bool> EnableBodyLeanZ { get; }

        public RProperty<bool> EnableBlinkAdjust { get; }

        public RProperty<bool> EnableVoiceBasedMotion { get; }
        public RProperty<string> SerializedTrackingLostFaceSwitchSetting { get; }

        public RProperty<bool> DisableFaceTrackingHorizontalFlip { get; }

        // NOTE: 設定ファイル互換のため名称は据え置きだが、実体は「表情をカメラでトラッキング」するかどうかを表す。
        public RProperty<bool> EnableWebCamHighPowerMode { get; }
        public RProperty<bool> EnableWebCamApplyBlink { get; }
        public RProperty<bool> EnableWebCamQuickMotion { get; }
        public RProperty<int> WebCamMouseLookAtMode { get; }
        public RProperty<bool> EnableImageBasedHandTracking { get; }
        public RProperty<bool> EnableImageBasedElbowTracking { get; }
        public RProperty<bool> AlwaysUseSingleMediaPipeTask { get; }
        public RProperty<bool> ShowEffectDuringHandTracking { get; }
        public RProperty<bool> DisableHandTrackingHorizontalFlip { get; }
        public RProperty<bool> EnableSendHandTrackingResult { get; }

        public RProperty<int> HandTrackingMotionScale { get; }
        public RProperty<int> HandTrackingMotionOffsetX { get; }
        public RProperty<int> HandTrackingMotionOffsetY { get; }
        public RProperty<int> HandTrackingHeadPoseAdjustFactor { get; }


        public RProperty<string> CameraDeviceName { get; }

        // NOTE: この値はUIに出す必要はないが、起動時に空でなければ送り、Unityからデータが来たら受け取り、終了時にはセーブする。
        public RProperty<string> CalibrateFaceDataHighPower { get; }

        public RProperty<int> FaceDefaultFun { get; }
        public RProperty<string> FaceNeutralClip { get; }
        public RProperty<string> FaceOffsetClip { get; }

        public RProperty<bool> MoveEyesDuringFaceClipApplied { get; }
        public RProperty<bool> DisableBlendShapeInterpolate { get; }

        public RProperty<bool> EnableWebCameraHighPowerModeLipSync { get; }
        
        // NOTE: Openのほうが値としては小さい想定(+0付近)
        public RProperty<int> WebCamEyeOpenBlinkValue { get; }
        public RProperty<int> WebCamEyeCloseBlinkValue { get; }
        public RProperty<bool> WebCamEyeApplySameBlinkValueBothEye { get; }
        public RProperty<bool> WebCamEyeApplyCorrectionToPerfectSync { get; }


        public void RequestCalibrateFace() => SendMessage(MessageFactory.CalibrateFace());

        #endregion

        #region Eye

        public RProperty<bool> UseAvatarEyeBoneMap { get; }
        public RProperty<int> EyeBoneRotationScale { get; }
        public RProperty<int> EyeBoneRotationScaleWithMap { get; }

        #endregion

        #region Mouth

        public RProperty<bool> EnableLipSync { get; }

        public RProperty<string> LipSyncMicrophoneDeviceName { get; }

        //NOTE: dB単位なので0がデフォルト。対数ベースのほうがレンジ取りやすい
        public RProperty<int> MicrophoneSensitivity { get; }

        public RProperty<bool> AdjustLipSyncByVolume { get; }

        #endregion

        #region Arm

        public RProperty<bool> EnableHidRandomTyping { get; }
        public RProperty<bool> EnableShoulderMotionModify { get; }
        public RProperty<int> ShoulderRotationOffset { get; }
        public RProperty<bool> EnableHandDownTimeout { get; }

        public RProperty<int> WaistWidth { get; }
        public RProperty<int> ElbowCloseStrength { get; }
        public RProperty<bool> EnableFpsAssumedRightHand { get; }

        public RProperty<bool> ShowPresentationPointer { get; }
        public RProperty<int> PresentationArmRadiusMin { get; }

        public bool PointerVisible =>
            KeyboardAndMouseMotionMode.Value == MotionSetting.KeyboardMouseMotionPresentation &&
            ShowPresentationPointer.Value;

        #endregion

        #region Hand

        /// <summary> Unit: [cm] </summary>
        public RProperty<int> LengthFromWristToTip { get; }
        public RProperty<int> HandYOffsetBasic { get; }
        public RProperty<int> HandYOffsetAfterKeyDown { get; }

        #endregion

        #region Wait

        public RProperty<bool> EnableWaitMotion { get; }
        public RProperty<int> WaitMotionScale { get; }
        public RProperty<int> WaitMotionPeriod { get; }

        #endregion

        #region Reset API

        public void ResetFaceBasicSetting()
        {
            var setting = MotionSetting.Default;
            EnableFaceTracking.Value = setting.EnableFaceTracking;
            CameraDeviceName.Value = setting.CameraDeviceName;
            EnableWebCamApplyBlink.Value = setting.EnableWebCamApplyBlink;
            EnableWebCamQuickMotion.Value = setting.EnableWebCamQuickMotion;
            WebCamMouseLookAtMode.Value = setting.WebCamMouseLookAtMode;

            EnableVoiceBasedMotion.Value = setting.EnableVoiceBasedMotion;
            DisableFaceTrackingHorizontalFlip.Value = setting.DisableFaceTrackingHorizontalFlip;
            EnableImageBasedHandTracking.Value = setting.EnableImageBasedHandTracking;
            EnableImageBasedElbowTracking.Value = setting.EnableImageBasedElbowTracking;
            AlwaysUseSingleMediaPipeTask.Value = setting.AlwaysUseSingleMediaPipeTask;

            EnableLipSync.Value = setting.EnableLipSync;
            LipSyncMicrophoneDeviceName.Value = setting.LipSyncMicrophoneDeviceName;
            MicrophoneSensitivity.Value = setting.MicrophoneSensitivity;
            AdjustLipSyncByVolume.Value = setting.AdjustLipSyncByVolume;
        }

        public void ResetFaceEyeSetting()
        {
            var setting = MotionSetting.Default;
            EnableBlinkAdjust.Value = setting.EnableBlinkAdjust;

            MoveEyesDuringFaceClipApplied.Value = setting.MoveEyesDuringFaceClipApplied;
            UseAvatarEyeBoneMap.Value = setting.UseAvatarEyeBoneMap;
            EyeBoneRotationScale.Value = setting.EyeBoneRotationScale;
            EyeBoneRotationScaleWithMap.Value = setting.EyeBoneRotationScaleWithMap;
        }

        public void ResetFaceBlendShapeSetting()
        {
            var setting = MotionSetting.Default;
            FaceDefaultFun.Value = setting.FaceDefaultFun;
            FaceNeutralClip.Value = setting.FaceNeutralClip;
            FaceOffsetClip.Value = setting.FaceOffsetClip;
            DisableBlendShapeInterpolate.Value = setting.DisableBlendShapeInterpolate;
        }

        public void ResetArmSetting()
        {
            var setting = MotionSetting.Default;

            KeyboardAndMouseMotionMode.Value = setting.KeyboardAndMouseMotionMode;
            GamepadMotionMode.Value = setting.GamepadMotionMode;

            EnableHidRandomTyping.Value = setting.EnableHidRandomTyping;
            EnableHandDownTimeout.Value = setting.EnableHandDownTimeout;
            WaistWidth.Value = setting.WaistWidth;
            ElbowCloseStrength.Value = setting.ElbowCloseStrength;
            EnableFpsAssumedRightHand.Value = setting.EnableFpsAssumedRightHand;
            ShowPresentationPointer.Value = setting.ShowPresentationPointer;
            PresentationArmRadiusMin.Value = setting.PresentationArmRadiusMin;
        }

        public void ResetShoulderAndBackSetting()
        {
            var setting = MotionSetting.Default;
            SpineAngleOffset.Value = setting.SpineAngleOffset;
            EnableShoulderMotionModify.Value = setting.EnableShoulderMotionModify;
            ShoulderRotationOffset.Value = setting.ShoulderRotationOffset;
        }

        public void ResetHandSetting()
        {
            var setting = MotionSetting.Default;
            LengthFromWristToTip.Value = setting.LengthFromWristToTip;
            HandYOffsetBasic.Value = setting.HandYOffsetBasic;
            HandYOffsetAfterKeyDown.Value = setting.HandYOffsetAfterKeyDown;
        }

        public void ResetWaitMotionSetting()
        {
            var setting = MotionSetting.Default;
            EnableWaitMotion.Value = setting.EnableWaitMotion;
            WaitMotionScale.Value = setting.WaitMotionScale;
            WaitMotionPeriod.Value = setting.WaitMotionPeriod;
        }

        public override void ResetToDefault()
        {
            var setting = MotionSetting.Default;
            EnableNoHandTrackMode.Value = setting.EnableNoHandTrackMode;
            EnableGameInputLocomotionMode.Value = setting.EnableGameInputLocomotionMode;
            EnableTwistBodyMotion.Value = setting.EnableTwistBodyMotion;
            EnableCustomHandDownPose.Value = setting.EnableCustomHandDownPose;
            ResetFaceBasicSetting();
            ResetFaceEyeSetting();
            ResetFaceBlendShapeSetting();
            ResetShoulderAndBackSetting();
            ResetArmSetting();
            ResetHandSetting();
            ResetWaitMotionSetting();
            ResetTrackingLostFaceSwitchSetting();
        }

        #endregion

        /// <summary>
        /// AutoAdjustParametersがシリアライズされた文字列を渡すことで、自動調整パラメータのうち
        /// モーションに関係のある値を適用します。
        /// </summary>
        /// <param name="data"></param>
        /// <remarks>
        /// ここで適用した値はUnityに対してメッセージ送信されません
        /// (そもそもUnity側から来る値だから)
        /// </remarks>
        public void SetAutoAdjustResults(string data)
        {
            try
            {
                var parameters = JsonConvert.DeserializeObject<AutoAdjustParameters>(data);
                if (parameters != null)
                {
                    LengthFromWristToTip.SilentSet(parameters.LengthFromWristToTip);
                }
            }
            catch (Exception)
            {
                //何もしない: データ形式が悪いので諦める
            }
        }

        protected override void AfterLoad(MotionSetting entity)
        {
            //ファイルに有効なキャリブレーション情報があれば送る。
            //NOTE: これ以外のタイミングではキャリブレーション情報は基本送らないでよい
            //(Unity側がすでにキャリブの値を知ってる状態でメッセージを投げてくるため)
            if (!string.IsNullOrEmpty(CalibrateFaceDataHighPower.Value))
            {
                SendMessage(MessageFactory.SetCalibrateFaceDataHighPower(CalibrateFaceDataHighPower.Value));
            }
        }

        public void ResetWebCameraHighPowerModeSettings()
        {
            var setting = MotionSetting.Default;
            DisableFaceTrackingHorizontalFlip.Value = setting.DisableFaceTrackingHorizontalFlip;
            EnableWebCameraHighPowerModeLipSync.Value = setting.EnableWebCameraHighPowerModeLipSync;
            EnableWebCamApplyBlink.Value = setting.EnableWebCamApplyBlink;
            EnableWebCamQuickMotion.Value = setting.EnableWebCamQuickMotion;
            WebCamMouseLookAtMode.Value = setting.WebCamMouseLookAtMode;
            EnableBodyLeanZ.Value = setting.EnableBodyLeanZ;
        }

        public void ResetTrackingLostFaceSwitchSetting()
        {
            SerializedTrackingLostFaceSwitchSetting.Value = MotionSetting.Default.SerializedTrackingLostFaceSwitchSetting;
        }
    }
}
