using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>指の制御をFingerControllerより後、かつマッスルベースで動かしたいときに使うやつ</summary>
    public class FingerLateController : MonoBehaviour
    {
        private const float DefaultBendingAngle = 10.0f;
        private const float HoldSpeedFactor = 18.0f;
        //角度ベースの値をMuscleのレートにおおよそ換算してくれるすごいやつだよ
        //観察ベースで「マッスル値をいくつ変えたら90度ぶん動くかな～」と調べてます
        private const float BendDegToMuscle = 1.65f / 90f;
        
        private Animator _animator = null;
        //角度入力時に使うマッスル系の情報
        private HumanPoseHandler _humanPoseHandler = null;
        //Tポーズ時点のポーズ情報
        private HumanPose _defaultHumanPose = default;
        //毎フレーム書き換えるポーズ情報
        private HumanPose _humanPose = default;

        private Transform[][] _fingers = null;
        
        private readonly bool[] _hold = new bool[10];
        private readonly float[] _targetAngles = new float[10];
        //「指を曲げっぱなしにする/離す」というオペレーションによって決まる値
        private readonly float[] _holdOperationBendingAngle = new float[10];

        private bool _hasHoldCalledAtLeastOnce = false;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += () =>
            {
                if (_animator == null)
                {
                    return;
                }
                _animator = null;
                _humanPoseHandler = null;
                _fingers = null;
            };
        }

        /// <summary>
        /// 特定の指の曲げ角度を固定します。
        /// </summary>
        /// <param name="fingerNumber"></param>
        /// <param name="angle"></param>
        public void Hold(int fingerNumber, float angle)
        {
            if (fingerNumber >= 0 && fingerNumber < _hold.Length)
            {
                _hold[fingerNumber] = true;
                _targetAngles[fingerNumber] = angle;
            }

            _hasHoldCalledAtLeastOnce = true;
        }

        /// <summary>
        /// <see cref="Hold"/>で固定していた指を解放します。
        /// </summary>
        /// <param name="fingerNumber"></param>
        public void Release(int fingerNumber)
        {
            if (fingerNumber >= 0 && fingerNumber < _hold.Length)
            {
                _hold[fingerNumber] = false;
                _targetAngles[fingerNumber] = 0;
            }
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _animator = info.animator;
            _humanPoseHandler = new HumanPoseHandler(_animator.avatar, _animator.transform);
            //とりあえず現在の値を拾っておく
            _humanPoseHandler.GetHumanPose(ref _humanPose);
            _defaultHumanPose = _humanPose;

            for (int i = 0; i < 10; i++)
            {
                _hold[i] = false;
                _targetAngles[i] = DefaultBendingAngle;
            }
            
            _fingers = new Transform[][]
            {
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightRingDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightRingProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleProximal),
                },
            };
        }

        private void LateUpdate()
        {
            if (!_hasHoldCalledAtLeastOnce || _animator == null)
            {
                return;
            }

            //今から曲げるべき指があるとか、指の曲げ/戻し中であるような場合だけ処理する。
            //このガードにより、普段はMuscleのI/Oが走らないため、CPUにとてもやさしい
            bool needUpdate = false;
            for (int i = 0; i < _hold.Length; i++)
            {
                if (_hold[i])
                {
                    needUpdate = true;
                    break;
                }
            }

            if (!needUpdate)
            {
                return;
            }
            
            _humanPoseHandler.GetHumanPose(ref _humanPose);

            for (int i = 0; i < 10; i++)
            {
                if (!_hold[i])
                {
                    continue;
                }
                
                _holdOperationBendingAngle[i] = Mathf.Lerp(
                    _holdOperationBendingAngle[i],
                    _targetAngles[i],
                    HoldSpeedFactor * Time.deltaTime
                    );
                
                float angle = _holdOperationBendingAngle[i];

                //MuscleをTポーズから変化させる値
                //常にマイナスの値を入れればOK: デフォルトから曲げ方向に動かすため
                float rate = -angle * BendDegToMuscle;
                FingerMuscleSetter.BendFinger(in _defaultHumanPose, ref _humanPose, i, rate);
            }

            _humanPoseHandler.SetHumanPose(ref _humanPose);
        }

        private void BendFinger(int fingerIndex, float angle)
        {
            if (fingerIndex > 4)
            {
                angle = -angle;
            }
            
            var fingerTransforms = _fingers[fingerIndex];
            for (int i = 0; i < fingerTransforms.Length; i++)
            {
                if (fingerTransforms[i] == null)
                {
                    continue;
                }
                fingerTransforms[i].localRotation = Quaternion.AngleAxis(
                    angle,
                    GetRotationAxis(fingerIndex, i)
                );
            }
        }
        
        private static Vector3 GetRotationAxis(int fingerNumber, int jointIndex)
        {
            if ((fingerNumber == FingerConsts.LeftThumb || fingerNumber == FingerConsts.RightThumb) && 
                jointIndex < 2
            )
            {
                return Vector3.down;
            }
            else
            {
                return Vector3.forward;
            }
        }
    }
}
