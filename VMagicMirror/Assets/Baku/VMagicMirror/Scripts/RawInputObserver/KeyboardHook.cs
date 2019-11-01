using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

//パクｒ…参考元: http://hongliang.seesaa.net/article/7539988.html
namespace Baku.VMagicMirror
{
    ///<summary>キーボードが操作されたときに実行されるメソッドを表すイベントハンドラ。</summary>
    public delegate void KeyboardHookedEventHandler(object sender, KeyboardHookedEventArgs e);

    ///<summary>KeyboardHookedイベントのデータを提供する。</summary>
    public class KeyboardHookedEventArgs : CancelEventArgs
    {
        ///<summary>新しいインスタンスを作成する。</summary>
        internal KeyboardHookedEventArgs(KeyboardMessage message, ref KeyboardState state)
        {
            _message = message;
            _state = state;
        }

        private readonly KeyboardMessage _message;
        private readonly KeyboardState _state;

        ///<summary>キーボードが押されたか放されたかを表す値を取得する。</summary>
        internal KeyboardUpDown UpDown
            => (_message == KeyboardMessage.KeyDown || _message == KeyboardMessage.SysKeyDown) ?
            KeyboardUpDown.Down : 
            KeyboardUpDown.Up;

        ///<summary>操作されたキーの仮想キーコードを表す値を取得する。</summary>
        public Keys KeyCode => _state.KeyCode;
        
        ///<summary>操作されたキーのスキャンコードを表す値を取得する。</summary>
        public int ScanCode => _state.ScanCode;
        
        ///<summary>操作されたキーがテンキーなどの拡張キーかどうかを表す値を取得する。</summary>
        public bool IsExtendedKey => _state.Flag.IsExtended;

        ///<summary>ALTキーが押されているかどうかを表す値を取得する。</summary>
        public bool AltDown => _state.Flag.AltDown;
    }

    ///<summary>キーボードの操作をフックし、任意のメソッドを挿入する。</summary>
    public class KeyboardHook : Component
    {
        #region DllImport

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int hookType, KeyboardHookDelegate hookDelegate, IntPtr hInstance, uint threadId);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int CallNextHookEx(IntPtr hook, int code, KeyboardMessage message, ref KeyboardState state);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        #endregion

        private delegate int KeyboardHookDelegate(int code, KeyboardMessage message, ref KeyboardState state);
        private const int KeyboardHookType = 13;
        private GCHandle hookDelegate;
        private IntPtr hook;
        private static readonly object EventKeyboardHooked = new object();

        ///<summary>キーボードが操作されたときに発生する。</summary>
        public event KeyboardHookedEventHandler KeyboardHooked
        {
            add { Events.AddHandler(EventKeyboardHooked, value); }
            remove { Events.RemoveHandler(EventKeyboardHooked, value); }
        }

        ///<summary>
        ///KeyboardHookedイベントを発生させる。
        ///</summary>
        ///<param name="e">イベントのデータ。</param>
        protected virtual void OnKeyboardHooked(KeyboardHookedEventArgs e)
        {
            (Events[EventKeyboardHooked] as KeyboardHookedEventHandler)?.Invoke(this, e);
        }

        ///<summary>
        ///新しいインスタンスを作成する。
        ///</summary>
        public KeyboardHook()
        {
            KeyboardHookDelegate callback = new KeyboardHookDelegate(CallNextHook);
            hookDelegate = GCHandle.Alloc(callback);

//            IntPtr module = Marshal.GetHINSTANCE(typeof(KeyboardHook).Assembly.GetModules()[0]);
//            hook = SetWindowsHookEx(KeyboardHookType, callback, module, 0);
            hook = SetWindowsHookEx(KeyboardHookType, callback, IntPtr.Zero, 0);
        }

        ///<summary>
        ///キーボードが操作されたときに実行するデリゲートを指定してインスタンスを作成する。
        ///</summary>
        ///<param name="handler">キーボードが操作されたときに実行するメソッドを表すイベントハンドラ。</param>
        public KeyboardHook(KeyboardHookedEventHandler handler) : this()
        {
            KeyboardHooked += handler;
        }

        private int CallNextHook(int code, KeyboardMessage message, ref KeyboardState state)
        {
            if (code >= 0)
            {
                KeyboardHookedEventArgs e = new KeyboardHookedEventArgs(message, ref state);
                OnKeyboardHooked(e);
                if (e.Cancel)
                {
                    return -1;
                }
            }
            return CallNextHookEx(IntPtr.Zero, code, message, ref state);
        }

        ///<summary>
        ///使用されているアンマネージリソースを解放し、オプションでマネージリソースも解放する。
        ///</summary>
        ///<param name="disposing">マネージリソースも解放する場合はtrue。</param>
        protected override void Dispose(bool disposing)
        {
            if (hookDelegate.IsAllocated)
            {
                UnhookWindowsHookEx(hook);
                hook = IntPtr.Zero;
                hookDelegate.Free();
            }
            base.Dispose(disposing);
        }
    }

    ///<summary>キーボードが押されているか放されているかを表す。</summary>
    internal enum KeyboardUpDown
    {
        ///<summary>キーは押されている。</summary>
        Down,
        ///<summary>キーは放されている。</summary>
        Up,
    }

    ///<summary>メッセージコードを表す。</summary>
    internal enum KeyboardMessage
    {
        ///<summary>キーが押された。</summary>
        KeyDown = 0x100,
        ///<summary>キーが放された。</summary>
        KeyUp = 0x101,
        ///<summary>システムキーが押された。</summary>
        SysKeyDown = 0x104,
        ///<summary>システムキーが放された。</summary>
        SysKeyUp = 0x105,
    }
  
    ///<summary>キーボードの状態を表す。</summary>
    internal struct KeyboardState
    {
        ///<summary>仮想キーコード。</summary>
        public Keys KeyCode;
        ///<summary>スキャンコード。</summary>
        public int ScanCode;
        ///<summary>各種特殊フラグ。</summary>
        public KeyboardStateFlag Flag;
        ///<summary>このメッセージが送られたときの時間。</summary>
        public int Time;
        ///<summary>メッセージに関連づけられた拡張情報。</summary>
        public IntPtr ExtraInfo;
    }
    
    ///<summary>キーボードの状態を補足する。</summary>
    internal struct KeyboardStateFlag
    {
        private int flag;

        private bool IsFlagging(int value)
            => (flag & value) != 0;

        private void Flag(bool value, int digit) 
            => flag = value ? (flag | digit) : (flag & ~digit);

        ///<summary>キーがテンキー上のキーのような拡張キーかどうかを表す。</summary>
        public bool IsExtended
        {
            get => IsFlagging(0x01);
            set => Flag(value, 0x01);
        }

        ///<summary>イベントがインジェクトされたかどうかを表す。</summary>
        public bool IsInjected
        {
            get => IsFlagging(0x10);
            set => Flag(value, 0x10);
        }

        ///<summary>ALTキーが押されているかどうかを表す。</summary>
        public bool AltDown
        {
            get => IsFlagging(0x20);
            set => Flag(value, 0x20);
        }

        ///<summary>キーが放されたどうかを表す。</summary>
        public bool IsUp
        {
            get => IsFlagging(0x80);
            set => Flag(value, 0x80);
        }
    }

}