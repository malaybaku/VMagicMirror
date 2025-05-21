namespace VMagicMirror.Buddy
{
    public interface IDeviceLayout
    {
        /// <summary>
        /// 画面を映しているカメラについて、現在の姿勢を取得します。
        /// </summary>
        /// <returns>カメラの姿勢</returns>
        Pose GetCameraPose();
        
        /// <summary>
        /// 画面を映しているカメラについて、視野角(Field of View)を度数法の値で取得します。
        /// この視野角は垂直方向の視野角を表します。
        /// </summary>
        /// <returns>カメラの視野角 [deg]</returns>
        float GetCameraFov();
        
        /// <summary>
        /// 画面内に表示されるキーボードの基準位置の姿勢を取得します。
        /// キーボードが非表示の場合も姿勢を取得できます。
        /// </summary>
        /// <returns>キーボードの姿勢</returns>
        Pose GetKeyboardPose();

        /// <summary>
        /// 画面内に表示されるタッチパッドの基準位置の姿勢を取得します。
        /// タッチパッドが非表示の場合も姿勢を取得できます。
        /// </summary>
        /// <returns>タッチパッドの姿勢</returns>
        Pose GetTouchpadPose();

        /// <summary>
        /// 画面内に表示されるペンタブレットの基準位置の姿勢を取得します。
        /// ペンタブレットが非表示の場合も姿勢を取得できます。
        /// </summary>
        /// <returns>ペンタブレットの姿勢</returns>
        Pose GetPenTabletPose();
        
        /// <summary>
        /// 画面内に表示されるゲームパッドの基準位置の姿勢を取得します。
        /// ゲームパッドが非表示の場合も姿勢を取得できます。
        /// </summary>
        /// <returns>ゲームパッドの姿勢</returns>
        Pose GetGamepadPose();
        
        /// <summary>
        /// キーボードを表示中かどうかを取得します。
        /// </summary>
        /// <returns>キーボードを表示中ならば <c>true</c>、そうでなければ <c>false</c></returns>
        bool GetKeyboardVisible();

        /// <summary>
        /// タッチパッドを表示中かどうかを取得します。
        /// </summary>
        /// <returns>タッチパッドを表示中ならば <c>true</c>、そうでなければ <c>false</c></returns>
        bool GetTouchpadVisible();
        
        /// <summary>
        /// ペンタブレットを表示中かどうかを取得します。
        /// </summary>
        /// <returns>ペンタブレットを表示中ならば <c>true</c>、そうでなければ <c>false</c></returns>
        bool GetPenTabletVisible();
        
        /// <summary>
        /// ゲームパッドを表示中かどうかを取得します。
        /// </summary>
        /// <returns>ゲームパッドを表示中ならば <c>true</c>、そうでなければ <c>false</c></returns>
        bool GetGamepadVisible();
    }
}
