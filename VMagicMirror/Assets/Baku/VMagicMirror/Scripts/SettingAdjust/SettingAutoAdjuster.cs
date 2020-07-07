using System;
using Baku.VMagicMirror.InterProcess;
using UnityEngine;
using UniRx;
using Zenject;
using IMessageReceiver = Baku.VMagicMirror.InterProcess.IMessageReceiver;

namespace Baku.VMagicMirror
{
    public class SettingAutoAdjuster : MonoBehaviour
    {
        //基準長はMegumi Baxterさんの体型。(https://hub.vroid.com/characters/9003440353945198963/models/7418874241157618732)
        private const float ReferenceChestHeight = 0.89008f;
        private const float ReferenceSpineHeight = 0.78448f;
        //UpperArm to Hand
        private const float ReferenceArmLength = 0.378f;
        //Hand (Wrist) to Middle Distal
        private const float ReferenceHandLength = 0.114f;

        [SerializeField] private BlendShapeAssignReceiver blendShapeAssignReceiver = null;
        [SerializeField] private Transform cam = null;

        private IMessageSender _sender = null;
        private IMessageDispatcher _dispatcher = null;
        
        private Transform _vrmRoot = null;

        public void AssignModelRoot(Transform vrmRoot) => _vrmRoot = vrmRoot;

        public void DisposeModelRoot() => _vrmRoot = null;

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

        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender, IMessageDispatcher dispatcher)
        {
            _sender = sender;
            _dispatcher = dispatcher;
            
            receiver.AssignCommandHandler(
                MessageCommandNames.RequestAutoAdjust,
                _ => AutoAdjust()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.RequestAutoAdjustEyebrow,
                _ => AutoAdjustOnlyEyebrow()
            );
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
                //眉毛はブレンドシェイプ
                SetEyebrowParameters(parameters);
                AdjustCameraPosition(animator);

                SendParameterRelatedCommands(parameters);

                //3. 決定したパラメータをコンフィグ側に送る
                _sender.SendCommand(MessageFactory.Instance.AutoAdjustResults(parameters));
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void AutoAdjustOnlyEyebrow()
        {
            if (_vrmRoot == null)
            {
                return;
            }

            var parameters = new AutoAdjustParameters();
            try
            {
                SetEyebrowParameters(parameters);
                SendParameterRelatedCommands(parameters, true);
                _sender.SendCommand(MessageFactory.Instance.AutoAdjustEyebrowResults(parameters));
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void SendParameterRelatedCommands(AutoAdjustParameters parameters, bool onlyEyebrow)
        {
            var eyebrowCommands = new ReceivedCommand[]
            {
                new ReceivedCommand(
                    MessageCommandNames.EyebrowLeftUpKey,
                    parameters.EyebrowLeftUpKey
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowLeftDownKey,
                    parameters.EyebrowLeftDownKey
                    ),
                new ReceivedCommand(
                    MessageCommandNames.UseSeparatedKeyForEyebrow,
                    $"{parameters.UseSeparatedKeyForEyebrow}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowRightUpKey,
                    parameters.EyebrowRightUpKey
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowRightDownKey,
                    parameters.EyebrowRightDownKey
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowUpScale,
                    $"{parameters.EyebrowUpScale}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.EyebrowDownScale,
                    $"{parameters.EyebrowDownScale}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.LengthFromWristToPalm,
                    $"{parameters.LengthFromWristToPalm}"
                    ),
                new ReceivedCommand(
                    MessageCommandNames.LengthFromWristToTip,
                    $"{parameters.LengthFromWristToTip}"
                    ),
            };
            foreach (var cmd in eyebrowCommands)
            {
                _dispatcher.ReceiveCommand(cmd);
            }

            if (onlyEyebrow)
            {
                return;
            }

            //レイアウト調整はコレ一発でおしまいです
            _dispatcher.ReceiveCommand(new ReceivedCommand(MessageCommandNames.ResetDeviceLayout));
        }

        private void SendParameterRelatedCommands(AutoAdjustParameters parameters)
        {
            SendParameterRelatedCommands(parameters, false);
        }

        private void AdjustCameraPosition(Animator animator)
        {
            var head = animator.GetBoneTransform(HumanBodyBones.Neck);
            cam.position = new Vector3(0, head.position.y, 1.3f);
            cam.rotation = Quaternion.Euler(0, 180, 0);
        }

        private void SetEyebrowParameters(AutoAdjustParameters parameters)
        {
            var blendShapeNames = blendShapeAssignReceiver.TryGetBlendShapeNames();
            var adjuster = new EyebrowBlendShapeAdjuster(blendShapeNames);
            var settings = adjuster.CreatePreferredSettings();
            parameters.EyebrowIsValidPreset = settings.IsValidPreset;
            parameters.EyebrowLeftUpKey = settings.EyebrowLeftUpKey;
            parameters.EyebrowLeftDownKey = settings.EyebrowLeftDownKey;
            parameters.UseSeparatedKeyForEyebrow = settings.UseSeparatedKeyForEyebrow;
            parameters.EyebrowRightUpKey = settings.EyebrowRightUpKey;
            parameters.EyebrowRightDownKey = settings.EyebrowRightDownKey;
            parameters.EyebrowUpScale = settings.EyebrowUpScale;
            parameters.EyebrowDownScale = settings.EyebrowDownScale;
        }

        private void SetHandSizeRelatedParameters(Animator animator, AutoAdjustParameters parameters)
        {
            var tip = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
            if (tip == null) { return; }

            var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
            float distance = Vector3.Distance(tip.position, wrist.position);

            float factor = distance / ReferenceHandLength;

            parameters.LengthFromWristToPalm = (int)(parameters.LengthFromWristToPalm * factor);
            parameters.LengthFromWristToTip = (int)(parameters.LengthFromWristToTip * factor);

        }

    }
}

