using System;
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
    public class MediaPipeTrackerSettingsRepository
    {
        private readonly IMessageSender _sender;
        
        [Inject]
        public MediaPipeTrackerSettingsRepository(IMessageSender sender)
        {
            _sender = sender;
        }

        // NOTE: この値はメインスレッドでしか読まない想定なのでAtomic無し (つけても害はない)
        public bool IsHandTrackingActive { get; set; }

        public Atomic<bool> IsFaceMirrored { get; } = new(true);
        public Atomic<bool> IsHandMirrored { get; } = new(true);

        // NOTE: 手と表情を同時にトラッキングする場合だけtrueになりうる想定だが、そもそも使わなくなるかも。今のところIPCでは受けていない、
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
