using System;
using Baku.VMagicMirror.ExternalTracker;
using Baku.VMagicMirror.MediaPipeTracker;
using R3;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    [Serializable]
    public sealed class TrackingLostFaceSwitchSetting
    {
        [SerializeField] private string clipName = "";
        [SerializeField] private string accessoryName = "";
        
        [NonSerialized] private string _filteredClipName = null;
        public string ClipName
        {
            get
            {
                if (_filteredClipName == null)
                {
                    _filteredClipName = BlendShapeCompatUtil.GetVrm10ClipName(clipName);
                }
                return _filteredClipName;
            }
        }
        public string AccessoryName => accessoryName;
        
        public static TrackingLostFaceSwitchSetting Default { get; } = new();

        public static TrackingLostFaceSwitchSetting FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<TrackingLostFaceSwitchSetting>(json);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return Default;
            }
        }
     
        public bool HasClipName => !string.IsNullOrEmpty(ClipName);
        public bool HasValue => !string.IsNullOrEmpty(ClipName) || !string.IsNullOrEmpty(AccessoryName);
    }
    
    /// <summary>
    /// 顔トラッキングを行う設定であり、かつそのトラッキングがロストしているときに適用したいブレンドシェイプ情報を出力するようなクラス
    /// 名前に反するがBlendShapeに加えてAccessoryの表示リクエストも出力する
    /// </summary>
    public class TrackingLostBlendShapeSource : PresenterBase, ITickable
    {
        private const float TrackingLostThreshold = 3.0f;
        
        // トラッキングがこの時間だけ成功し続けると _trackingSucceedAtLeastOnce を true にする。
        // なぜ1Fの判定じゃダメかというと、
        // 「顔が検出できてない状態でiFacialMocapが送ってくるデータ」をトラッキングロスト扱いする判定に時間がかかるから
        private const float TrackingFirstSucceedDuration = 1.0f;
        
        private readonly IMessageReceiver _receiver;
        private readonly FaceControlConfiguration _config;

        private readonly MediaPipeKinematicSetter _mediaPipeKinematicSetter;
        private readonly ExternalTrackerDataSource _externalTrackerDataSource;
        
        private TrackingLostFaceSwitchSetting _trackingLostFaceSwitchSetting;
        private ExpressionKey? _settingBasedKey;
        
        // 「トラッキングロス時にブレンドシェイプを利かす」という処理自体が有効かどうかのフラグはsettingの内容から定まる
        private readonly ReactiveProperty<bool> _isFeatureEnabled = new(false);
        private readonly ReactiveProperty<string> _cameraDeviceName = new("");

        private bool _isActive;
        private bool _trackingSucceedAtLeastOnce;
        private float _trackedTime;
        private float _trackingLostTime;
        
        [Inject]
        public TrackingLostBlendShapeSource(
            IMessageReceiver receiver,
            FaceControlConfiguration config,
            MediaPipeKinematicSetter mediaPipeKinematicSetter,
            ExternalTrackerDataSource externalTrackerDataSource)
        {
            _receiver = receiver;
            _config = config;
            _mediaPipeKinematicSetter = mediaPipeKinematicSetter;
            _externalTrackerDataSource = externalTrackerDataSource;
        }

        public bool HasRequest => _expressionKey.CurrentValue.HasValue;

        private readonly ReactiveProperty<ExpressionKey?> _expressionKey = new(null);
        public ReadOnlyReactiveProperty<ExpressionKey?> ExpressionKey => _expressionKey;

        private readonly ReactiveProperty<string> _accessoryNameRequest = new("");
        public ReadOnlyReactiveProperty<string> AccessoryNameRequest => _accessoryNameRequest;

        public void Accumulate(ExpressionAccumulator accumulator)
        {
            if (_expressionKey.CurrentValue.HasValue)
            {
                accumulator.Accumulate(_expressionKey.CurrentValue.Value, 1f);
            }
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.SetTrackingLostFaceSwitchSetting, v =>
                {
                    _trackingLostFaceSwitchSetting = TrackingLostFaceSwitchSetting.FromJson(v.GetStringValue());
                    _isFeatureEnabled.Value = _trackingLostFaceSwitchSetting.HasValue;
                    
                    _settingBasedKey = _trackingLostFaceSwitchSetting.HasClipName
                        ? ExpressionKeyUtils.CreateKeyByName(_trackingLostFaceSwitchSetting.ClipName)
                        : null;
                });
            _receiver.BindStringProperty(VmmCommands.SetCameraDeviceName, _cameraDeviceName);

            _config.HeadMotionControlMode
                .Subscribe(_ => _trackingSucceedAtLeastOnce = false)
                .AddTo(this);
            
            _isFeatureEnabled.CombineLatest(
                _config.HeadMotionControlMode,
                _cameraDeviceName, 
                    (enabled, mode, cameraName) => (enabled, mode, cameraName)
                )
                .DistinctUntilChanged()
                .Subscribe(value =>
                {
                    // NOTE: VMCProtocolの受信が絡むケースはトラッキングロスの概念が扱いにくいので考えないことにする
                    _isActive = value.enabled && value.mode switch
                    {
                        FaceControlModes.ExternalTracker => true,
                        FaceControlModes.WebCam => !string.IsNullOrEmpty(value.cameraName),
                        _ => false,
                    };
                    
                })
                .AddTo(this);
        }

        void ITickable.Tick()
        {
            if (!_isActive)
            {
                _trackingLostTime = 0f;
                _trackingSucceedAtLeastOnce = false;
                _expressionKey.Value = null;
                _accessoryNameRequest.Value = "";
                return;
            }
            
            var tracked = _config.HeadMotionControlMode.CurrentValue is FaceControlModes.ExternalTracker
                ? _externalTrackerDataSource.Connected 
                : _mediaPipeKinematicSetter.TryGetHeadPose(out _);

            if (tracked && !_trackingSucceedAtLeastOnce)
            {
                _trackedTime += Time.deltaTime;
                if (_trackedTime >= TrackingFirstSucceedDuration)
                {
                    _trackingSucceedAtLeastOnce = true;
                }
            }

            if (!tracked)
            {
                _trackedTime = 0f;
            }
            
            // トラッキングに1回成功するまではトラッキングロストしててもBlendShapeが作動しない
            if (tracked || !_trackingSucceedAtLeastOnce)
            {
                _trackingLostTime = 0f;
                _expressionKey.Value = null;
                _accessoryNameRequest.Value = "";
                return;
            }
            
            _trackingLostTime += Time.deltaTime;
            if (_trackingLostTime >= TrackingLostThreshold)
            {
                _expressionKey.Value = _settingBasedKey;
                _accessoryNameRequest.Value = _trackingLostFaceSwitchSetting.AccessoryName;
            }
        }
    }
}


