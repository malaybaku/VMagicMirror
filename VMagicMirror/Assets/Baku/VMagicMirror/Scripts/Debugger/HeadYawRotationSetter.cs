using Baku.VMagicMirror.IK;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(LateUpdateSourceAfterFinalIK))]
    public class HeadYawRotationSetter : MonoBehaviour
    {
        [SerializeField] private Transform avatarRoot;
        [SerializeField] private Transform target;
        [SerializeField] private Transform[] eyes;
        [SerializeField] private CarHandleIkGenerator ik;
        [SerializeField] private float factor = 30f;
        [SerializeField] private float eyeFactor = 6f;
        //
        // private void Start()
        // {
        //     GetComponent<LateUpdateSourceAfterFinalIK>()
        //         .OnLateUpdate
        //         .Subscribe(_ => SetYawRotation())
        //         .AddTo(this);
        // }
        //
        // private void Update()
        // {
        //     if (avatarRoot != null)
        //     {
        //         avatarRoot.localRotation = Quaternion.Euler(
        //             0,
        //             ik.HeadYawRotationRate.Value * factor,
        //             0
        //         );
        //     }
        // }
        //
        // private void SetYawRotation()
        // {
        //     if (target != null)
        //     {
        //         target.localRotation = Quaternion.Euler(
        //             0,
        //             ik.HeadYawRotationRate.Value * factor,
        //             0
        //         );
        //     }
        //     
        //     foreach (var eye in eyes)
        //     {
        //         eye.localRotation = Quaternion.Euler(
        //             0,
        //             ik.EyeRotationRate.Value * eyeFactor,
        //             0
        //         );
        //     }
        // }
    }
}
