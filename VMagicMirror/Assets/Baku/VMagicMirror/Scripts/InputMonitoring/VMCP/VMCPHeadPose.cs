using Google.Protobuf.WellKnownTypes;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPHeadPose : IInitializable, ITickable
    {
        private const float DisconnectCount = VMCPReceiver.DisconnectCount;
        private const float ResetLerpFactor = 6f;

        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;

        //TODO: 受信が一定時間滞った場合に非接続扱いできるようにしたい
        private readonly ReactiveProperty<bool> _connected = new ReactiveProperty<bool>(true);
        public IReadOnlyReactiveProperty<bool> Connected => _connected;

        private readonly IVRMLoadable _vrmLoadable;
        //NOTE: 0にはしないでおく、一応
        private Vector3 _defaultHeadOffsetOnHips = Vector3.up;
        private bool _hasModel;

        private bool _isConnected;
        private float _disconnectCountDown;

        public VMCPHeadPose(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }

        public void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                //NOTE: ロードした瞬間はyaw=0で立ってるはず
                _defaultHeadOffsetOnHips =
                    info.animator.GetBoneTransform(HumanBodyBones.Head).position -
                    info.animator.GetBoneTransform(HumanBodyBones.Hips).position;
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _defaultHeadOffsetOnHips = Vector3.up;
            };
        }

        void ITickable.Tick()
        {
            if (!_isActive.Value || !_isConnected || _disconnectCountDown <= 0f)
            {
                if (_isActive.Value)
                {
                    var factor = ResetLerpFactor * Time.deltaTime;
                    PositionOffset = Vector3.Lerp(PositionOffset, Vector3.zero, factor);
                    Rotation = Quaternion.Slerp(Rotation, Quaternion.identity, factor);
                }
                return;
            }

            //ポーズを一定時間受け取れてない場合、切断したものと見なす
            _disconnectCountDown -= Time.deltaTime;
            if (_disconnectCountDown <= 0f)
            {
                _isConnected = false;
            }
        }

        /// <summary> 直立時にゼロになるような、足元の座標系で見たときの頭の移動量 </summary>
        public Vector3 PositionOffset { get; private set; }

        /// <summary> 正面向きならゼロ回転になるような頭部回転 </summary>
        public Quaternion Rotation { get; private set; }

        public void SetActive(bool active)
        {
            _isActive.Value = active;
            if (!active)
            {
                _isConnected = false;
                _disconnectCountDown = 0f;
            }
        }

        public void SetPoseOnHips(Pose pose)
        {
            if (!_hasModel)
            {
                return;
            }
            PositionOffset = pose.position - _defaultHeadOffsetOnHips;
            Rotation = pose.rotation;

            _isConnected = true;
            _disconnectCountDown = DisconnectCount;
        }
    }
}

