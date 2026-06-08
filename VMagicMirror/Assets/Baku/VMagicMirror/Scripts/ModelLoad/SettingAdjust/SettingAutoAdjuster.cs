using System;
using Baku.VMagicMirror.IpcMessage;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class SettingAutoAdjuster
    {
        //基準長はMegumi Baxterさんの体型。(https://hub.vroid.com/characters/9003440353945198963/models/7418874241157618732)
        //他クラスから使いたくなったら随時publicにしてよい

        //UpperArm to Hand
        public const float ReferenceArmLength = 0.378f;
        //Hand (Wrist) to Middle Distal
        private const float ReferenceHandLength = 0.114f;
        private const float ReferenceChestHeight = 0.89008f;
        private const float ReferenceSpineHeight = 0.78448f;

        public SettingAutoAdjuster(
            IVRMLoadable vrmLoadable,
            IMessageReceiver receiver,
            IMessageSender sender, 
            IMessageDispatcher dispatcher, 
            Camera mainCam
            )
        {
            _mainCam = mainCam.transform;
            
            _sender = sender;
            _dispatcher = dispatcher;
            
            receiver.AssignCommandHandler(
                VmmCommands.RequestAutoAdjust,
                _ => AutoAdjust()
                );

            vrmLoadable.PreVrmLoaded += info =>
            {
                _avatarBones = info.AvatarBones;
                _hasModel = true;
            };
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _avatarBones = null;
            };
        }
        
        private readonly IMessageSender _sender;
        private readonly IMessageDispatcher _dispatcher;
        private readonly Transform _mainCam;

        private bool _hasModel;
        private VRMAvatarBones _avatarBones;

        /// <summary>
        /// VRMがロード済みの状態で呼び出すと、
        /// キーボード、マウス用のレイアウトパラメータと、ゲームパッド用のレイアウトパラメータを計算します。
        /// VRMがロードされていない場合はnullを返します。
        /// </summary>
        /// <returns></returns>
        public DeviceLayoutAutoAdjustParameters GetDeviceLayoutParameters()
        {
            if (!_hasModel)
            {
                return null;
            }
            
            var result = new DeviceLayoutAutoAdjustParameters();

            var chest = _avatarBones.Chest;
            result.HeightFactor = 
                (chest != null) ? 
                    chest.position.y / ReferenceChestHeight :
                    _avatarBones.Spine.position.y / ReferenceSpineHeight;
            
            var upperArm = _avatarBones.RightUpperArm.position;
            var lowerArm = _avatarBones.RightLowerArm.position;
            var wrist = _avatarBones.RightHand.position;
            var armLength = Vector3.Distance(upperArm, lowerArm) + Vector3.Distance(lowerArm, wrist);
            result.ArmLengthFactor = armLength / ReferenceArmLength;

            return result;
        }
    
        private void AutoAdjust()
        {
            if (!_hasModel)
            {
                return;
            }

            var parameters = new AutoAdjustParameters();
            //やること: 
            //1. いま読まれてるモデルの体型からいろんなパラメータを決めてparametersに入れていく
            //2. 決定したパラメータが疑似的にメッセージハンドラから飛んできたことにして適用
            //3. 決定したパラメータをコンフィグ側に送る

            try
            {
                // Adjust対象の部位である程度分けられるので分けておく
                SetHandSizeRelatedParameters(_avatarBones, parameters);
                AdjustCameraPosition(_avatarBones);
                
                //デバイスレイアウト調整: これは別途調整が終わるとメッセージが飛ぶ
                _dispatcher.ReceiveCommand(new ReceivedCommand(
                    MessageSerializer.None((ushort) VmmCommands.ResetDeviceLayout)
                    ));
                
                //3. 決定したパラメータをコンフィグ側に送る
                _sender.SendCommand(MessageFactory.AutoAdjustResults(parameters));
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        private void AdjustCameraPosition(VRMAvatarBones avatarBones)
        {
            var head = avatarBones.Head;
            _mainCam.position = new Vector3(0, head.position.y, 1.3f);
            _mainCam.rotation = Quaternion.Euler(0, 180, 0);
        }
        
        private void SetHandSizeRelatedParameters(VRMAvatarBones avatarBones, AutoAdjustParameters parameters)
        {
            var tip = avatarBones.RightMiddleDistal;
            if (tip == null) { return; }

            var wrist = avatarBones.RightHand;
            var distance = Vector3.Distance(tip.position, wrist.position);

            var factor = distance / ReferenceHandLength;
            parameters.LengthFromWristToTip = (int)(parameters.LengthFromWristToTip * factor);
        }
    }
}

