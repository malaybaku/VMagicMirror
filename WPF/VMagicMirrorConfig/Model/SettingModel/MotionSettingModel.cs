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
            var factory = MessageFactory.Instance;
            var setting = MotionSetting.Default;

            //NOTE: 長大になってるのはプロパティの初期化仕様によるもの。半手動でテキスト変換して作ってます

            EnableNoHandTrackMode = new RProperty<bool>(setting.EnableNoHandTrackMode, v => SendMessage(factory.EnableNoHandTrackMode(v)));
            EnableGameInputLocomotionMode = new RProperty<bool>(
                setting.EnableGameInputLocomotionMode, v => SendMessage(factory.EnableGameInputLocomotionMode(v))
                );
            EnableTwistBodyMotion = new RProperty<bool>(setting.EnableTwistBodyMotion, v => SendMessage(factory.EnableTwistBodyMotion(v)));
            EnableCustomHandDownPose = new RProperty<bool>(setting.EnableCustomHandDownPose, v => SendMessage(factory.EnableCustomHandDownPose(v)));
            CustomHandDownPose = new RProperty<string>(setting.CustomHandDownPose, v => SendMessage(factory.SetHandDownModeCustomPose(v)));

            EnableFaceTracking = new RProperty<bool>(setting.EnableFaceTracking, v => SendMessage(factory.EnableFaceTracking(v)));
            AutoBlinkDuringFaceTracking = new RProperty<bool>(setting.AutoBlinkDuringFaceTracking, v => SendMessage(factory.AutoBlinkDuringFaceTracking(v)));
            EnableBodyLeanZ = new RProperty<bool>(setting.EnableBodyLeanZ, v => SendMessage(factory.EnableBodyLeanZ(v)));
            EnableBlinkAdjust = new RProperty<bool>(setting.EnableBlinkAdjust, v =>
            {
                SendMessage(factory.EnableHeadRotationBasedBlinkAdjust(v));
                SendMessage(factory.EnableLipSyncBasedBlinkAdjust(v));
            });
            EnableVoiceBasedMotion = new RProperty<bool>(setting.EnableVoiceBasedMotion, v => SendMessage(factory.EnableVoiceBasedMotion(v)));
            DisableFaceTrackingHorizontalFlip = new RProperty<bool>(setting.DisableFaceTrackingHorizontalFlip, v => SendMessage(factory.DisableFaceTrackingHorizontalFlip(v)));

            EnableWebCamHighPowerMode = new RProperty<bool>(setting.EnableWebCamHighPowerMode, v => SendMessage(factory.EnableWebCamHighPowerMode(v)));

            EnableImageBasedHandTracking = new RProperty<bool>(
                setting.EnableImageBasedHandTracking,
                v => SendMessage(factory.EnableImageBasedHandTracking(v)));
            ShowEffectDuringHandTracking = new RProperty<bool>(
                setting.ShowEffectDuringHandTracking,
                v => SendMessage(factory.ShowEffectDuringHandTracking(v)));
            DisableHandTrackingHorizontalFlip = new RProperty<bool>(
                setting.DisableHandTrackingHorizontalFlip,
                v => SendMessage(factory.DisableHandTrackingHorizontalFlip(v)));
            EnableSendHandTrackingResult = new RProperty<bool>(
                false,
                v => SendMessage(factory.EnableSendHandTrackingResult(v)));

            CameraDeviceName = new RProperty<string>(setting.CameraDeviceName, v => SendMessage(factory.SetCameraDeviceName(v)));
            CalibrateFaceData = new RProperty<string>(setting.CalibrateFaceData, v => SendMessage(factory.SetCalibrateFaceData(v)));

            FaceDefaultFun = new RProperty<int>(setting.FaceDefaultFun, v => SendMessage(factory.FaceDefaultFun(v)));
            FaceNeutralClip = new RProperty<string>(setting.FaceNeutralClip, v => SendMessage(factory.FaceNeutralClip(v)));
            FaceOffsetClip = new RProperty<string>(setting.FaceOffsetClip, v => SendMessage(factory.FaceOffsetClip(v)));

            MoveEyesDuringFaceClipApplied = new RProperty<bool>(
                setting.MoveEyesDuringFaceClipApplied, v => SendMessage(factory.EnableEyeMotionDuringClipApplied(v)));
            DisableBlendShapeInterpolate = new RProperty<bool>(
                setting.DisableBlendShapeInterpolate, v => SendMessage(factory.DisableBlendShapeInterpolate(v)));
            UsePerfectSyncWithWebCamera = new RProperty<bool>(
                setting.UsePerfectSyncWithWebCamera, v => SendMessage(factory.UsePerfectSyncWithWebCamera(v)));

            EnableWebCameraHighPowerModeBlink = new RProperty<bool>(
                setting.EnableWebCameraHighPowerModeBlink, v => SendMessage(factory.EnableWebCameraHighPowerModeBlink(v)));

            EnableWebCameraHighPowerModeLipSync = new RProperty<bool>(
                setting.EnableWebCameraHighPowerModeLipSync, v => SendMessage(factory.EnableWebCameraHighPowerModeLipSync(v)));

            EnableWebCameraHighPowerModeMoveZ = new RProperty<bool>(
                setting.EnableWebCameraHighPowerModeMoveZ, v => SendMessage(factory.EnableWebCameraHighPowerModeMoveZ(v)));

            //TODO: 排他のタイミング次第でRadioButtonが使えなくなってしまうので要検証
            UseLookAtPointNone = new RProperty<bool>(setting.UseLookAtPointNone, v =>
            {
                if (v)
                {
                    SendMessage(factory.LookAtStyle(LookAtStyles.UseLookAtPointNone));
                    UseLookAtPointMousePointer?.Set(false);
                    UseLookAtPointMainCamera?.Set(false);
                }
            });

            UseLookAtPointMousePointer = new RProperty<bool>(setting.UseLookAtPointMousePointer, v =>
            {
                if (v)
                {
                    SendMessage(factory.LookAtStyle(LookAtStyles.UseLookAtPointMousePointer));
                    UseLookAtPointNone.Value = false;
                    UseLookAtPointMainCamera?.Set(false);
                }
            });

            UseLookAtPointMainCamera = new RProperty<bool>(setting.UseLookAtPointMainCamera, v =>
            {
                if (v)
                {
                    SendMessage(factory.LookAtStyle(LookAtStyles.UseLookAtPointMainCamera));
                    UseLookAtPointNone.Value = false;
                    UseLookAtPointMousePointer.Value = false;
                }
            });

            UseAvatarEyeBoneMap = new RProperty<bool>(setting.UseAvatarEyeBoneMap, v => SendMessage(factory.SetUseAvatarEyeBoneMap(v)));
            EyeBoneRotationScale = new RProperty<int>(setting.EyeBoneRotationScale, v => SendMessage(factory.SetEyeBoneRotationScale(v)));
            EyeBoneRotationScaleWithMap = new RProperty<int>(setting.EyeBoneRotationScaleWithMap, v => SendMessage(factory.SetEyeBoneRotationScaleWithMap(v)));

            EnableLipSync = new RProperty<bool>(setting.EnableLipSync, v => SendMessage(factory.EnableLipSync(v)));
            LipSyncMicrophoneDeviceName = new RProperty<string>(setting.LipSyncMicrophoneDeviceName, v => SendMessage(factory.SetMicrophoneDeviceName(v)));
            MicrophoneSensitivity = new RProperty<int>(setting.MicrophoneSensitivity, v => SendMessage(factory.SetMicrophoneSensitivity(v)));
            AdjustLipSyncByVolume = new RProperty<bool>(setting.AdjustLipSyncByVolume, v => SendMessage(factory.AdjustLipSyncByVolume(v)));

            EnableHidRandomTyping = new RProperty<bool>(setting.EnableHidRandomTyping, v => SendMessage(factory.EnableHidRandomTyping(v)));
            EnableShoulderMotionModify = new RProperty<bool>(setting.EnableShoulderMotionModify, v => SendMessage(factory.EnableShoulderMotionModify(v)));
            EnableHandDownTimeout = new RProperty<bool>(setting.EnableHandDownTimeout, v => SendMessage(factory.EnableTypingHandDownTimeout(v)));
            WaistWidth = new RProperty<int>(setting.WaistWidth, v => SendMessage(factory.SetWaistWidth(v)));
            ElbowCloseStrength = new RProperty<int>(setting.ElbowCloseStrength, v => SendMessage(factory.SetElbowCloseStrength(v)));

            EnableFpsAssumedRightHand = new RProperty<bool>(setting.EnableFpsAssumedRightHand, v => SendMessage(factory.EnableFpsAssumedRightHand(v)));

            ShowPresentationPointer = new RProperty<bool>(setting.ShowPresentationPointer);
            PresentationArmRadiusMin = new RProperty<int>(setting.PresentationArmRadiusMin, v => SendMessage(factory.PresentationArmRadiusMin(v)));

            LengthFromWristToTip = new RProperty<int>(setting.LengthFromWristToTip, v => SendMessage(factory.LengthFromWristToTip(v)));
            HandYOffsetBasic = new RProperty<int>(setting.HandYOffsetBasic, v => SendMessage(factory.HandYOffsetBasic(v)));
            HandYOffsetAfterKeyDown = new RProperty<int>(setting.HandYOffsetAfterKeyDown, v => SendMessage(factory.HandYOffsetAfterKeyDown(v)));

            EnableWaitMotion = new RProperty<bool>(setting.EnableWaitMotion, v => SendMessage(factory.EnableWaitMotion(v)));
            WaitMotionScale = new RProperty<int>(setting.WaitMotionScale, v => SendMessage(factory.WaitMotionScale(v)));
            WaitMotionPeriod = new RProperty<int>(setting.WaitMotionPeriod, v => SendMessage(factory.WaitMotionPeriod(v)));

            KeyboardAndMouseMotionMode = new RProperty<int>(
                setting.KeyboardAndMouseMotionMode, v => SendMessage(factory.SetKeyboardAndMouseMotionMode(v))
                );
            GamepadMotionMode = new RProperty<int>(
                setting.GamepadMotionMode, v => SendMessage(factory.SetGamepadMotionMode(v))
                );

            receiver.ReceivedCommand += OnReceiveCommand;
        }


        private void OnReceiveCommand(object? sender, CommandReceivedEventArgs e)
        {
            if (e.Command != ReceiveMessageNames.UpdateCustomHandDownPose)
            {
                return;
            }

            CustomHandDownPose.SilentSet(e.Args);
        }

        public RProperty<int> KeyboardAndMouseMotionMode { get; }
        public RProperty<int> GamepadMotionMode { get; }

        #region Full Body 

        public RProperty<bool> EnableNoHandTrackMode { get; }
        public RProperty<bool> EnableGameInputLocomotionMode { get; }

        public RProperty<bool> EnableTwistBodyMotion { get; }

        public RProperty<bool> EnableCustomHandDownPose { get; }

        public RProperty<string> CustomHandDownPose { get; }

        public void ResetCustomHandDownPose() => SendMessage(MessageFactory.Instance.ResetCustomHandDownPose());

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

        public RProperty<string> CameraDeviceName { get; }

        /// <summary>
        /// NOTE: この値はUIに出す必要はないが、起動時に空でなければ送り、Unityからデータが来たら受け取り、終了時にはセーブする。
        /// </summary>
        public RProperty<string> CalibrateFaceData { get; }

        public RProperty<int> FaceDefaultFun { get; }
        public RProperty<string> FaceNeutralClip { get; }
        public RProperty<string> FaceOffsetClip { get; }

        public RProperty<bool> MoveEyesDuringFaceClipApplied { get; }
        public RProperty<bool> DisableBlendShapeInterpolate { get; }

        public RProperty<bool> UsePerfectSyncWithWebCamera { get; }

        public RProperty<bool> EnableWebCameraHighPowerModeBlink { get; }
        public RProperty<bool> EnableWebCameraHighPowerModeLipSync { get; }
        public RProperty<bool> EnableWebCameraHighPowerModeMoveZ { get; }

        public void RequestCalibrateFace() => SendMessage(MessageFactory.Instance.CalibrateFace());

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
                SendMessage(MessageFactory.Instance.SetCalibrateFaceData(CalibrateFaceData.Value));
            }
        }

        public void ResetWebCameraHighPowerModeSettings()
        {
            var setting = MotionSetting.Default;
            UsePerfectSyncWithWebCamera.Value = setting.UsePerfectSyncWithWebCamera;
            EnableWebCameraHighPowerModeBlink.Value = setting.EnableWebCameraHighPowerModeBlink;
            EnableWebCameraHighPowerModeLipSync.Value = setting.EnableWebCameraHighPowerModeLipSync;
            EnableWebCameraHighPowerModeMoveZ.Value = setting.EnableWebCameraHighPowerModeMoveZ;
        }
    }
}
