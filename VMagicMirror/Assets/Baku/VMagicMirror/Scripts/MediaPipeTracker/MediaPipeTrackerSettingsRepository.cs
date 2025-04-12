using System;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // TODO: 受け取りたい設定の例
    // - そもそもどのトラッキングが動作すべきか & WebCamTextureの使用をリクエストすべきか
    // - モーションのスケール
    // - 前回起動時のカメラのキャリブレーション結果
    // (他で使ってるのと同じでいいので) 左右反転のオン・オフ

    // NOTE: このクラスが公開する値はメインスレッド以外(=MediaPipeのタスクが走ってるスレッド)からも読み込まれるので注意
    /// <summary>
    /// MediaPipeTrackerの設定のうち、IPCによって動的に変化するような値を保持するレポジトリ
    /// </summary>
    public class MediaPipeTrackerRuntimeSettingsRepository
    {
        private readonly IMessageSender _sender;
        
        [Inject]
        public MediaPipeTrackerRuntimeSettingsRepository(IMessageSender sender)
        {
            _sender = sender;
        }

        // NOTE: このへん値はTaskより後の適用フェーズで使う == メインスレッドでしか使わない想定なのでAtomic無し (つけても害はない)
        public bool IsHandTrackingActive { get; set; }

        private readonly ReactiveProperty<bool> _shouldUseLipSyncResult = new();
        public IReadOnlyReactiveProperty<bool> ShouldUseLipSyncResult => _shouldUseLipSyncResult;
        public void SetShouldUseLipSyncResult(bool value) => _shouldUseLipSyncResult.Value = value;

        // NOTE: 現状ではパーフェクトシンク中はこのフラグが無視され、常に目にはパーフェクトシンクベースの値が適用される。
        // これは「目を適用しないならパーフェクトシンクする意味がない」と思ってそうしているが、フラグを反映したほうが分かりやすいかも
        public bool ShouldUseEyeResult { get; set; } = true;
        
        private readonly ReactiveProperty<bool> _shouldUsePerfectSyncResult = new();
        public IReadOnlyReactiveProperty<bool> ShouldUsePerfectSyncResult => _shouldUsePerfectSyncResult;
        public void SetShouldUsePerfectSyncResult(bool value) => _shouldUsePerfectSyncResult.Value = value;
        public bool EnableBodyMoveZAxis { get; set; }

        public float EyeOpenBlinkValue { get; set; } = 0.2f;
        public float EyeCloseBlinkValue { get; set; } = 0.5f;
        
        // NOTE: ここから下はMediaPipeのタスクから直接使う == メインスレッド外から使うことがある
        public Atomic<bool> IsFaceMirrored { get; } = new(true);
        public Atomic<bool> IsHandMirrored { get; } = new(true);

        // NOTE: 手と表情を同時にトラッキングする場合だけtrueになりうる想定だが、そもそも使わなくなるかも。今のところIPCでは受けていない
        public Atomic<bool> UseInterlace { get; } = new(false);
        
        private readonly Atomic<bool> _hasCalibrationRequest = new();
        public bool HasCalibrationRequest => _hasCalibrationRequest.Value;
        
        private readonly Atomic<CameraCalibrationData> _cameraCalibrationData 
            = new(CameraCalibrationData.Empty());
        public CameraCalibrationData CurrentCalibrationData => _cameraCalibrationData.Value;

        /// <summary>
        /// ファイル等にセーブされていたキャリブレーション情報を適用します。
        /// </summary>
        /// <param name="json"></param>
        public void ApplyReceivedCalibrationData(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                var data = JsonUtility.FromJson<MediaPipeCalibrationData>(json);
                _cameraCalibrationData.Value = data.ToData();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        public void RaiseCalibrationRequest() => _hasCalibrationRequest.Value = true;
        
        /// <summary>
        /// WPFからのキャリブレーション指示に基づいて実施したキャリブレーションの結果を適用するとともに、
        /// キャリブレーション情報をWPFにも送信して永続化します。
        /// </summary>
        /// <param name="data"></param>
        public void SetCalibrationResult(CameraCalibrationData data)
        {
            _cameraCalibrationData.Value = data;
            _hasCalibrationRequest.Value = false;

            var json = JsonUtility.ToJson(MediaPipeCalibrationData.Create(data));
            _sender.SendCommand(MessageFactory.Instance.SetCalibrationFaceDataHighPower(json));
        }
    }

    // NOTE: この値はWPFはただの文字列として取り扱う
    [Serializable]
    public class MediaPipeCalibrationData
    {
        [SerializeField] private Vector2 faceCenterNormalizedPosition;
        [SerializeField] private bool hasCameraLocalPose;
        [SerializeField] private Vector3 cameraLocalPosition;
        [SerializeField] private Quaternion cameraLocalRotation;
        
        public static MediaPipeCalibrationData Create(CameraCalibrationData source)
        {
            return new MediaPipeCalibrationData()
            {
                faceCenterNormalizedPosition = source.FaceCenterNormalizedPosition,
                hasCameraLocalPose = source.HasCameraLocalPose,
                cameraLocalPosition = source.CameraLocalPose.position,
                cameraLocalRotation = source.CameraLocalPose.rotation,
            };
        }

        public CameraCalibrationData ToData() => new CameraCalibrationData(
            faceCenterNormalizedPosition,
            hasCameraLocalPose, 
            new Pose(cameraLocalPosition, cameraLocalRotation)
        );
    }
}
