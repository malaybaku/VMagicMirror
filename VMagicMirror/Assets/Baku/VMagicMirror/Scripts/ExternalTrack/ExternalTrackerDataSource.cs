using System;
using System.Linq;
using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker.iFacialMocap;
using Baku.VMagicMirror.ExternalTracker.Waidayo;
using Baku.VMagicMirror.InterProcess;

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
        private const int SourceTypeWaidayo = 2;

        //いちど適用したFaceSwitchは最小でもこの秒数だけ維持するよ、という下限値。チャタリングを防ぐのが狙い。
        private const float FaceSwitchMinimumKeepDuration = 0.5f;

        [Tooltip("顔トラがロスした場合、これにtime.deltaTimeをかけた分のLerpファクターで値を減衰させていく")]
        [SerializeField] private float lossBreakRate = 3.0f;
        
        [SerializeField] private iFacialMocapReceiver iFacialMocapReceiver = null;
        [SerializeField] private WaidayoReceiver waidayoReceiver = null;

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
        private readonly EmptyExternalTrackSourceProvider _emptyProvider = new EmptyExternalTrackSourceProvider();
        private readonly FaceSwitchExtractor _faceSwitchExtractor = new FaceSwitchExtractor();

        private IExternalTrackSourceProvider _currentProvider = null;
        private IExternalTrackSourceProvider CurrentProvider => _currentProvider ?? _emptyProvider;

        private IMessageSender _sender = null;
        private FaceControlConfiguration _config = null;
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            IMessageReceiver receiver, 
            IMessageSender sender,
            FaceControlConfiguration config)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmUnloaded;
            _sender = sender;
            _config = config;
            
            var _ = new ExternalTrackerSettingReceiver(receiver, this);
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _faceSwitchExtractor.AvatarBlendShapeNames = info
                .blendShape
                .BlendShapeAvatar
                .Clips
                .Select(c => c.BlendShapeName)
                .ToArray();
        }

        private void OnVrmUnloaded()
        {
            _faceSwitchExtractor.AvatarBlendShapeNames = new string[0];
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
            
            if (_faceSwitchKeepCount <= 0 && FaceSwitchClipName != _faceSwitchExtractor.ClipName)
            {
                _faceSwitchClipName = _faceSwitchExtractor.ClipName;
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

            _faceSwitchExtractor.Update(CurrentSource);
            _config.FaceSwitchActive = !string.IsNullOrEmpty(FaceSwitchClipName);
            _config.FaceSwitchRequestStopLipSync = _config.FaceSwitchActive && !_faceSwitchExtractor.KeepLipSync;
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
                waidayo = waidayoReceiver.CalibrationData,
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
                waidayoReceiver.CalibrationData = data.waidayo;
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
                sourceType > SourceTypeWaidayo)
            {
                return;
            }
            
            UpdateReceiver(_trackingEnabled, sourceType);
        }

        /// <summary>
        /// FaceSwitchの設定をJSON文字列で受け取って更新します。
        /// </summary>
        /// <param name="json"></param>
        public void SetFaceSwitchSetting(string json)
        {
            try
            {
                _faceSwitchExtractor.Setting = JsonUtility.FromJson<FaceSwitchSettings>(json);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
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
                    case SourceTypeWaidayo:
                        _currentProvider = waidayoReceiver;
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

        public IFaceTrackSource CurrentSource => CurrentProvider.FaceTrackSource;
        public Quaternion HeadRotation => CurrentProvider.HeadRotation;
        public Vector3 HeadPositionOffset => CurrentProvider.HeadPositionOffset;

        private string _faceSwitchClipName = "";
        /// <summary> FaceSwitch機能で指定されたブレンドシェイプがあればその名称を取得し、なければ空文字を取得します。 </summary>
        public string FaceSwitchClipName => Connected ? _faceSwitchClipName : "";
        
        public bool KeepLipSyncForFaceSwitch =>
            !string.IsNullOrEmpty(FaceSwitchClipName) && _faceSwitchExtractor.KeepLipSync;

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
