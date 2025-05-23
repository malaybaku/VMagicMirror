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
        static class LookAtStyles
        {
            public const string UseLookAtPointNone = nameof(UseLookAtPointNone);
            public const string UseLookAtPointMousePointer = nameof(UseLookAtPointMousePointer);
            public const string UseLookAtPointMainCamera = nameof(UseLookAtPointMainCamera);
        }

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
            EnableCustomHandDownPose = new RProperty<bool>(setting.EnableCustomHandDownPose, v => SendMessage(MessageFactory.EnableCustomHandDownPose(v)));
            CustomHandDownPose = new RProperty<string>(setting.CustomHandDownPose, v => SendMessage(MessageFactory.SetHandDownModeCustomPose(v)));

            EnableFaceTracking = new RProperty<bool>(setting.EnableFaceTracking, v => SendMessage(MessageFactory.EnableFaceTracking(v)));
            AutoBlinkDuringFaceTracking = new RProperty<bool>(setting.AutoBlinkDuringFaceTracking, v => SendMessage(MessageFactory.AutoBlinkDuringFaceTracking(v)));
            EnableBodyLeanZ = new RProperty<bool>(setting.EnableBodyLeanZ, v => SendMessage(MessageFactory.EnableBodyLeanZ(v)));
            EnableBlinkAdjust = new RProperty<bool>(setting.EnableBlinkAdjust, v =>
            {
                SendMessage(MessageFactory.EnableHeadRotationBasedBlinkAdjust(v));
                SendMessage(MessageFactory.EnableLipSyncBasedBlinkAdjust(v));
            });
            EnableVoiceBasedMotion = new RProperty<bool>(setting.EnableVoiceBasedMotion, v => SendMessage(MessageFactory.EnableVoiceBasedMotion(v)));
            DisableFaceTrackingHorizontalFlip = new RProperty<bool>(setting.DisableFaceTrackingHorizontalFlip, v => SendMessage(MessageFactory.DisableFaceTrackingHorizontalFlip(v)));

            EnableWebCamHighPowerMode = new RProperty<bool>(setting.EnableWebCamHighPowerMode, v => SendMessage(MessageFactory.EnableWebCamHighPowerMode(v)));

            EnableImageBasedHandTracking = new RProperty<bool>(
                setting.EnableImageBasedHandTracking,
                v => SendMessage(MessageFactory.EnableImageBasedHandTracking(v)));
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
            HandTrackingMotionOffsetY = new RProperty<int>(setting.HandPositionOffsetY, v => SendMessage(MessageFactory.SetHandTrackingPositionOffsetY(v)));

            CameraDeviceName = new RProperty<string>(setting.CameraDeviceName, v => SendMessage(MessageFactory.SetCameraDeviceName(v)));
            CalibrateFaceData = new RProperty<string>(setting.CalibrateFaceData, v => SendMessage(MessageFactory.SetCalibrateFaceData(v)));
            CalibrateFaceDataHighPower = new RProperty<string>(setting.CalibrateFaceDataHighPower, v => SendMessage(MessageFactory.SetCalibrateFaceDataHighPower(v)));

            FaceDefaultFun = new RProperty<int>(setting.FaceDefaultFun, v => SendMessage(MessageFactory.FaceDefaultFun(v)));
            FaceNeutralClip = new RProperty<string>(setting.FaceNeutralClip, v => SendMessage(MessageFactory.FaceNeutralClip(v)));
            FaceOffsetClip = new RProperty<string>(setting.FaceOffsetClip, v => SendMessage(MessageFactory.FaceOffsetClip(v)));

            MoveEyesDuringFaceClipApplied = new RProperty<bool>(
                setting.MoveEyesDuringFaceClipApplied, v => SendMessage(MessageFactory.EnableEyeMotionDuringClipApplied(v)));
            DisableBlendShapeInterpolate = new RProperty<bool>(
                setting.DisableBlendShapeInterpolate, v => SendMessage(MessageFactory.DisableBlendShapeInterpolate(v)));
            
            EnableWebCameraHighPowerModeLipSync = new RProperty<bool>(
                setting.EnableWebCameraHighPowerModeLipSync, v => SendMessage(MessageFactory.EnableWebCameraHighPowerModeLipSync(v)));

            EnableWebCameraHighPowerModeMoveZ = new RProperty<bool>(
                setting.EnableWebCameraHighPowerModeMoveZ, v => SendMessage(MessageFactory.EnableWebCameraHighPowerModeMoveZ(v)));

            WebCamEyeOpenBlinkValue = new RProperty<int>(
                setting.WebCamEyeOpenBlinkValue, v => SendMessage(MessageFactory.SetWebCamEyeOpenBlinkValue(v)));
            WebCamEyeCloseBlinkValue = new RProperty<int>(
                setting.WebCamEyeCloseBlinkValue, v => SendMessage(MessageFactory.SetWebCamEyeCloseBlinkValue(v)));
            WebCamEyeApplySameBlinkValueBothEye = new RProperty<bool>(
                setting.WebCamEyeApplySameBlinkValueBothEye, v => SendMessage(MessageFactory.SetWebCamEyeApplySameBlinkBothEye(v)));
            WebCamEyeApplyCorrectionToPerfectSync = new RProperty<bool>(
                setting.WebCamEyeApplyCorrectionToPerfectSync, v => SendMessage(MessageFactory.SetWebCamEyeApplyCorrectionToPerfectSync(v)));

            //TODO: 排他のタイミング次第でRadioButtonが使えなくなってしまうので要検証
            UseLookAtPointNone = new RProperty<bool>(setting.UseLookAtPointNone, v =>
            {
                if (v)
                {
                    SendMessage(MessageFactory.LookAtStyle(LookAtStyles.UseLookAtPointNone));
                    UseLookAtPointMousePointer?.Set(false);
                    UseLookAtPointMainCamera?.Set(false);
                }
            });

            UseLookAtPointMousePointer = new RProperty<bool>(setting.UseLookAtPointMousePointer, v =>
            {
                if (v)
                {
                    SendMessage(MessageFactory.LookAtStyle(LookAtStyles.UseLookAtPointMousePointer));
                    UseLookAtPointNone.Value = false;
                    UseLookAtPointMainCamera?.Set(false);
                }
            });

            UseLookAtPointMainCamera = new RProperty<bool>(setting.UseLookAtPointMainCamera, v =>
            {
                if (v)
                {
                    SendMessage(MessageFactory.LookAtStyle(LookAtStyles.UseLookAtPointMainCamera));
                    UseLookAtPointNone.Value = false;
                    UseLookAtPointMousePointer.Value = false;
                }
            });

            UseAvatarEyeBoneMap = new RProperty<bool>(setting.UseAvatarEyeBoneMap, v => SendMessage(MessageFactory.SetUseAvatarEyeBoneMap(v)));
            EyeBoneRotationScale = new RProperty<int>(setting.EyeBoneRotationScale, v => SendMessage(MessageFactory.SetEyeBoneRotationScale(v)));
            EyeBoneRotationScaleWithMap = new RProperty<int>(setting.EyeBoneRotationScaleWithMap, v => SendMessage(MessageFactory.SetEyeBoneRotationScaleWithMap(v)));

            EnableLipSync = new RProperty<bool>(setting.EnableLipSync, v => SendMessage(MessageFactory.EnableLipSync(v)));
            LipSyncMicrophoneDeviceName = new RProperty<string>(setting.LipSyncMicrophoneDeviceName, v => SendMessage(MessageFactory.SetMicrophoneDeviceName(v)));
            MicrophoneSensitivity = new RProperty<int>(setting.MicrophoneSensitivity, v => SendMessage(MessageFactory.SetMicrophoneSensitivity(v)));
            AdjustLipSyncByVolume = new RProperty<bool>(setting.AdjustLipSyncByVolume, v => SendMessage(MessageFactory.AdjustLipSyncByVolume(v)));

            EnableHidRandomTyping = new RProperty<bool>(setting.EnableHidRandomTyping, v => SendMessage(MessageFactory.EnableHidRandomTyping(v)));
            EnableShoulderMotionModify = new RProperty<bool>(setting.EnableShoulderMotionModify, v => SendMessage(MessageFactory.EnableShoulderMotionModify(v)));
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

        public RProperty<bool> EnableCustomHandDownPose { get; }

        public RProperty<string> CustomHandDownPose { get; }

        public void ResetCustomHandDownPose() => SendMessage(MessageFactory.ResetCustomHandDownPose());

        #endregion

        #region Face

        public RProperty<bool> EnableFaceTracking { get; }

        public RProperty<bool> AutoBlinkDuringFaceTracking { get; }

        public RProperty<bool> EnableBodyLeanZ { get; }

        public RProperty<bool> EnableBlinkAdjust { get; }

        public RProperty<bool> EnableVoiceBasedMotion { get; }

        public RProperty<bool> DisableFaceTrackingHorizontalFlip { get; }

        public RProperty<bool> EnableWebCamHighPowerMode { get; }
        public RProperty<bool> EnableImageBasedHandTracking { get; }
        public RProperty<bool> ShowEffectDuringHandTracking { get; }
        public RProperty<bool> DisableHandTrackingHorizontalFlip { get; }
        public RProperty<bool> EnableSendHandTrackingResult { get; }

        public RProperty<int> HandTrackingMotionScale { get; }
        public RProperty<int> HandTrackingMotionOffsetY { get; }


        public RProperty<string> CameraDeviceName { get; }

        /// <summary>
        /// NOTE: この値はUIに出す必要はないが、起動時に空でなければ送り、Unityからデータが来たら受け取り、終了時にはセーブする。
        /// </summary>
        public RProperty<string> CalibrateFaceData { get; }
        public RProperty<string> CalibrateFaceDataHighPower { get; }

        public RProperty<int> FaceDefaultFun { get; }
        public RProperty<string> FaceNeutralClip { get; }
        public RProperty<string> FaceOffsetClip { get; }

        public RProperty<bool> MoveEyesDuringFaceClipApplied { get; }
        public RProperty<bool> DisableBlendShapeInterpolate { get; }

        public RProperty<bool> EnableWebCameraHighPowerModeLipSync { get; }
        public RProperty<bool> EnableWebCameraHighPowerModeMoveZ { get; }
        
        // NOTE: Openのほうが値としては小さい想定(+0付近)
        public RProperty<int> WebCamEyeOpenBlinkValue { get; }
        public RProperty<int> WebCamEyeCloseBlinkValue { get; }
        public RProperty<bool> WebCamEyeApplySameBlinkValueBothEye { get; }
        public RProperty<bool> WebCamEyeApplyCorrectionToPerfectSync { get; }


        public void RequestCalibrateFace() => SendMessage(MessageFactory.CalibrateFace());

        #endregion

        #region Eye

        public RProperty<bool> UseLookAtPointNone { get; }
        public RProperty<bool> UseLookAtPointMousePointer { get; }
        public RProperty<bool> UseLookAtPointMainCamera { get; }

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
            AutoBlinkDuringFaceTracking.Value = setting.AutoBlinkDuringFaceTracking;
            EnableBodyLeanZ.Value = setting.EnableBodyLeanZ;

            EnableVoiceBasedMotion.Value = setting.EnableVoiceBasedMotion;
            DisableFaceTrackingHorizontalFlip.Value = setting.DisableFaceTrackingHorizontalFlip;
            EnableImageBasedHandTracking.Value = setting.EnableImageBasedHandTracking;

            EnableLipSync.Value = setting.EnableLipSync;
            LipSyncMicrophoneDeviceName.Value = setting.LipSyncMicrophoneDeviceName;
            MicrophoneSensitivity.Value = setting.MicrophoneSensitivity;
            AdjustLipSyncByVolume.Value = setting.AdjustLipSyncByVolume;
        }

        public void ResetFaceEyeSetting()
        {
            var setting = MotionSetting.Default;
            EnableBlinkAdjust.Value = setting.EnableBlinkAdjust;
            UseLookAtPointNone.Value = setting.UseLookAtPointNone;
            UseLookAtPointMousePointer.Value = setting.UseLookAtPointMousePointer;
            UseLookAtPointMainCamera.Value = setting.UseLookAtPointMainCamera;

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
            EnableShoulderMotionModify.Value = setting.EnableShoulderMotionModify;
            EnableHandDownTimeout.Value = setting.EnableHandDownTimeout;
            WaistWidth.Value = setting.WaistWidth;
            ElbowCloseStrength.Value = setting.ElbowCloseStrength;
            EnableFpsAssumedRightHand.Value = setting.EnableFpsAssumedRightHand;
            ShowPresentationPointer.Value = setting.ShowPresentationPointer;
            PresentationArmRadiusMin.Value = setting.PresentationArmRadiusMin;
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
            ResetArmSetting();
            ResetHandSetting();
            ResetWaitMotionSetting();
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
            if (!string.IsNullOrEmpty(CalibrateFaceData.Value))
            {
                SendMessage(MessageFactory.SetCalibrateFaceData(CalibrateFaceData.Value));
            }

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
            EnableWebCameraHighPowerModeMoveZ.Value = setting.EnableWebCameraHighPowerModeMoveZ;
        }
    }
}
