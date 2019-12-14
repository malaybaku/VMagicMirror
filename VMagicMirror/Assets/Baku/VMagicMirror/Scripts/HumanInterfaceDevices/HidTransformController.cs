using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class HidTransformController : MonoBehaviour
    {
        [Inject]
        private ReceivedMessageHandler handler = null;

        [SerializeField] private KeyboardProvider keyboard = null;
        [SerializeField] private TouchPadProvider touchpad = null;
        [SerializeField] private ParticleStore particleStore = null;
        
        //NOTE: この辺の基準値はMegumi Baxterさんの表示時にちょうどよくなる値
        [SerializeField] private Vector3 refKeyboardPosition = new Vector3(0, 0.9f, 0.28f);
        [SerializeField] private Vector3 refKeyboardRotation = Vector3.zero;
        [SerializeField] private Vector3 refKeyboardScale = new Vector3(0.5f, 1f, 0.5f);

        [SerializeField] private Vector3 refTouchpadPosition = new Vector3(0.25f, 0.98f, 0.25f);
        [SerializeField] private Vector3 refTouchpadRotation = new Vector3(60, 30, 0);
        [SerializeField] private Vector3 refTouchpadScale = new Vector3(0.15f, 0.15f, 1.0f);

        private KeyboardVisibility _keyboardVisibility = null;
        private TouchpadVisibility _touchpadVisibility = null;
        
        private void Start()
        {
            _keyboardVisibility = keyboard.GetComponent<KeyboardVisibility>();
            _touchpadVisibility = touchpad.GetComponent<TouchpadVisibility>();
            
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.HidVisibility:
                        SetHidVisibility(message.ToBoolean());
                        break;
                }
            });
        }

        private void Update()
        {
            particleStore.ParticleScale = keyboard.transform.localScale;
            particleStore.ParticleRotation = keyboard.transform.rotation;
        }

        /// <summary>
        /// 指定されたパラメータベースを用いてタッチパッドとキーボードの位置を初期化します。
        /// </summary>
        /// <param name="parameters"></param>
        public void SetHidLayoutByParameter(DeviceLayoutAutoAdjustParameters parameters)
        {
            var keyboardTransform = keyboard.transform;
            var touchpadTransform = touchpad.transform;

            keyboardTransform.localRotation = Quaternion.Euler(refKeyboardRotation);
            keyboardTransform.localPosition = new Vector3(
                refKeyboardPosition.x * parameters.ArmLengthFactor,
                refKeyboardPosition.y * parameters.HeightFactor,
                refKeyboardPosition.z * parameters.ArmLengthFactor
                );
            keyboardTransform.localScale = new Vector3(
                refKeyboardScale.x * parameters.ArmLengthFactor,
                1.0f,
                refKeyboardScale.z * parameters.ArmLengthFactor
            ); 
            
            touchpadTransform.localRotation = Quaternion.Euler(refTouchpadRotation);
            touchpadTransform.localPosition = new Vector3(
                refTouchpadPosition.x * parameters.ArmLengthFactor,
                refTouchpadPosition.y * parameters.HeightFactor,
                refTouchpadPosition.z * parameters.ArmLengthFactor
            );
            touchpadTransform.localScale = new Vector3(
                refTouchpadScale.x * parameters.ArmLengthFactor,
                refTouchpadScale.y * parameters.ArmLengthFactor,
                1.0f
                );
        }
        
        private void SetHidVisibility(bool v)
        {
            _keyboardVisibility.SetVisibility(v);
            _touchpadVisibility.SetVisibility(v);
        }
    }

}

