using System;
using System.Linq;
using UniRx;
using UnityEngine;
using uOSC;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPReceiver : PresenterBase, ITickable
    {
        private const int OscServerCount = 3;
        //この秒数だけ受信してなければステータスとして切断扱いになる
        public const float DisconnectCount = 0.5f;
        
        private readonly IMessageReceiver _messageReceiver;
        private readonly IMessageSender _messageSender;
        private readonly IFactory<uOscServer> _oscServerFactory;
        private readonly AvatarBoneInitialLocalOffsets _boneOffsets;
        private readonly VMCPBlendShape _blendShape;
        private readonly VMCPHeadPose _headPose;
        private readonly VMCPHandPose _handPose;

        //NOTE: oscServerはOnDisableで止まるので、アプリ終了時の停止はこっちから呼ばないでよい
        private readonly uOscServer[] _servers = new uOscServer[OscServerCount];
        private readonly VMCPDataPassSettings[] _dataPassSettings = new VMCPDataPassSettings[OscServerCount];
        private readonly VMCPBasedHumanoid[] _receiverHumanoids = new VMCPBasedHumanoid[OscServerCount];
        
        private bool _optionEnabled;
        private VMCPSources _sources = VMCPSources.Empty;

        private readonly bool[] _connected = new bool[OscServerCount];
        //NOTE: ビットフラグで_connectedの状態を表す値(0~7)を入れる。変化検知のために使い、値そのものは読まない
        private readonly ReactiveProperty<int> _connectedValue = new ReactiveProperty<int>(0);

        private readonly float[] _disconnectCountDown = new float[OscServerCount];
        
        
        [Inject]
        public VMCPReceiver(
            IMessageReceiver messageReceiver, 
            IMessageSender messageSender,
            IFactory<uOscServer> oscServerFactory,
            AvatarBoneInitialLocalOffsets boneOffsets,
            VMCPBlendShape blendShape,
            VMCPHeadPose headPose,
            VMCPHandPose handPose
            )
        {
            _messageReceiver = messageReceiver;
            _messageSender = messageSender;
            _oscServerFactory = oscServerFactory;
            _boneOffsets = boneOffsets;
            _blendShape = blendShape;
            _headPose = headPose;
            _handPose = handPose;
        }

        public override void Initialize()
        {
            for (var i = 0; i < _servers.Length; i++)
            {
                var sourceIndex = i;
                _servers[i] = _oscServerFactory.Create();
                _servers[i].onDataReceived.AddListener(message => OnOscDataReceived(sourceIndex, message));

                _receiverHumanoids[i] = new VMCPBasedHumanoid(_boneOffsets);
            }

            _messageReceiver.AssignCommandHandler(
                VmmCommands.EnableVMCP,
                command => SetReceiverActive(command.ToBoolean())
                );
            
            _messageReceiver.AssignCommandHandler(
                VmmCommands.SetVMCPSources,
                command => RefreshSettings(command.GetStringValue())
                );
            
            //NOTE: 「複数ソースの受信設定していたのがほぼ同時に始まる」というケースに備えてDebounceしておく
            // 最初の1回はアプリケーション起動後のやつなので無視
            _connectedValue
                .Throttle(TimeSpan.FromSeconds(0.5f))
                .Skip(1)
                .Subscribe(_ => NotifyConnectStatus())
                .AddTo(this);

            _connectedValue
                .Subscribe(_ => UpdateTrackerConnectStatus())
                .AddTo(this);
        }

        public void Tick()
        {
            if (!_optionEnabled)
            {
                return;
            }

            for (var i = 0; i < OscServerCount; i++)
            {
                if (_dataPassSettings[i].ReceiveHeadPose)
                {
                    //NOTE: Headに並進項を入れようとするのもアリ
                    var headPose = _receiverHumanoids[i].GetFKHeadPoseFromHips();
                    _headPose.SetPoseOnHips(headPose);
                }

                if (_dataPassSettings[i].ReceiveHandPose)
                {
                    //NOTE: IKがあったらIK優先したいんだけどな～
                    var leftHandPose = _receiverHumanoids[i].GetFKLeftHandPoseFromHips();
                    var rightHandPose = _receiverHumanoids[i].GetFKRightHandPoseFromHips();
                    _handPose.SetLeftHandPoseOnHips(
                        leftHandPose.position,
                        leftHandPose.rotation
                    );
                    _handPose.SetRightHandPoseOnHips(
                        rightHandPose.position,
                        rightHandPose.rotation
                    );
                }

                //一定時間データを受信しなかった受信元は切断扱いになる
                if (_disconnectCountDown[i] > 0f)
                {
                    _disconnectCountDown[i] -= Time.deltaTime;
                    if (_disconnectCountDown[i] <= 0f)
                    {
                        UpdateConnectedStatus(i, false);
                    }
                }
            }
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
            //設定の変更後はいったん未接続扱いにする。オプション無効化時などにステータスを切るのはコレで実現される
            for (var i = 0; i < OscServerCount; i++)
            {
                UpdateConnectedStatus(i, false);
            }
            _connectedValue.Value = 0;

            var serverShouldActive =
                _optionEnabled && _sources.Sources.Any(s => s.HasValidSetting());
            if (!serverShouldActive)
            {
                foreach (var server in _servers)
                {
                    server.StopServer();
                }
                _blendShape.SetActive(false);
                _headPose.SetInactive();
                _handPose.SetActive(false);
                _handPose.SetHumanoid(null);
                return;
            }

            //けっこう積極的にリセットしちゃうが許容したい。ダメそうだったら何か考えてね
            foreach (var humanoid in _receiverHumanoids)
            {
                humanoid.Clear();
            }

            var headPoseRefIndex = Array.FindIndex(_dataPassSettings, s => s.ReceiveHeadPose);
            if (headPoseRefIndex >= 0)
            {
                _headPose.SetActive(_receiverHumanoids[headPoseRefIndex]);
            }
            else
            {
                _headPose.SetInactive();
            }

            _handPose.SetActive(_dataPassSettings.Any(s => s.ReceiveHandPose));
            _blendShape.SetActive(_dataPassSettings.Any(s => s.ReceiveFacial));

            //指のFK用のリファレンスを決めておく
            var handSourceHumanoidIndex = 
                Array.FindIndex(_dataPassSettings, s => s.ReceiveHandPose);
            _handPose.SetHumanoid(handSourceHumanoidIndex >= 0
                ? _receiverHumanoids[handSourceHumanoidIndex]
                : null
            );

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

        private void UpdateConnectedStatus(int index, bool connected)
        {
            _connected[index] = connected;
            _connectedValue.Value = (_connected[2] ? 4 : 0) + (_connected[1] ? 2 : 0) + (_connected[0] ? 1 : 0);
            _disconnectCountDown[index] = connected ? DisconnectCount : 0f;
        }

        private void UpdateTrackerConnectStatus()
        {
            //NOTE:
            // - BlendShapeの受信状態は単に受信が停止したら検知可能なので、こういうケアをしないでよい
            // - 非ActiveなときはHeadPose/HandPose側で勝手に切断扱いになるので、わざわざ叩かないでOK

            if (_headPose.IsActive.Value)
            {
                for (var i = 0; i < _dataPassSettings.Length; i++)
                {
                    var setting = _dataPassSettings[i];
                    if (setting.ReceiveHeadPose)
                    {
                        _headPose.SetConnected(_connected[i]);
                        break;
                    }
                }
            }

            if (_handPose.IsActive.Value)
            {
                for (var i = 0; i < _dataPassSettings.Length; i++)
                {
                    var setting = _dataPassSettings[i];
                    if (setting.ReceiveHandPose)
                    {
                        _handPose.SetConnected(_connected[i]);
                        break;
                    }
                }
            }
        }
        
        private void OnOscDataReceived(int sourceIndex, uOSC.Message message)
        {
            UpdateConnectedStatus(sourceIndex, true);

            var settings = _dataPassSettings[sourceIndex];
            var messageType = message.GetMessageType();
            switch (messageType)
            {
                case VMCPMessageType.TrackerPose:
                    if (settings.ReceiveHeadPose || settings.ReceiveHandPose)
                    {
                        ApplyTrackerPose(message, _receiverHumanoids[sourceIndex]);
                    }
                    break;
                case VMCPMessageType.ForwardKinematics:
                    if (settings.ReceiveHeadPose || settings.ReceiveHandPose)
                    {
                        ApplyBoneLocalPose(message, _receiverHumanoids[sourceIndex]);
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

        private void ApplyTrackerPose(uOSC.Message message, VMCPBasedHumanoid humanoid)
        {
            var poseType = message.GetTrackerPoseType(out _);
            if (poseType == VMCPTrackerPoseType.Unknown)
            {
                return;
            }

            //NOTE: データが正常であるという前提で楽観的に取る。パフォーマンス重視です
            var (pos, rot) = message.GetPose();
            switch (poseType)
            {
                case VMCPTrackerPoseType.Head:
                    humanoid.SetTrackerPose(VMCPBasedHumanoid.HeadBoneName, pos, rot);
                    return;
                case VMCPTrackerPoseType.LeftHand:
                    humanoid.SetTrackerPose(VMCPBasedHumanoid.LeftHandBoneName, pos, rot);
                    return;
                case VMCPTrackerPoseType.RightHand:
                    humanoid.SetTrackerPose(VMCPBasedHumanoid.RightHandBoneName, pos, rot);
                    return;
                case VMCPTrackerPoseType.Hips:
                    humanoid.SetTrackerPose(VMCPBasedHumanoid.HipsBoneName, pos, rot);
                    return;
            }
        }

        private void ApplyBoneLocalPose(uOSC.Message message, VMCPBasedHumanoid humanoid)
        {
            //NOTE: データが正常であるという前提で楽観的に取る。パフォーマンス重視です
            var boneName = message.GetForwardKinematicBoneName();
            var (pos, rot) = message.GetPose();
            humanoid.SetLocalPose(boneName, pos, rot);
        }

        private void SetBlendShapeValue(uOSC.Message message)
        {
            if (message.TryGetBlendShapeValue(out var key, out var value))
            {
                _blendShape.SetValue(key, value);
            }
        }

        private void NotifyConnectStatus()
        {
            var json = new VMCPReceiveStatus(_connected).ToJson();
            _messageSender.SendCommand(MessageFactory.NotifyVmcpReceiveStatus(json));
        }
    }
}

