using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class VMCPHeadPose
    {
        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;

        //NOTE: アバターの足元Rootから見たときのローカル座標として扱いたい。ゲーム入力中でなければワールド座標とほぼ同義。
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        public void SetActive(bool active) => _isActive.Value = active;
        
        public void SetPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}

