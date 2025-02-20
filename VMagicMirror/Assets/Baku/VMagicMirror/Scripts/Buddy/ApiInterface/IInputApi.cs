using System;

namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface IInputApi
    {
        Vector2 MousePosition { get; }

        //TODO: enumにしたい寄り
        Action<ApiGamepadKey> GamepadButtonDown { get; set; }
        Action<ApiGamepadKey> GamepadButtonUp { get; set; }

        // ENTER以外のキー名は通知されない (全て "" になる) 
        Action<string> KeyboardKeyDown { get; set; }
        Action<string> KeyboardKeyUp { get; set; }

        bool GetGamepadButton(ApiGamepadKey key);
    }

    public enum ApiGamepadKey
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

