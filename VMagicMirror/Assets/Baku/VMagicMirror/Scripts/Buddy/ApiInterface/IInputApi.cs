using System;

namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface IInputApi
    {
        Vector2 MousePosition { get; }

        //TODO: enumにしたい寄り
        Action<GamepadKey> GamepadButtonDown { get; set; }
        Action<GamepadKey> GamepadButtonUp { get; set; }

        // ENTER以外のキー名は通知されない (全て "" になる) 
        Action<string> KeyboardKeyDown { get; set; }
        Action<string> KeyboardKeyUp { get; set; }

        bool GetGamepadButton(GamepadKey key);
    }

    public enum GamepadKey
    {
        LEFT,
        RIGHT,
        UP,
        DOWN,
        A,
        B,
        X,
        Y,
        RShoulder,
        LShoulder,
        //NOTE: トリガーキーも便宜的にon/offのボタン扱いする
        RTrigger,
        LTrigger,
        Start,
        Select,
        //NOTE: キーアサインの話をするときに「アサイン無し」をやりたいので定義してる
        Unknown,
    }
}

