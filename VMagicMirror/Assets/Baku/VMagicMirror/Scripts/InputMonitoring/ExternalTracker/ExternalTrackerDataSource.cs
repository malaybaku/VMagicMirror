using System;
using System.Linq;
using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker.iFacialMocap;
using UniRx;
using UniVRM10;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// 外部トラッキングのデータ受信を統括してるとても偉いクラス。執行役員くらい偉い。
    /// </summary>
    /// <remarks>
    /// アバターのモーションや顔をいじるクラスは、コイツにアクセスすると必要な情報が出てきます。
    /// ExternalTracker関連のメッセージも一次的にコイツが受けます。
    /// </remarks>
    public class ExternalTrackerDataSource : MonoBehaviour
    {
        private const int SourceTypeNone = 0;
        private const int SourceTypeIFacialMocap = 1;

        //いちど適用したFaceSwitchは最小でもこの秒数だけ維持するよ、という下限値。チャタリングを防ぐのが狙い。
        private const float FaceSwitchMinimumKeepDuration = 0.5f;

        [Tooltip("顔トラがロスした場合、これにtime.deltaTimeをかけた分のLerpファクターで値を減衰させていく")]
        [SerializeField] private float lossBreakRate = 3.0f;
        
        [SerializeField] private iFacialMocapReceiver iFacialMocapReceiver = null;

        [Tooltip("この秒数だけトラッキングの更新イベントが来なかった場合は受動的にロスト扱いする")]
        [SerializeField] private float notTrackCountLimit = 0.5f;

        [Tooltip("トラッキングロス後に再度トラッキングが開始したとき、この秒数をかけてリカバー用のブレンディングを行う")]
        [SerializeField] private float trackRecoverDuration = 1.0f;
        
        
        //顔トラッキングが更新されなかった秒数
        private float _notTrackCount = 0f;
        //顔トラッキングがされた秒数
        private float _trackedCount = 0f;
        
        private float _faceSwitchKeepCount = 0f;
        
        //ソースが「なし」のときに便宜的に割り当てるための、常に顔が中央にあり、無表情であるとみなせるような顔トラッキングデータ
        private readonly EmptyExternalTrackSourceProvider _emptyProvider = new();
        private readonly FaceSwitchExtractor _faceSwitchExtractor = new();
        public FaceSwitchExtractor FaceSwitchExtractor => _faceSwitchExtractor;

        private IExternalTrackSourceProvider _currentProvider = null;
        private IExternalTrackSourceProvider CurrentProvider => _currentProvider ?? _emptyProvider;

        private IMessageSender _sender = null;
        private FaceControlConfiguration _config = null;
        private HorizontalFlipController _horizontalFlipController;
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            IMessageReceiver receiver, 
            IMessageSender sender,
            FaceControlConfiguration config,
            HorizontalFlipController horizontalFlipController)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmUnloaded;
            _horizontalFlipController = horizontalFlipController;
            _sender = sender;
            _config = config;
            
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnable,
                c => EnableTracking(c.ToBoolean())
            );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerCalibrate,
                _ => Calibrate()
            );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerSetCalibrateData,
                c => SetCalibrationData(c.Content)
            );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerSetSource,
                c => SetSourceType(c.ToInt())
            );
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _faceSwitchExtractor.AvatarBlendShapeNames = info
                .instance.Vrm.Expression.LoadExpressionMap()
                .Keys.Select(k =>
                {
                    var result = k.Name;
                    if (k.Preset != ExpressionPreset.custom)
                    {
                        result = char.ToUpper(result[0]) + result.Substring(1);
                    }
                    return result;
                })
                .ToArray();
        }

        private void OnVrmUnloaded()
        {
            _faceSwitchExtractor.AvatarBlendShapeNames = Array.Empty<string>();
        }
        
        private void OnFaceTrackUpdated(IFaceTrackSource source)
        {
            _notTrackCount = 0;
        }

        private void Update()
        {
            if (_notTrackCount < notTrackCountLimit)
            {
                _notTrackCount += Time.deltaTime;
            }

            if (_faceSwitchKeepCount > 0)
            {
                _faceSwitchKeepCount -= Time.deltaTime;
            }
            
            if (_faceSwitchKeepCount <= 0 && !ActiveFaceSwitchItem.Value.Equals(_faceSwitchExtractor.ActiveItem))
            {
                _activeFaceSwitchItem.Value = _faceSwitchExtractor.ActiveItem;
                _faceSwitchKeepCount = FaceSwitchMinimumKeepDuration;
            }

            //空トラッカーを仮想的に更新するやつ
            if (CurrentProvider == _emptyProvider)
            {
                _emptyProvider.RaiseFaceTrackUpdated();
            }

            //接続が継続しているほど、生センサーの値を信じるようになる
            _trackedCount = Connected 
                ? Mathf.Clamp(_trackedCount + Time.deltaTime, 0, trackRecoverDuration)
                : 0;
            CurrentProvider.UpdateApplyRate = _trackedCount / trackRecoverDuration;
            
            //データが来なくなったら基準位置まで戻しておく
            if (!Connected)
            {
                CurrentProvider.BreakToBasePosition(1 - lossBreakRate * Time.deltaTime);
            }

            //FaceSwitchExtractorの処理がなにげに重いので、外部トラッキング非使用時に通らないようガードしてます
            if (CurrentProvider != _emptyProvider)
            {
                _faceSwitchExtractor.Update(CurrentSource);
                _config.FaceSwitchActive = !string.IsNullOrEmpty(FaceSwitchClipName);
                _config.FaceSwitchRequestStopLipSync = _config.FaceSwitchActive && !_activeFaceSwitchItem.Value.KeepLipSync;
            }
        }

        #region IPCで受け取る処理

        private bool _trackingEnabled = false;        
        //NOTE: このクラス以外はデータソースの種類に関知しない(しないほうがよい)ことに注意
        private int _currentSourceType = SourceTypeNone;
        
        /// <summary> トラッキングの有効/無効を切り替えます。 </summary>
        public void EnableTracking(bool enable)
        {
            if (_trackingEnabled == enable)
            {
                return;
            }
            
            UpdateReceiver(enable, _currentSourceType);
        }
        
        public void Calibrate()
        {
            //トラッキング前にキャリブすると訳わからないので禁止！
            if (!Connected)
            {
                return;
            }
            
            //NOTE: 現行実装ではcalibrationが即座に終わる + 返却値は全キャリブデータを一括りにした値なので、こんな感じになります
            CurrentProvider.Calibrate();
            var data = new ExternalTrackerCalibrationData()
            {
                iFacialMocap = iFacialMocapReceiver.CalibrationData,
            };
            _sender.SendCommand(MessageFactory.Instance.ExTrackerCalibrateComplete(
                JsonUtility.ToJson(data)
                ));   

            //POINT: キャリブレーションした直後は値がジャンプしがちになるので、
            //対策としてトラッキングロスからの復帰直後と同じように値をブレンドしていく
            _trackedCount = 0f;
        }

        public void SetCalibrationData(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<ExternalTrackerCalibrationData>(json);
                iFacialMocapReceiver.CalibrationData = data.iFacialMocap;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        public void SetSourceType(int sourceType)
        {
            if (sourceType == _currentSourceType || 
                sourceType < 0 ||
                sourceType > SourceTypeIFacialMocap)
            {
                return;
            }
            
            UpdateReceiver(_trackingEnabled, sourceType);
        }

        private void UpdateReceiver(bool enable, int sourceType)
        {
            //NOTE: ここのスイッチングではUDPの受信開始/停止が走るので、切り替えが最小限になるよう若干ガチガチに書いてます
            if (_trackingEnabled == enable && _currentSourceType == sourceType)
            {
                //何も変わらない
                return;
            }
            
            //NOTE: コード上の制御により、必ずenableかsourceTypeだけが更新値になる(一気に両方書き換えることはない)
            if (_currentSourceType == sourceType)
            {
                _trackingEnabled = enable;
                if (enable)
                {
                    CurrentProvider.StartReceive();
                }
                else
                {
                    CurrentProvider.StopReceive();
                }
            }
            else
            {
                if (enable)
                {
                    CurrentProvider.StopReceive();
                }

                CurrentProvider.FaceTrackUpdated -= OnFaceTrackUpdated;

                _currentSourceType = sourceType;
                switch (_currentSourceType)
                {
                    case SourceTypeIFacialMocap:
                        _currentProvider = iFacialMocapReceiver;
                        _notTrackCount = notTrackCountLimit;
                        break;
                    case SourceTypeNone:
                    default:
                        //無効なときだけトラッキング即OK扱いになります
                        _currentProvider = _emptyProvider;
                        _notTrackCount = 0;
                        break;
                }
                CurrentProvider.FaceTrackUpdated += OnFaceTrackUpdated;
                
                if (enable)
                {
                    CurrentProvider.StartReceive();
                }
            }
        }
        
        #endregion
        
        /// <summary> 外部トラッキングに接続できているかどうかを取得します。 </summary>
        public bool Connected => _notTrackCount < notTrackCountLimit;


        #region 連携先の機能サポートチェック

        /// <summary> 現在、頭部の並進移動トラッキングがサポートされているかどうかを取得します。</summary>
        public bool SupportFacePositionOffset => CurrentProvider.SupportFacePositionOffset;

        /// <summary>現在、ハンドトラッキングをサポートするかどうかを取得します。</summary>
        public bool SupportHandTracking => CurrentProvider.SupportHandTracking;

        #endregion
        
        #region トラッキングデータの内訳

        public bool DisableHorizontalFlip => _horizontalFlipController?.DisableFaceHorizontalFlip.Value ?? false;
        
        /// <summary>
        /// 現在の顔トラッキング情報を取得します。
        /// このデータの内容は<see cref="DisableHorizontalFlip"/>を考慮していないことに注意して下さい。
        /// </summary>
        public IFaceTrackSource CurrentSource => CurrentProvider.FaceTrackSource;

        /// <summary>
        /// 頭部の回転を取得します。この回転は<see cref="DisableHorizontalFlip"/>の値を考慮したデータになります。
        /// </summary>
        public Quaternion HeadRotation
        {
            get
            {
                if (!DisableHorizontalFlip)
                {
                    return CurrentProvider.HeadRotation;
                }

                var result = CurrentProvider.HeadRotation;
                //TODO: これでいいんだっけココ
                result.y = -result.y;
                result.z = -result.z;
                return result;
            }
        }

        /// <summary>
        /// 頭部位置のオフセットを取得します。
        /// この値は<see cref="DisableHorizontalFlip"/>を考慮した値になります。
        /// </summary>
        public Vector3 HeadPositionOffset
        {
            get
            {
                if (!DisableHorizontalFlip)
                {
                    return CurrentProvider.HeadPositionOffset;
                }
                else
                {
                    var result = CurrentProvider.HeadPositionOffset;
                    result.x = -result.x;
                    return result;
                }
            }
        }

        private readonly ReactiveProperty<ActiveFaceSwitchItem> _activeFaceSwitchItem = new();
        public IReadOnlyReactiveProperty<ActiveFaceSwitchItem> ActiveFaceSwitchItem => _activeFaceSwitchItem;

        /// <summary> FaceSwitch機能で指定されたブレンドシェイプがあればその名称を取得し、なければ空文字を取得します。 </summary>
        public string FaceSwitchClipName => Connected ? _activeFaceSwitchItem.Value.ClipName : "";
        
        public bool KeepLipSyncForFaceSwitch => 
            !string.IsNullOrEmpty(_activeFaceSwitchItem.Value.ClipName) && _activeFaceSwitchItem.Value.KeepLipSync;

        #endregion
    }

    /// <summary> 常に「ニュートラルの顔が中央正面向き」扱いであるように取り扱える、空のトラッキング実装 </summary>
    public class EmptyExternalTrackSourceProvider : IExternalTrackSourceProvider
    {
        public void StartReceive()
        {
        }
        public void StopReceive()
        {
        }

        public void BreakToBasePosition(float breakRate)
        {
        }

        private readonly RecordFaceTrackSource _record = new RecordFaceTrackSource();
        public IFaceTrackSource FaceTrackSource => _record;
        
        public bool SupportHandTracking => false;
        public bool SupportFacePositionOffset => false;

        public Quaternion HeadRotation => Quaternion.identity;
        public Vector3 HeadPositionOffset => Vector3.zero;
        
        public event Action<IFaceTrackSource> FaceTrackUpdated;

        public void Calibrate()
        {
        }
        public string CalibrationData { get; set; }

        public float UpdateApplyRate { get; set; }

        /// <summary>
        /// イベントを強制発火します。Updateなどで呼び出すことで、常時データがアップデートされているように取り扱う事が出来ます。
        /// </summary>
        public void RaiseFaceTrackUpdated() => FaceTrackUpdated?.Invoke(_record);
    }
}
