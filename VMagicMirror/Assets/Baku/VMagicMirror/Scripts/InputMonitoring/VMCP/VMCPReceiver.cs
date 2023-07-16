using System;
using System.Linq;
using UnityEngine;
using uOSC;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public enum VMCPMessageType
    {
        //NOTE: VMCPで想定されててもVMagicMirror的に知らんやつはUnknownになる
        Unknown,
        TrackerPose,
        BlendShapeValue,
        BlendShapeApply,
    }

    public class VMCPReceiver : PresenterBase
    {
        private const string HeadPoseParameterName = "Head";
        private const string LeftHandPoseParameterName = "LeftHand";
        private const string RightHandPoseParameterName = "RightHand";
        
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

            var messageType = message.address switch
            {
                "/VMC/Ext/Tra/Pos" => VMCPMessageType.TrackerPose,
                "/VMC/Ext/Blend/Val" => VMCPMessageType.BlendShapeValue,
                "/VMC/Ext/Blend/Apply" => VMCPMessageType.BlendShapeApply,
                _ => VMCPMessageType.Unknown,
            };

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
            }
            
            if (messageType is VMCPMessageType.BlendShapeValue && settings.ReceiveFacial)
            {
                SetBlendShapeValue(message);
            }
        }

        private void ParseTrackerMessage(uOSC.Message message, bool setHeadPose, bool setHandPose)
        {
            if (!(message.values.Length >= 8 && message.values[0] is string key))
            {
                return;
            }

            switch (key)
            {
                case HeadPoseParameterName:
                    if (!setHeadPose)
                    {
                        return;
                    }
                    break;
                case LeftHandPoseParameterName:
                case RightHandPoseParameterName:
                    if (!setHandPose)
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }
            
            if (!(message.values[1] is float posX &&
                message.values[2] is float posY &&
                message.values[3] is float posZ &&
                message.values[4] is float rotX &&
                message.values[5] is float rotY &&
                message.values[6] is float rotZ &&
                message.values[7] is float rotW))
            {
                return;
            }

            var pos = new Vector3(posX, posY, posZ);
            var rot = new Quaternion(rotX, rotY, rotZ, rotW);

            switch (key)
            {
                case HeadPoseParameterName:
                    _headPose.SetPose(pos, rot);
                    return;
                case LeftHandPoseParameterName:
                    _handPose.SetLeftHandPose(pos, rot);
                    return;
                case RightHandPoseParameterName:
                    _handPose.SetRightHandPose(pos, rot);
                    return;
            }
        }

        private void SetBlendShapeValue(uOSC.Message message)
        {
            if (message.values.Length >= 2 &&
                message.values[0] is string key && 
                message.values[1] is float value)
            {
                _blendShape.SetValue(key, value);
            }
        }
    }
}

