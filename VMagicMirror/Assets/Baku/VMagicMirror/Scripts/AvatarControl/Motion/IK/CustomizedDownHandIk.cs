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
    public class CustomizedDownHandIk : PresenterBase, ITickable
    {
        private readonly CustomizableHandIkTarget _leftHandTarget;
        private readonly CustomizableHandIkTarget _rightHandTarget;

        private readonly HandDownIkCalculator _handDownIkCalculator;
        private readonly DeviceTransformController _deviceTransformController;
        private readonly IMessageReceiver _receiver;
        private readonly IMessageSender _sender;
        private readonly IVRMLoadable _vrmLoadable;

        private readonly ReactiveProperty<bool> _enableFreeLayoutMode = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> _enableCustomHandDownPose = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> EnableCustomHandDownPose => _enableCustomHandDownPose;

        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKData LeftHand => _leftHand;
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKData RightHand => _rightHand;

        private readonly ReactiveProperty<bool> _showGizmo = new ReactiveProperty<bool>(false);
        private bool _hasModel;
        private Transform _leftUpperArm;
        private Transform _rightUpperArm;

        private readonly HandDownRestPose _currentPose = new HandDownRestPose();
        private bool _leftControlIsActive;
        private bool _rightControlIsActive;

        public CustomizedDownHandIk(
            IKTargetTransforms ikTargetTransforms, 
            HandDownIkCalculator handDownIkCalculator,
            IVRMLoadable vrmLoadable, 
            IMessageReceiver receiver,
            IMessageSender sender,
            DeviceTransformController deviceTransformController
            )
        {
            _leftHandTarget = ikTargetTransforms.LeftHandDown;
            _rightHandTarget = ikTargetTransforms.RightHandDown;
            _sender = sender;
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _handDownIkCalculator = handDownIkCalculator;
            _deviceTransformController = deviceTransformController;
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.EnableCustomHandDownPose,
                command =>
                {
                    _enableCustomHandDownPose.Value = command.ToBoolean();
                });

            _receiver.AssignCommandHandler(
                VmmCommands.EnableDeviceFreeLayout,
                command => _enableFreeLayoutMode.Value = command.ToBoolean()
            );

            _receiver.AssignCommandHandler(
                VmmCommands.SetHandDownModeCustomPose,
                command => ApplyHandDownPose(command.Content)
            );

            _receiver.AssignCommandHandler(
                VmmCommands.ResetCustomHandDownPose,
                _ =>
                {
                    ResetCustomHandDownPose();
                    SendPose();
                });

            _vrmLoadable.VrmLoaded += info =>
            {
                _leftUpperArm = info.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                _rightUpperArm = info.animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                _leftHandTarget.TargetTransform.SetParent(_leftUpperArm);
                _rightHandTarget.TargetTransform.SetParent(_rightUpperArm);
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _leftHandTarget.TargetTransform.SetParent(null);
                _rightHandTarget.TargetTransform.SetParent(null);
                _leftUpperArm = null;
                _rightUpperArm = null;
            };

            _deviceTransformController.ControlRequested
                .Subscribe(OnDeviceControlRequested)
                .AddTo(this);
            
            _enableFreeLayoutMode
                .CombineLatest(EnableCustomHandDownPose, (x, y) => x && y)
                .DistinctUntilChanged()
                .Subscribe(gizmoVisible =>
                {
                    _showGizmo.Value = gizmoVisible;
                    _leftHandTarget.SetGizmoImageActiveness(gizmoVisible);
                    _rightHandTarget.SetGizmoImageActiveness(gizmoVisible);
                    if (!gizmoVisible)
                    {
                        _leftControlIsActive = false;
                        _rightControlIsActive = false;
                    }
                })
                .AddTo(this);
        }

        //片方の手の位置を調整
        void ITickable.Tick()
        {
            //NOTE: デフォルトの手下ろしとは更新タイミングが微妙に違う事に注意。何か起こるかもしれない…
            if (EnableCustomHandDownPose.Value)
            {
                UpdateIkDataRecord();
            }

            if (!_showGizmo.Value)
            {
                return;
            }

            //gizmoの操作が確定するとWPF側にも姿勢が送られる
            if (_leftControlIsActive && !_leftHandTarget.TransformControl.IsDragging)
            {
                var pose = GetPoseFromTransform(_leftHandTarget.TargetTransform, true);
                SetPosesToTransforms(pose);
                SendPose();
            }

            if (_rightControlIsActive && !_rightHandTarget.TransformControl.IsDragging)
            {
                var pose = GetPoseFromTransform(_rightHandTarget.TargetTransform, false);
                SetPosesToTransforms(pose);
                SendPose();
            }

            _leftControlIsActive = _leftHandTarget.TransformControl.IsDragging;
            _rightControlIsActive = _rightHandTarget.TransformControl.IsDragging;
            
            //操作中のgizmoは逐一見てIKに反映
            if (_leftHandTarget.TransformControl.IsDragging)
            {
                var pose = GetPoseFromTransform(_leftHandTarget.TargetTransform, true);
                SetPosesToTransforms(pose);
            }

            if (_rightHandTarget.TransformControl.IsDragging)
            {
                var pose = GetPoseFromTransform(_rightHandTarget.TargetTransform, false);
                SetPosesToTransforms(pose);
            }
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
            _leftHandTarget.TransformControl.mode = useMode ? mode : TransformControl.TransformMode.None;
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

            _leftHandTarget.TargetTransform.localPosition = _currentPose.LeftPosition;
            _leftHandTarget.TargetTransform.localRotation = Quaternion.Euler(_currentPose.LeftRotation);

            _rightHandTarget.TargetTransform.localPosition = new Vector3(
                -_currentPose.LeftPosition.x,
                _currentPose.LeftPosition.y,
                _currentPose.LeftPosition.z
            );

            //TODO: これで合ってるかは要確認ですよ
            _rightHandTarget.TargetTransform.localRotation = Quaternion.Euler(
                _currentPose.LeftPosition.x,
                -_currentPose.LeftPosition.y,
                -_currentPose.LeftPosition.z
            );
        }

        private void UpdateIkDataRecord()
        {
            if (!_hasModel)
            {
                return;
            }

            if (!_currentPose.IsValid)
            {
                ResetCustomHandDownPose();
            }

            _leftHand.Position = _leftUpperArm.TransformPoint(_currentPose.LeftPosition);
            _leftHand.Rotation = _leftUpperArm.rotation * Quaternion.Euler(_currentPose.LeftRotation);
            
            _rightHand.Position = _rightUpperArm.TransformPoint(new Vector3(
                -_currentPose.LeftPosition.x,
                _currentPose.LeftPosition.y,
                _currentPose.LeftPosition.z)
            );
            _rightHand.Rotation = _rightUpperArm.rotation * Quaternion.Euler(
                _currentPose.LeftRotation.x,
                -_currentPose.LeftRotation.y,
                -_currentPose.LeftRotation.z
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
