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

            vrmLoadable.PreVrmLoaded += info => _vrmRoot = info.vrmRoot;
            vrmLoadable.VrmDisposing += () => _vrmRoot = null;
        }
        
        private readonly IMessageSender _sender;
        private readonly IMessageDispatcher _dispatcher;
        private readonly Transform _mainCam;
        
        private Transform _vrmRoot = null;

        /// <summary>
        /// VRMがロード済みの状態で呼び出すと、
        /// キーボード、マウス用のレイアウトパラメータと、ゲームパッド用のレイアウトパラメータを計算します。
        /// VRMがロードされていない場合はnullを返します。
        /// </summary>
        /// <returns></returns>
        public DeviceLayoutAutoAdjustParameters GetDeviceLayoutParameters()
        {
            if (_vrmRoot == null)
            {
                return null;
            }
            
            var result = new DeviceLayoutAutoAdjustParameters();

            var animator = _vrmRoot.GetComponent<Animator>();
            
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            result.HeightFactor = 
                (chest != null) ? 
                    chest.position.y / ReferenceChestHeight :
                    animator.GetBoneTransform(HumanBodyBones.Spine).position.y / ReferenceSpineHeight;
            
            var upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            var lowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
            var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            float armLength =
                Vector3.Distance(upperArm, lowerArm) +
                Vector3.Distance(lowerArm, wrist);

            result.ArmLengthFactor = armLength / ReferenceArmLength;

            return result;
        }
    
        private void AutoAdjust()
        {
            if (_vrmRoot == null) { return; }

            var parameters = new AutoAdjustParameters();
            //やること: 
            //1. いま読まれてるモデルの体型からいろんなパラメータを決めてparametersに入れていく
            //2. 決定したパラメータが疑似的にメッセージハンドラから飛んできたことにして適用
            //3. 決定したパラメータをコンフィグ側に送る

            try
            {
                var animator = _vrmRoot.GetComponent<Animator>();

                //3つのサブルーチンではanimatorのHumanoidBoneを使うが、部位である程度分けられるので分けておく
                SetHandSizeRelatedParameters(animator, parameters);
                AdjustCameraPosition(animator);
                //デバイスレイアウト調整: これは別途調整が終わるとメッセージが飛ぶ
                _dispatcher.ReceiveCommand(new ReceivedCommand(
                    MessageSerializer.None((ushort) VmmCommands.ResetDeviceLayout)
                    ));
                
                //3. 決定したパラメータをコンフィグ側に送る
                _sender.SendCommand(MessageFactory.Instance.AutoAdjustResults(parameters));
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        private void AdjustCameraPosition(Animator animator)
        {
            var head = animator.GetBoneTransform(HumanBodyBones.Neck);
            _mainCam.position = new Vector3(0, head.position.y, 1.3f);
            _mainCam.rotation = Quaternion.Euler(0, 180, 0);
        }
        
        private void SetHandSizeRelatedParameters(Animator animator, AutoAdjustParameters parameters)
        {
            var tip = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
            if (tip == null) { return; }

            var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
            float distance = Vector3.Distance(tip.position, wrist.position);

            float factor = distance / ReferenceHandLength;
            parameters.LengthFromWristToTip = (int)(parameters.LengthFromWristToTip * factor);
        }
    }
}

