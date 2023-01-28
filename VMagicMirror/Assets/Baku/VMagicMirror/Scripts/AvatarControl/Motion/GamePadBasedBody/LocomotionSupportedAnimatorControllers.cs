using UnityEngine;

namespace Baku.VMagicMirror
{
    [CreateAssetMenu(menuName = "VMagicMirror/LocomotionSupportedAnimatorControllers")]
    public class LocomotionSupportedAnimatorControllers: ScriptableObject
    {
        //DefaultControllerのなかでアニメーションの参照抜けが発生する場合、このフラグをfalseにする
        //(※モーションがなくなるので、ゲームパッドによる全身移動の機能は動かなくなる)
        private const bool UseDefaultController = true;

        //NOTE: Controllerが参照するClipをOSSとして公開しない可能性があるので、明示的にカラのControllerも用意している
        [SerializeField] private RuntimeAnimatorController emptyController;
        [SerializeField] private RuntimeAnimatorController defaultController;

        public RuntimeAnimatorController DefaultController =>
            UseDefaultController ? defaultController : emptyController;
    }
}
