using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.ExternalTracker.iFacialMocap
{
    public class iFacialMocapTroubleChecker : MonoBehaviour
    {
        [SerializeField] private iFacialMocapReceiver receiver = null;
        //NOTE: コンポーネントの置き場所的にInjectするモチベがないんですよコイツは
        [SerializeField] private ExternalTrackerDataSource dataSource = null;
        [SerializeField] private float checkInterval = 5f;

        private IMessageSender _sender = null;
        private FaceControlConfiguration _faceConfig = null;
        private VRMPreviewLanguage _language = null;
        
        [Inject]
        public void Initialize(IMessageSender sender, VRMPreviewLanguage language, FaceControlConfiguration config)
        {
            _sender = sender;
            _language = language;
            _faceConfig = config;
        }
        
        private string _troubleMessage = "";
        private float _count = 0;

        private string LoadLowerOrUpperNotCapturedMessage()
        {
            if (_language.Language == "Japanese")
            {
                return "表情キャプチャのうち下半分(Lower)または上半分(Upper)が無効になっています。iFacialMocapアプリの設定で、これらの表情キャプチャを有効にして下さい。";
            }
            else
            {
                return "Face blendShape capture of lower or upper part seems disabled. Please turn on them on iFacialMocap app.";
            }            
        }

        private void SetTroubleMessage(string message)
        {
            if (_troubleMessage != message)
            {
                _troubleMessage = message;
                _sender.SendCommand(MessageFactory.ExTrackerSetIFacialMocapTroubleMessage(_troubleMessage));
            }
        }

        private void Update()
        {
            _count += Time.deltaTime;
            if (_count < checkInterval)
            {
                return;
            }

            _count = 0;
            
            if (_faceConfig.ControlMode != FaceControlModes.ExternalTracker ||
                !receiver.IsRunning ||
                !dataSource.Connected)
            {
                SetTroubleMessage("");
                return;
            }
            
            CheckIFacialMocapDataValidity();
        }

        private void CheckIFacialMocapDataValidity()
        {
            const float Limit = 0.01f;
            
            //NOTE: iFacialMocapはオプションで切った部分のブレンドシェイプは0にして投げてくる。
            //つまりデータ自体には有効性情報は乗っかってない。
            //そこで、「ここらへんの値がぜんぶ0になってるのは流石におかしくない？」みたいなアプローチでやる

            var src = receiver.FaceTrackSource;

            //NOTE: 順番は「0になりにくいものを手前に持ってくる」みたいな発想で並べる。
            //これにより、通常の(問題ないケースでの)ジャッジを素早く終わらせる
            
            //口はもっとブレンドシェイプあるけど、値の有効判定ならコレで足りそうなのでコレで。
            var jaw = src.Jaw;
            var mouth = src.Mouth;
            bool isLowerOff =
                jaw.Open < Limit &&
                jaw.Left < Limit &&
                jaw.Right < Limit &&
                jaw.Forward < Limit &&
                mouth.Close < Limit &&
                mouth.LeftLowerDown < Limit &&
                mouth.LeftUpperUp < Limit &&
                mouth.LeftSmile < Limit &&
                mouth.LeftFrown < Limit &&
                mouth.RightLowerDown < Limit &&
                mouth.RightUpperUp < Limit &&
                mouth.RightSmile < Limit &&
                mouth.RightFrown < Limit;

            if (isLowerOff)
            {
                SetTroubleMessage(LoadLowerOrUpperNotCapturedMessage());
                return;                
            }

            //目も一部ブレンドシェイプは無視する(こんだけあれば足りそうなので)
            var eye = src.Eye;
            var brow = src.Brow;
            bool isUpperOff =
                eye.LeftBlink < Limit &&
                eye.RightBlink < Limit &&
                brow.LeftDown < Limit &&
                brow.LeftOuterUp < Limit &&
                brow.RightDown < Limit &&
                brow.RightOuterUp < Limit &&
                brow.InnerUp < Limit &&
                eye.LeftLookDown < Limit &&
                eye.LeftLookUp < Limit &&
                eye.LeftLookIn < Limit &&
                eye.LeftLookOut < Limit &&
                eye.RightLookDown < Limit &&
                eye.RightLookUp < Limit &&
                eye.RightLookIn < Limit &&
                eye.RightLookOut < Limit;

            if (isUpperOff)
            {
                SetTroubleMessage(LoadLowerOrUpperNotCapturedMessage());
                return;
            }
            
 
            SetTroubleMessage("");
        }
    }
}
