using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    // TODO: Setterを許可するかどうか少し考えると面白いかも (=アバターの邪魔するサブキャラが作れてもいいかも)
    // ただしアプリ終了を挟んでSetterの結果が影響するのはダルいので、そこは要検討

    /// <summary>
    /// キーボード、ゲームパッドなどの、VMMが標準で空間中に配置しているオブジェクトの配置情報が取得出来るクラス
    /// </summary>
    public class DeviceLayoutApiImplement
    {
        private readonly Camera _camera;
        private readonly KeyboardProvider _keyboardProvider;
        private readonly TouchPadProvider _touchPadProvider;
        private readonly PenTabletProvider _penTabletProvider;
        private readonly GamepadProvider _gamepadProvider;
        //NOTE: アーケードスティックとかハンドルも追加してよい (現時点では単に面倒で省いているだけ)

        // NOTE: 都度コンポーネントを拾うのとどっちが良いかかなり微妙なとこではあるが、キャッシュしておく
        private readonly KeyboardVisibilityView _keyboardVisibility;
        private readonly TouchpadVisibilityView _touchpadVisibility;
        private readonly PenTabletVisibilityView _penTabletVisibility;
        private readonly GamepadVisibilityView _gamepadVisibility;

        public DeviceLayoutApiImplement(
            Camera mainCamera,
            KeyboardProvider keyboardProvider,
            TouchPadProvider touchPadProvider,
            PenTabletProvider penTabletProvider,
            GamepadProvider gamepadProvider
        )
        {
            _camera = mainCamera;
            _keyboardProvider = keyboardProvider;
            _touchPadProvider = touchPadProvider;
            _penTabletProvider = penTabletProvider;
            _gamepadProvider = gamepadProvider;

            _keyboardVisibility = keyboardProvider.GetVisibilityView();
            _touchpadVisibility = touchPadProvider.GetVisibilityView();
            _penTabletVisibility = penTabletProvider.GetVisibilityView();
            _gamepadVisibility = gamepadProvider.GetVisibilityView();
        }

        public Pose GetCameraPose()
        {
            var t = _camera.transform;
            return new Pose(t.position, t.rotation);
        }

        public float GetCameraFov() => _camera.fieldOfView;
        
        public Pose GetKeyboardPose() => _keyboardProvider.GetPose();
        public Pose GetTouchpadPose() => _touchPadProvider.GetPose();
        public Pose GetPenTabletPose() => _penTabletProvider.GetPose();
        public Pose GetGamepadPose() => _gamepadProvider.GetModelRootPose();

        public bool GetKeyboardVisible() => _keyboardVisibility.IsVisible;
        public bool GetTouchpadVisible() => _touchpadVisibility.IsVisible;
        public bool GetPenTabletVisible() => _penTabletVisibility.IsVisible;
        public bool GetGamepadVisible() => _gamepadVisibility.IsVisible;
    }
}