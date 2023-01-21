using System;
using Baku.VMagicMirror.IK;
using mattatz.TransformControl;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 手をおろした状態の手の姿勢をカスタムできるようにするやつ
    /// </summary>
    public class CustomizedDownHandIk : MonoBehaviour
    {
        private CustomizableHandIkTarget leftHand;
        private CustomizableHandIkTarget rightHand;
        private HandDownIkCalculator _handDownIkCalculator;
        private IMessageSender _sender;

        private readonly ReactiveProperty<bool> _enableFreeLayoutMode = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> _enableCustomHandDownPose = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> EnableCustomHandDownPose => _enableCustomHandDownPose;

        private readonly ReactiveProperty<bool> _showGizmo = new ReactiveProperty<bool>(false);
        private bool _hasModel;
        private Transform _hips;

        private readonly HandDownRestPose _currentPose = new HandDownRestPose();
        private bool _leftControlIsActive;
        private bool _rightControlIsActive;

        [Inject]
        public void Initialize(
            IKTargetTransforms ikTargetTransforms, 
            HandDownIkCalculator handDownIkCalculator,
            IVRMLoadable vrmLoadable, 
            IMessageReceiver receiver,
            IMessageSender sender,
            DeviceTransformController deviceTransformController
            )
        {
            leftHand = ikTargetTransforms.LeftHandDown;
            rightHand = ikTargetTransforms.RightHandDown;
            _sender = sender;
            _handDownIkCalculator = handDownIkCalculator;

            receiver.AssignCommandHandler(
                VmmCommands.EnableCustomHandDownPose,
                command => _enableCustomHandDownPose.Value = command.ToBoolean()
            );

            receiver.AssignCommandHandler(
                VmmCommands.EnableDeviceFreeLayout,
                command => _enableFreeLayoutMode.Value = command.ToBoolean()
            );

            receiver.AssignCommandHandler(
                VmmCommands.SetHandDownModeCustomPose,
                command => ApplyHandDownPose(command.Content)
            );

            receiver.AssignCommandHandler(
                VmmCommands.ResetCustomHandDownPose,
                _ => ResetCustomHandDownPose()
            );

            vrmLoadable.VrmLoaded += info =>
            {
                _hips = info.animator.GetBoneTransform(HumanBodyBones.Hips);
                leftHand.TargetTransform.SetParent(_hips);
                rightHand.TargetTransform.SetParent(_hips);
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                leftHand.TargetTransform.SetParent(null);
                rightHand.TargetTransform.SetParent(null);
                _hips = null;
            };

            deviceTransformController.ControlRequested
                .Subscribe(OnDeviceControlRequested)
                .AddTo(this);
        }

        private void Start()
        {
            _enableFreeLayoutMode
                .CombineLatest(EnableCustomHandDownPose, (x, y) => x && y)
                .DistinctUntilChanged()
                .Subscribe(gizmoVisible =>
                {
                    _showGizmo.Value = gizmoVisible;
                    leftHand.SetGizmoImageActiveness(gizmoVisible);
                    rightHand.SetGizmoImageActiveness(gizmoVisible);
                    if (!gizmoVisible)
                    {
                        _leftControlIsActive = false;
                        _rightControlIsActive = false;
                    }
                })
                .AddTo(this);
        }

        //片方の手の位置を調整
        private void Update()
        {
            if (!_showGizmo.Value)
            {
                return;
            }

            //左手のgizmoが操作され、確定した
            if (_leftControlIsActive && !leftHand.TransformControl.IsDragging)
            {
                var pose = GetPoseFromTransform(leftHand.TargetTransform, true);
                SetPosesToTransforms(pose);
            }

            if (_rightControlIsActive && !rightHand.TransformControl.IsDragging)
            {
                var pose = GetPoseFromTransform(rightHand.TargetTransform, false);
                SetPosesToTransforms(pose);
            }

            _leftControlIsActive = leftHand.TransformControl.IsDragging;
            _rightControlIsActive = rightHand.TransformControl.IsDragging;
        }

        private void OnDeviceControlRequested(TransformControlRequest request)
        {
            if (!_showGizmo.Value)
            {
                return;
            }

            var mode = request.Mode;
            var useMode =
                mode == TransformControl.TransformMode.Translate || 
                mode == TransformControl.TransformMode.Rotate;
            //scaleは許可されてないことに注意
            leftHand.TransformControl.mode = useMode ? mode : TransformControl.TransformMode.None;
        }

        private void ApplyHandDownPose(string poseJson)
        {
            //無効なデータを渡されても上書きしない: 場合によってはコレで変になるが、まあ気にしない方向で…
            if (string.IsNullOrEmpty(poseJson))
            {
                return;
            }

            try
            {
                var pose = JsonUtility.FromJson<HandDownRestPose>(poseJson);
                SetPosesToTransforms(pose);
            }
            catch
            {
                //パースエラーのはずなので無視
            }
        }

        private HandDownRestPose GetPoseFromTransform(Transform handIkTransform, bool isLeft)
        {
            if (!_hasModel)
            {
                return HandDownRestPose.Empty;
            }

            var position = handIkTransform.localPosition;
            var rotation = handIkTransform.localRotation.eulerAngles;

            if (!isLeft)
            {
                position = new Vector3(-position.x, position.y, position.z);
                rotation = new Vector3(rotation.x, -rotation.y, -rotation.z);
            }

            return new HandDownRestPose()
            {
                IsValid = true,
                LeftPosition = position,
                LeftRotation = rotation,
            };
        }
        
        private void SetPosesToTransforms(HandDownRestPose pose)
        {
            if (!pose.IsValid)
            {
                return;
            }

            //結果的にtrueにしかならない
            _currentPose.IsValid = pose.IsValid;
            _currentPose.LeftPosition = pose.LeftPosition;
            _currentPose.LeftRotation = pose.LeftRotation;

            leftHand.TargetTransform.localPosition = _currentPose.LeftPosition;
            leftHand.TargetTransform.localRotation = Quaternion.Euler(_currentPose.LeftRotation);

            rightHand.TargetTransform.localPosition = new Vector3(
                -_currentPose.LeftPosition.x,
                _currentPose.LeftPosition.y,
                _currentPose.LeftPosition.z
            );

            //TODO: これで合ってるかは要確認ですよ
            rightHand.TargetTransform.localRotation = Quaternion.Euler(
                _currentPose.LeftPosition.x,
                -_currentPose.LeftPosition.y,
                -_currentPose.LeftPosition.z
            );
        }
        
        private void SendPose()
        {
            _sender.SendCommand(
                MessageFactory.Instance.UpdateHandDownRestPose(JsonUtility.ToJson(_currentPose))
                );
        }
        
        private void ResetCustomHandDownPose()
        {
            var ik = _handDownIkCalculator.LeftHandLocalToHip;
            var pose = new HandDownRestPose()
            {
                IsValid = true,
                LeftPosition = ik.Position,
                LeftRotation = ik.Rotation.eulerAngles,
            };
            SetPosesToTransforms(pose);
            SendPose();
        }
    }

    [Serializable]
    public class HandDownRestPose
    {
        public bool IsValid;
        //NOTE: とりあえず左右対象の設定のみを保持する
        public Vector3 LeftPosition;

        //オイラー角で入れておく
        public Vector3 LeftRotation;

        public static HandDownRestPose Empty { get; } = new HandDownRestPose();
    }
}
