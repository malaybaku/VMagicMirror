//System.Windows.Formsで定義されているキー名の一覧
//実装手法がグローバルフックのため、キー一覧としてはコレを使う必要があります
//(WPFとか別のフレームワークのキー列挙体だと数字の割り当て方が異なる)

namespace System.Windows.Forms
{
    [Flags]
    public enum _Obsolete_Keys
    {
        //
        // 概要:
        //     The bitmask to extract modifiers from a key value.
        Modifiers = -65536,
        //
        // 概要:
        //     No key pressed.
        None = 0,
        //
        // 概要:
        //     The left mouse button.
        LButton = 1,
        //
        // 概要:
        //     The right mouse button.
        RButton = 2,
        //
        // 概要:
        //     The CANCEL key.
        Cancel = 3,
        //
        // 概要:
        //     The middle mouse button (three-button mouse).
        MButton = 4,
        //
        // 概要:
        //     The first x mouse button (five-button mouse).
        XButton1 = 5,
        //
        // 概要:
        //     The second x mouse button (five-button mouse).
        XButton2 = 6,
        //
        // 概要:
        //     The BACKSPACE key.
        Back = 8,
        //
        // 概要:
        //     The TAB key.
        Tab = 9,
        //
        // 概要:
        //     The LINEFEED key.
        LineFeed = 10,
        //
        // 概要:
        //     The CLEAR key.
        Clear = 12,
        //
        // 概要:
        //     The RETURN key.
        Return = 13,
        //
        // 概要:
        //     The ENTER key.
        Enter = 13,
        //
        // 概要:
        //     The SHIFT key.
        ShiftKey = 16,
        //
        // 概要:
        //     The CTRL key.
        ControlKey = 17,
        //
        // 概要:
        //     The ALT key.
        Menu = 18,
        //
        // 概要:
        //     The PAUSE key.
        Pause = 19,
        //
        // 概要:
        //     The CAPS LOCK key.
        Capital = 20,
        //
        // 概要:
        //     The CAPS LOCK key.
        CapsLock = 20,
        //
        // 概要:
        //     The IME Kana mode key.
        KanaMode = 21,
        //
        // 概要:
        //     The IME Hanguel mode key. (maintained for compatibility; use HangulMode)
        HanguelMode = 21,
        //
        // 概要:
        //     The IME Hangul mode key.
        HangulMode = 21,
        //
        // 概要:
        //     The IME Junja mode key.
        JunjaMode = 23,
        //
        // 概要:
        //     The IME final mode key.
        FinalMode = 24,
        //
        // 概要:
        //     The IME Hanja mode key.
        HanjaMode = 25,
        //
        // 概要:
        //     The IME Kanji mode key.
        KanjiMode = 25,
        //
        // 概要:
        //     The ESC key.
        Escape = 27,
        //
        // 概要:
        //     The IME convert key.
        IMEConvert = 28,
        //
        // 概要:
        //     The IME nonconvert key.
        IMENonconvert = 29,
        //
        // 概要:
        //     The IME accept key, replaces System.Windows.Forms.Keys.IMEAceept.
        IMEAccept = 30,
        //
        // 概要:
        //     The IME accept key. Obsolete, use System.Windows.Forms.Keys.IMEAccept instead.
        IMEAceept = 30,
        //
        // 概要:
        //     The IME mode change key.
        IMEModeChange = 31,
        //
        // 概要:
        //     The SPACEBAR key.
        Space = 32,
        //
        // 概要:
        //     The PAGE UP key.
        Prior = 33,
        //
        // 概要:
        //     The PAGE UP key.
        PageUp = 33,
        //
        // 概要:
        //     The PAGE DOWN key.
        Next = 34,
        //
        // 概要:
        //     The PAGE DOWN key.
        PageDown = 34,
        //
        // 概要:
        //     The END key.
        End = 35,
        //
        // 概要:
        //     The HOME key.
        Home = 36,
        //
        // 概要:
        //     The LEFT ARROW key.
        Left = 37,
        //
        // 概要:
        //     The UP ARROW key.
        Up = 38,
        //
        // 概要:
        //     The RIGHT ARROW key.
        Right = 39,
        //
        // 概要:
        //     The DOWN ARROW key.
        Down = 40,
        //
        // 概要:
        //     The SELECT key.
        Select = 41,
        //
        // 概要:
        //     The PRINT key.
        Print = 42,
        //
        // 概要:
        //     The EXECUTE key.
        Execute = 43,
        //
        // 概要:
        //     The PRINT SCREEN key.
        Snapshot = 44,
        //
        // 概要:
        //     The PRINT SCREEN key.
        PrintScreen = 44,
        //
        // 概要:
        //     The INS key.
        Insert = 45,
        //
        // 概要:
        //     The DEL key.
        Delete = 46,
        //
        // 概要:
        //     The HELP key.
        Help = 47,
        //
        // 概要:
        //     The 0 key.
        D0 = 48,
        //
        // 概要:
        //     The 1 key.
        D1 = 49,
        //
        // 概要:
        //     The 2 key.
        D2 = 50,
        //
        // 概要:
        //     The 3 key.
        D3 = 51,
        //
        // 概要:
        //     The 4 key.
        D4 = 52,
        //
        // 概要:
        //     The 5 key.
        D5 = 53,
        //
        // 概要:
        //     The 6 key.
        D6 = 54,
        //
        // 概要:
        //     The 7 key.
        D7 = 55,
        //
        // 概要:
        //     The 8 key.
        D8 = 56,
        //
        // 概要:
        //     The 9 key.
        D9 = 57,
        //
        // 概要:
        //     The A key.
        A = 65,
        //
        // 概要:
        //     The B key.
        B = 66,
        //
        // 概要:
        //     The C key.
        C = 67,
        //
        // 概要:
        //     The D key.
        D = 68,
        //
        // 概要:
        //     The E key.
        E = 69,
        //
        // 概要:
        //     The F key.
        F = 70,
        //
        // 概要:
        //     The G key.
        G = 71,
        //
        // 概要:
        //     The H key.
        H = 72,
        //
        // 概要:
        //     The I key.
        I = 73,
        //
        // 概要:
        //     The J key.
        J = 74,
        //
        // 概要:
        //     The K key.
        K = 75,
        //
        // 概要:
        //     The L key.
        L = 76,
        //
        // 概要:
        //     The M key.
        M = 77,
        //
        // 概要:
        //     The N key.
        N = 78,
        //
        // 概要:
        //     The O key.
        O = 79,
        //
        // 概要:
        //     The P key.
        P = 80,
        //
        // 概要:
        //     The Q key.
        Q = 81,
        //
        // 概要:
        //     The R key.
        R = 82,
        //
        // 概要:
        //     The S key.
        S = 83,
        //
        // 概要:
        //     The T key.
        T = 84,
        //
        // 概要:
        //     The U key.
        U = 85,
        //
        // 概要:
        //     The V key.
        V = 86,
        //
        // 概要:
        //     The W key.
        W = 87,
        //
        // 概要:
        //     The X key.
        X = 88,
        //
        // 概要:
        //     The Y key.
        Y = 89,
        //
        // 概要:
        //     The Z key.
        Z = 90,
        //
        // 概要:
        //     The left Windows logo key (Microsoft Natural Keyboard).
        LWin = 91,
        //
        // 概要:
        //     The right Windows logo key (Microsoft Natural Keyboard).
        RWin = 92,
        //
        // 概要:
        //     The application key (Microsoft Natural Keyboard).
        Apps = 93,
        //
        // 概要:
        //     The computer sleep key.
        Sleep = 95,
        //
        // 概要:
        //     The 0 key on the numeric keypad.
        NumPad0 = 96,
        //
        // 概要:
        //     The 1 key on the numeric keypad.
        NumPad1 = 97,
        //
        // 概要:
        //     The 2 key on the numeric keypad.
        NumPad2 = 98,
        //
        // 概要:
        //     The 3 key on the numeric keypad.
        NumPad3 = 99,
        //
        // 概要:
        //     The 4 key on the numeric keypad.
        NumPad4 = 100,
        //
        // 概要:
        //     The 5 key on the numeric keypad.
        NumPad5 = 101,
        //
        // 概要:
        //     The 6 key on the numeric keypad.
        NumPad6 = 102,
        //
        // 概要:
        //     The 7 key on the numeric keypad.
        NumPad7 = 103,
        //
        // 概要:
        //     The 8 key on the numeric keypad.
        NumPad8 = 104,
        //
        // 概要:
        //     The 9 key on the numeric keypad.
        NumPad9 = 105,
        //
        // 概要:
        //     The multiply key.
        Multiply = 106,
        //
        // 概要:
        //     The add key.
        Add = 107,
        //
        // 概要:
        //     The separator key.
        Separator = 108,
        //
        // 概要:
        //     The subtract key.
        Subtract = 109,
        //
        // 概要:
        //     The decimal key.
        Decimal = 110,
        //
        // 概要:
        //     The divide key.
        Divide = 111,
        //
        // 概要:
        //     The F1 key.
        F1 = 112,
        //
        // 概要:
        //     The F2 key.
        F2 = 113,
        //
        // 概要:
        //     The F3 key.
        F3 = 114,
        //
        // 概要:
        //     The F4 key.
        F4 = 115,
        //
        // 概要:
        //     The F5 key.
        F5 = 116,
        //
        // 概要:
        //     The F6 key.
        F6 = 117,
        //
        // 概要:
        //     The F7 key.
        F7 = 118,
        //
        // 概要:
        //     The F8 key.
        F8 = 119,
        //
        // 概要:
        //     The F9 key.
        F9 = 120,
        //
        // 概要:
        //     The F10 key.
        F10 = 121,
        //
        // 概要:
        //     The F11 key.
        F11 = 122,
        //
        // 概要:
        //     The F12 key.
        F12 = 123,
        //
        // 概要:
        //     The F13 key.
        F13 = 124,
        //
        // 概要:
        //     The F14 key.
        F14 = 125,
        //
        // 概要:
        //     The F15 key.
        F15 = 126,
        //
        // 概要:
        //     The F16 key.
        F16 = 127,
        //
        // 概要:
        //     The F17 key.
        F17 = 128,
        //
        // 概要:
        //     The F18 key.
        F18 = 129,
        //
        // 概要:
        //     The F19 key.
        F19 = 130,
        //
        // 概要:
        //     The F20 key.
        F20 = 131,
        //
        // 概要:
        //     The F21 key.
        F21 = 132,
        //
        // 概要:
        //     The F22 key.
        F22 = 133,
        //
        // 概要:
        //     The F23 key.
        F23 = 134,
        //
        // 概要:
        //     The F24 key.
        F24 = 135,
        //
        // 概要:
        //     The NUM LOCK key.
        NumLock = 144,
        //
        // 概要:
        //     The SCROLL LOCK key.
        Scroll = 145,
        //
        // 概要:
        //     The left SHIFT key.
        LShiftKey = 160,
        //
        // 概要:
        //     The right SHIFT key.
        RShiftKey = 161,
        //
        // 概要:
        //     The left CTRL key.
        LControlKey = 162,
        //
        // 概要:
        //     The right CTRL key.
        RControlKey = 163,
        //
        // 概要:
        //     The left ALT key.
        LMenu = 164,
        //
        // 概要:
        //     The right ALT key.
        RMenu = 165,
        //
        // 概要:
        //     The browser back key (Windows 2000 or later).
        BrowserBack = 166,
        //
        // 概要:
        //     The browser forward key (Windows 2000 or later).
        BrowserForward = 167,
        //
        // 概要:
        //     The browser refresh key (Windows 2000 or later).
        BrowserRefresh = 168,
        //
        // 概要:
        //     The browser stop key (Windows 2000 or later).
        BrowserStop = 169,
        //
        // 概要:
        //     The browser search key (Windows 2000 or later).
        BrowserSearch = 170,
        //
        // 概要:
        //     The browser favorites key (Windows 2000 or later).
        BrowserFavorites = 171,
        //
        // 概要:
        //     The browser home key (Windows 2000 or later).
        BrowserHome = 172,
        //
        // 概要:
        //     The volume mute key (Windows 2000 or later).
        VolumeMute = 173,
        //
        // 概要:
        //     The volume down key (Windows 2000 or later).
        VolumeDown = 174,
        //
        // 概要:
        //     The volume up key (Windows 2000 or later).
        VolumeUp = 175,
        //
        // 概要:
        //     The media next track key (Windows 2000 or later).
        MediaNextTrack = 176,
        //
        // 概要:
        //     The media previous track key (Windows 2000 or later).
        MediaPreviousTrack = 177,
        //
        // 概要:
        //     The media Stop key (Windows 2000 or later).
        MediaStop = 178,
        //
        // 概要:
        //     The media play pause key (Windows 2000 or later).
        MediaPlayPause = 179,
        //
        // 概要:
        //     The launch mail key (Windows 2000 or later).
        LaunchMail = 180,
        //
        // 概要:
        //     The select media key (Windows 2000 or later).
        SelectMedia = 181,
        //
        // 概要:
        //     The start application one key (Windows 2000 or later).
        LaunchApplication1 = 182,
        //
        // 概要:
        //     The start application two key (Windows 2000 or later).
        LaunchApplication2 = 183,
        //
        // 概要:
        //     The OEM Semicolon key on a US standard keyboard (Windows 2000 or later).
        OemSemicolon = 186,
        //
        // 概要:
        //     The OEM 1 key.
        Oem1 = 186,
        //
        // 概要:
        //     The OEM plus key on any country/region keyboard (Windows 2000 or later).
        Oemplus = 187,
        //
        // 概要:
        //     The OEM comma key on any country/region keyboard (Windows 2000 or later).
        Oemcomma = 188,
        //
        // 概要:
        //     The OEM minus key on any country/region keyboard (Windows 2000 or later).
        OemMinus = 189,
        //
        // 概要:
        //     The OEM period key on any country/region keyboard (Windows 2000 or later).
        OemPeriod = 190,
        //
        // 概要:
        //     The OEM question mark key on a US standard keyboard (Windows 2000 or later).
        OemQuestion = 191,
        //
        // 概要:
        //     The OEM 2 key.
        Oem2 = 191,
        //
        // 概要:
        //     The OEM tilde key on a US standard keyboard (Windows 2000 or later).
        Oemtilde = 192,
        //
        // 概要:
        //     The OEM 3 key.
        Oem3 = 192,
        //
        // 概要:
        //     The OEM open bracket key on a US standard keyboard (Windows 2000 or later).
        OemOpenBrackets = 219,
        //
        // 概要:
        //     The OEM 4 key.
        Oem4 = 219,
        //
        // 概要:
        //     The OEM pipe key on a US standard keyboard (Windows 2000 or later).
        OemPipe = 220,
        //
        // 概要:
        //     The OEM 5 key.
        Oem5 = 220,
        //
        // 概要:
        //     The OEM close bracket key on a US standard keyboard (Windows 2000 or later).
        OemCloseBrackets = 221,
        //
        // 概要:
        //     The OEM 6 key.
        Oem6 = 221,
        //
        // 概要:
        //     The OEM singled/double quote key on a US standard keyboard (Windows 2000 or later).
        OemQuotes = 222,
        //
        // 概要:
        //     The OEM 7 key.
        Oem7 = 222,
        //
        // 概要:
        //     The OEM 8 key.
        Oem8 = 223,
        //
        // 概要:
        //     The OEM angle bracket or backslash key on the RT 102 key keyboard (Windows 2000
        //     or later).
        OemBackslash = 226,
        //
        // 概要:
        //     The OEM 102 key.
        Oem102 = 226,
        //
        // 概要:
        //     The PROCESS KEY key.
        ProcessKey = 229,
        //
        // 概要:
        //     Used to pass Unicode characters as if they were keystrokes. The Packet key value
        //     is the low word of a 32-bit virtual-key value used for non-keyboard input methods.
        Packet = 231,
        //
        // 概要:
        //     The ATTN key.
        Attn = 246,
        //
        // 概要:
        //     The CRSEL key.
        Crsel = 247,
        //
        // 概要:
        //     The EXSEL key.
        Exsel = 248,
        //
        // 概要:
        //     The ERASE EOF key.
        EraseEof = 249,
        //
        // 概要:
        //     The PLAY key.
        Play = 250,
        //
        // 概要:
        //     The ZOOM key.
        Zoom = 251,
        //
        // 概要:
        //     A constant reserved for future use.
        NoName = 252,
        //
        // 概要:
        //     The PA1 key.
        Pa1 = 253,
        //
        // 概要:
        //     The CLEAR key.
        OemClear = 254,
        //
        // 概要:
        //     The bitmask to extract a key code from a key value.
        KeyCode = 65535,
        //
        // 概要:
        //     The SHIFT modifier key.
        Shift = 65536,
        //
        // 概要:
        //     The CTRL modifier key.
        Control = 131072,
        //
        // 概要:
        //     The ALT modifier key.
        Alt = 262144
    }
}