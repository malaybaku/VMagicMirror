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
                return "表情キャプチャのうち下半分(Lower)または上半分(Upper)が無効になっています。iFacialMocapアプリを開き、これらの表情キャプチャを有効にして下さい。";
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
                _sender.SendCommand(MessageFactory.Instance.ExTrackerSetIFacialMocapTroubleMessage(_troubleMessage));
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
            //TODO: 実データの特定位置がゼロだらけであるか、あるいはそもそも中身の文字列で判断できると嬉しい
        }
    }
}
