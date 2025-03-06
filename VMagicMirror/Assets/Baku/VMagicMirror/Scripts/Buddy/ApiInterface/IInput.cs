using System;

namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// キーボード、マウス、ゲームパッドなどの入力状態を取得できるAPIです。
    /// </summary>
    public interface IInput
    {
        /// <summary>
        /// マウスポインターの現在の位置を、画面座標を基準として取得します。
        /// </summary>
        /// <remarks>
        /// <para>
        ///  x成分は左端が0、右端が1を表すような値です。
        ///  y成分は下端が0、上端が1を表すような値です。
        ///  値が0より小さいか、または1より大きい場合、マウスポインターがウィンドウの外側にあることを表します。
        /// </para>
        /// </remarks>
        Vector2 MousePosition { get; }
        
        /// <summary> ゲームパッドのボタンが押されると発火します。 </summary>
        event Action<GamepadButton> GamepadButtonDown;

        /// <summary> ゲームパッドのボタンが離されると発火します。 </summary>
        event Action<GamepadButton> GamepadButtonUp;

        /// <summary>
        /// ゲームパッドの左スティックの値を取得します。
        /// </summary>
        /// <remarks>
        /// x成分は左が <c>-1</c>、右が <c>1</c> に対応します。
        /// y成分は下が <c>-1</c>、上が <c>1</c> に対応します。
        /// 取得できるベクトルの <see cref="Vector2.magnitude"/> は1以下の値になります。
        /// </remarks>
        Vector2 GamepadLeftStick { get; }

        /// <summary>
        /// ゲームパッドの右スティックの値を取得します。
        /// </summary>
        /// <remarks>
        /// x成分は左が <c>-1</c>、右が <c>1</c> に対応します。
        /// y成分は下が <c>-1</c>、上が <c>1</c> に対応します。
        /// 取得できるベクトルの <see cref="Vector2.magnitude"/> は1以下の値になります。
        /// </remarks>
        Vector2 GamepadRightStick { get; }
        
        // ENTER以外のキー名は通知されない (全て "" になる) 

        /// <summary>
        /// キーボードのキーを押し下げると発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// キー名はENTERキーの打鍵時に <c>"Enter"</c> を引数とします。
        /// それ以外のキーについては、打鍵時してもキー名は特定できず、空文字列が引数となります。
        /// </para>
        /// <para>
        /// ユーザーが打鍵をランダム表示するオプションを有効にしている場合、常に空文字列が引数になります。
        /// </para>
        /// </remarks>
        event Action<string> KeyboardKeyDown;
        
        /// <summary>
        /// キーボードのキーを離すと発火します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// キー名はENTERキーの打鍵時に <c>"Enter"</c> を引数とします。
        /// それ以外のキーについては、打鍵時してもキー名は特定できず、空文字列が引数となります。
        /// </para>
        /// <para>
        /// ユーザーが打鍵をランダム表示するオプションを有効にしている場合、常に空文字列が引数になります。
        /// </para>
        /// </remarks>
        event Action<string> KeyboardKeyUp;

        /// <summary>
        /// 指定したゲームパッドのボタンを押しているかどうかを取得します。
        /// </summary>
        /// <param name="key">確認したいゲームパッドのボタン</param>
        /// <returns>指定したボタンを現在押している状態であれば <c>true</c>、そうでなければ <c>false</c> </returns>
        bool GetGamepadButton(GamepadButton key);
    }

    /// <summary>
    /// ゲームパッドのボタンです。
    /// </summary>
    public enum GamepadButton
    {
        /// <summary> 矢印キーの左 </summary>
        Left,
        /// <summary> 矢印キーの右 </summary>
        Right,
        /// <summary> 矢印キーの上 </summary>
        Up,
        /// <summary> 矢印キーの下 </summary>
        Down,
        /// <summary> Aボタン </summary>
        A,
        /// <summary> Bボタン </summary>
        B,
        /// <summary> Xボタン </summary>
        X,
        /// <summary> Yボタン </summary>
        Y,
        /// <summary> 右人差し指で押せるRBボタン </summary>
        RButton,
        /// <summary> 左人差し指で押せるLBボタン </summary>
        LButton,
        /// <summary> 右中指で押せるRTボタン </summary>
        /// <remarks> トリガーについては、一定以上握り込むことでボタンを押したものとして扱います。 </remarks>
        RTrigger,
        /// <summary> 左中指で押せるRTボタン </summary>
        /// <remarks> トリガーについては、一定以上握り込むことでボタンを押したものとして扱います。 </remarks>
        LTrigger,
        /// <summary> ゲームパッド中央付近で、右側にあるViewボタン </summary>
        View,
        /// <summary> ゲームパッド中央付近の左側にあるMenuボタン </summary>
        Select,
        /// <summary> ボタンが不明な場合に使用されますが、通常は使用されない値です。</summary>
        Unknown,
    }
}

