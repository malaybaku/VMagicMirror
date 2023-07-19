using System;
using System.Linq;
using UnityEngine;
using uOSC;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPReceiver : PresenterBase
    {
        private const int OscServerCount = 3;
        private readonly IMessageReceiver _messageReceiver;
        private readonly IFactory<uOscServer> _oscServerFactory;
        private readonly VMCPBlendShape _blendShape;
        private readonly VMCPHeadPose _headPose;
        private readonly VMCPHandPose _handPose;

        //NOTE: oscServerはOnDisableで止まるので、アプリ終了時の停止はこっちから呼ばないでよい
        private uOscServer[] _servers;
        private VMCPDataPassSettings[] _dataPassSettings;
        
        private bool _optionEnabled;
        private VMCPSources _sources = VMCPSources.Empty;

        [Inject]
        public VMCPReceiver(
            IMessageReceiver messageReceiver, 
            IFactory<uOscServer> oscServerFactory,
            VMCPBlendShape blendShape,
            VMCPHeadPose headPose,
            VMCPHandPose handPose
            )
        {
            _messageReceiver = messageReceiver;
            _oscServerFactory = oscServerFactory;
            _blendShape = blendShape;
            _headPose = headPose;
            _handPose = handPose;
        }

        public override void Initialize()
        {
            _servers = new uOscServer[OscServerCount];
            _dataPassSettings = new VMCPDataPassSettings[OscServerCount];
            for (var i = 0; i < _servers.Length; i++)
            {
                var sourceIndex = i;
                _servers[i] = _oscServerFactory.Create();
                _servers[i].onDataReceived.AddListener(message => OnOscDataReceived(sourceIndex, message));
            }

            _messageReceiver.AssignCommandHandler(
                VmmCommands.EnableVMCP,
                command => SetReceiverActive(command.ToBoolean())
                );
            
            _messageReceiver.AssignCommandHandler(
                VmmCommands.SetVMCPSources,
                command => RefreshSettings(command.Content)
                );
        }

        private void SetReceiverActive(bool active)
        {
            if (_optionEnabled == active)
            {
                return;
            }

            _optionEnabled = active;
            RefreshInternal();
        }

        private void RefreshSettings(string rawSettings)
        {
            try
            {
                var sources = JsonUtility.FromJson<SerializedVMCPSources>(rawSettings);
                _sources = sources.ToSources();
                RefreshDataPassSettings();
                RefreshInternal();
            }
            catch (Exception e)
            {
                LogOutput.Instance.Write(e);
            }
        }

        private void RefreshDataPassSettings()
        {
            //誰かが使ったデータはもう使えない…という定義の仕方をする
            var receiveHeadPose = false;
            var receiveFacial = false;
            var receiveHandPose = false;
            
            for (var i = 0; i < _dataPassSettings.Length; i++)
            {
                _dataPassSettings[i] = VMCPDataPassSettings.Empty;
                if (_sources.Sources.Count < i)
                {
                    continue;
                }

                var src = _sources.Sources[i];
                if (!src.HasValidSetting())
                {
                    //ポート番号が明らかにおかしい場合、受信設定になってても考慮しない
                    continue;
                }

                _dataPassSettings[i] = new VMCPDataPassSettings(
                    !receiveHeadPose && src.ReceiveHeadPose,
                    !receiveFacial && src.ReceiveFacial,
                    !receiveHandPose && src.ReceiveHandPose
                );

                receiveHeadPose = receiveHeadPose || _dataPassSettings[i].ReceiveHeadPose;
                receiveFacial = receiveFacial || _dataPassSettings[i].ReceiveFacial;
                receiveHandPose = receiveHandPose || _dataPassSettings[i].ReceiveHandPose;
            }
        }

        private void RefreshInternal()
        {
            var serverShouldActive =
                _optionEnabled && _sources.Sources.Any(s => s.HasValidSetting());

            if (!serverShouldActive)
            {
                _blendShape.SetActive(false);
                foreach (var server in _servers)
                {
                    server.StopServer();
                }
                return;
            }

            _headPose.SetActive(_dataPassSettings.Any(s => s.ReceiveHeadPose));
            _handPose.SetActive(_dataPassSettings.Any(s => s.ReceiveHandPose));
            _blendShape.SetActive(_dataPassSettings.Any(s => s.ReceiveFacial));

            // オプションがオンであっても受信しないケースがあるので注意
            for (var i = 0; i < _servers.Length; i++)
            {
                var server = _servers[i];
                if (_sources.Sources.Count < i)
                {
                    server.StopServer();
                    continue;
                }

                var source = _sources.Sources[i];
                if (!source.HasValidSetting())
                {
                    server.StopServer();
                    continue;
                }
                
                server.port = source.Port;
                server.StartServer();
            }
        }

        private void OnOscDataReceived(int sourceIndex, uOSC.Message message)
        {
            var settings = _dataPassSettings[sourceIndex];

            var messageType = message.GetMessageType();
            switch (messageType)
            {
                case VMCPMessageType.TrackerPose:
                    if (settings.ReceiveHeadPose || settings.ReceiveHandPose)
                    {
                        ParseTrackerMessage(message, settings.ReceiveHeadPose, settings.ReceiveHandPose);
                    }
                    break;
                case VMCPMessageType.BlendShapeValue:
                    if (settings.ReceiveFacial)
                    {
                        SetBlendShapeValue(message);
                    }
                    break;
                case VMCPMessageType.BlendShapeApply:
                    if (settings.ReceiveFacial)
                    {
                        _blendShape.Apply();
                    }
                    break;
                default:
                    return;
            }
            
            if (messageType is VMCPMessageType.BlendShapeValue && settings.ReceiveFacial)
            {
                SetBlendShapeValue(message);
            }
        }

        private void ParseTrackerMessage(uOSC.Message message, bool setHeadPose, bool setHandPose)
        {
            var poseType = message.GetTrackerPoseType();
            switch (poseType)
            {
                case VMCPTrackerPoseType.Head:
                    if (!setHeadPose)
                    {
                        return;
                    }
                    break;
                case VMCPTrackerPoseType.LeftHand:
                case VMCPTrackerPoseType.RightHand:
                    if (!setHandPose)
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }

            //NOTE: データが正常であるという前提で楽観的に取る。パフォーマンス重視です
            var (pos, rot) = message.GetPose();
            switch (poseType)
            {
                case VMCPTrackerPoseType.Head:
                    _headPose.SetPose(pos, rot);
                    return;
                case VMCPTrackerPoseType.LeftHand:
                    _handPose.SetLeftHandPose(pos, rot);
                    return;
                case VMCPTrackerPoseType.RightHand:
                    _handPose.SetRightHandPose(pos, rot);
                    return;
            }
        }

        private void SetBlendShapeValue(uOSC.Message message)
        {
            if (message.TryGetBlendShapeValue(out var key, out var value))
            {
                _blendShape.SetValue(key, value);
            }
        }
    }
}

