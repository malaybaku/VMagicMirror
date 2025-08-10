using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> マウスボタンイベントをグローバルフックから拾ってUIスレッドで発火してくれる凄いやつだよ </summary>
    /// <remarks>
    /// summaryの説明で書いてないが、実際にはマウスフックはIPCで受け取る(WPFが実際のグローバルフックを行う)。
    /// 以前はここでキーボードのグローバルフックも拾っていたが、RawInput実装に切り替わったため廃止した。
    /// IReleaseBeforeQuitを消してもいいのだが、グローバルフックに関係ある立場なので一応残している
    /// </remarks>
    public class GlobalHookInputChecker : MonoBehaviour, IReleaseBeforeQuit, IKeyMouseEventSource
    {
        public IObservable<string> RawKeyDown { get; } = new Subject<string>();
        public IObservable<string> RawKeyUp { get; } = new Subject<string>();
        //NOTE: このクラスはKeyDown/KeyUpを発火させない(すでにダミー実装が差し込まれてしまってるため)
        public IObservable<string> KeyDown { get; } = new Subject<string>();
        public IObservable<string> KeyUp { get; } = new Subject<string>();        
        public IObservable<string> MouseButton => _mouseButton;
        private readonly Subject<string> _mouseButton = new Subject<string>();

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.MouseButton,
                c => _mouseButton.OnNext(c.GetStringValue())
                );
        }

        public void ReleaseBeforeCloseConfig()
        {
            //何もしない
        }

        public Task ReleaseResources() => Task.CompletedTask;
    }
}
