using System;
using System.Collections;
using Baku.VMagicMirror.IK;
using mattatz.TransformControl;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 手をおろした状態の手の姿勢をカスタムできるようにするやつ
    /// </summary>
    public class CustomizedDownHandIk : PresenterBase
    {
        private readonly CustomizableHandIkTarget _leftHandTarget;
        private readonly CustomizableHandIkTarget _rightHandTarget;

        private readonly HandDownIkCalculator _handDownIkCalculator;
        private readonly DeviceTransformController _deviceTransformController;
        private readonly IMessageReceiver _receiver;
        private readonly IMessageSender _sender;
        private readonly IVRMLoadable _vrmLoadable;
        private readonly ICoroutineSource _coroutineSource;

        private readonly ReactiveProperty<bool> _enableFreeLayoutMode = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> _enableCustomHandDownPose = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> EnableCustomHandDownPose => _enableCustomHandDownPose;

        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKData LeftHand => _leftHand;
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKData RightHand => _rightHand;

        private readonly ReactiveProperty<bool> _showGizmo = new ReactiveProperty<bool>(false);
        private bool _hasModel;
        private Transform _hips;

        private readonly HandDownRestPose _currentPose = new HandDownRestPose();

        public CustomizedDownHandIk(
            IKTargetTransforms ikTargetTransforms, 
            HandDownIkCalculator handDownIkCalculator,
            IVRMLoadable vrmLoadable, 
            IMessageReceiver receiver,
            IMessageSender sender,
            DeviceTransformController deviceTransformController,
            ICoroutineSource coroutineSource
            )
        {
            _leftHandTarget = ikTargetTransforms.LeftHandDown;
            _rightHandTarget = ikTargetTransforms.RightHandDown;
            _sender = sender;
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _handDownIkCalculator = handDownIkCalculator;
            _deviceTransformController = deviceTransformController;
            _coroutineSource = coroutineSource;
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
                _hips = info.animator.GetBoneTransform(HumanBodyBones.Hips);
                _leftHandTarget.TargetTransform.SetParent(_hips);
                _rightHandTarget.TargetTransform.SetParent(_hips);
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _leftHandTarget.TargetTransform.SetParent(null);
                _rightHandTarget.TargetTransform.SetParent(null);
            };

            _deviceTransformController.ControlRequested
                .Subscribe(OnGizmoControlRequested)
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
                        _leftHandTarget.TransformControl.mode = TransformControl.TransformMode.None;
                        _rightHandTarget.TransformControl.mode = TransformControl.TransformMode.None;
                    }
                })
                .AddTo(this);

            //gizmoの操作が確定するとWPF側にも姿勢が送られる: ドラッグ操作の途中では内部的にのみ反映する
            _leftHandTarget.TransformControl.DragEnded += mode =>
            {
                if (mode == TransformControl.TransformMode.None) return;
                var pose = GetPoseFromTransform(_leftHandTarget.TargetTransform, true);
                SetPose(pose);
                SendPose();
            };

            _rightHandTarget.TransformControl.DragEnded += mode =>
            {
                if (mode == TransformControl.TransformMode.None) return;
                var pose = GetPoseFromTransform(_rightHandTarget.TargetTransform, false);
                SetPose(pose);
                SendPose();
            };
            
            _coroutineSource.StartCoroutine(UpdateOnEndOfFrame());
        }

        //片方の手の位置を調整
        IEnumerator UpdateOnEndOfFrame()
        {
            var eof = new WaitForEndOfFrame();
            while (true)
            {
                yield return eof;
                //NOTE: デフォルトの手下ろしとは更新タイミングが微妙に違う事に注意。何か起こるかもしれない…
                if (EnableCustomHandDownPose.Value)
                {
                    UpdateIkDataRecord();
                }

                if (!_showGizmo.Value)
                {
                    continue;
                }

                //操作中のgizmoは逐一見てIKに反映
                if (_leftHandTarget.TransformControl.IsDragging)
                {
                    var pose = GetPoseFromTransform(_leftHandTarget.TargetTransform, true);
                    SetPose(pose);
                }

                if (_rightHandTarget.TransformControl.IsDragging)
                {
                    var pose = GetPoseFromTransform(_rightHandTarget.TargetTransform, false);
                    SetPose(pose);
                }
            }
        }

        private void OnGizmoControlRequested(TransformControlRequest request)
        {
            if (!_showGizmo.Value)
            {
                return;
            }

            _leftHandTarget.TransformControl.global = request.WorldCoordinate;
            _rightHandTarget.TransformControl.global = request.WorldCoordinate;

            var rawMode = request.Mode;
            var useMode =
                rawMode == TransformControl.TransformMode.Translate || 
                rawMode == TransformControl.TransformMode.Rotate;
            var mode = useMode ? rawMode : TransformControl.TransformMode.None;
            
            //scaleは許可されてないことに注意
            _leftHandTarget.TransformControl.mode = mode;
            _rightHandTarget.TransformControl.mode = mode;
            _leftHandTarget.TransformControl.Control();
            _rightHandTarget.TransformControl.Control();
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
                SetPose(pose);
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

            var position = _hips.InverseTransformPoint(handIkTransform.position);
            var rotation = (Quaternion.Inverse(_hips.rotation) * handIkTransform.rotation).eulerAngles;

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
        
        private void SetPose(HandDownRestPose pose)
        {
            if (!pose.IsValid)
            {
                return;
            }

            Debug.Log(nameof(SetPose));
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

            _rightHandTarget.TargetTransform.localRotation = Quaternion.Euler(
                _currentPose.LeftRotation.x,
                -_currentPose.LeftRotation.y,
                -_currentPose.LeftRotation.z
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

            var hipsRot = _hips.rotation;
            
            _leftHand.Position = _hips.TransformPoint(_currentPose.LeftPosition);
            _leftHand.Rotation = hipsRot * Quaternion.Euler(_currentPose.LeftRotation);
            
            _rightHand.Position = _hips.TransformPoint(new Vector3(
                -_currentPose.LeftPosition.x,
                _currentPose.LeftPosition.y,
                _currentPose.LeftPosition.z)
            );
            _rightHand.Rotation = hipsRot * Quaternion.Euler(
                _currentPose.LeftRotation.x,
                -_currentPose.LeftRotation.y,
                -_currentPose.LeftRotation.z
            );
        }
        
        private void SendPose()
        {
            Debug.Log(nameof(SendPose));
            _sender.SendCommand(
                MessageFactory.Instance.UpdateHandDownRestPose(JsonUtility.ToJson(_currentPose))
                );
        }
        
        private void ResetCustomHandDownPose()
        {
            Debug.Log(nameof(ResetCustomHandDownPose));
            // var ik = _handDownIkCalculator.LeftHandLocalOnUpperArm;
            var ik = _handDownIkCalculator.LeftHandLocalFromHips;
            var pose = new HandDownRestPose()
            {
                IsValid = true,
                LeftPosition = ik.Position,
                LeftRotation = ik.Rotation.eulerAngles,
            };
            SetPose(pose);
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
