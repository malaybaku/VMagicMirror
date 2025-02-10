using System;
using UniRx;

namespace Baku.VMagicMirror.Buddy.Api
{
    //NOTE: このAPIレイヤーは直接Luaに露出する使い方はせず、コールバック関数を呼ぶときに使いたい
    public class InputApi
    {
        public IObservable<string> OnKeyboardDown => onKeyboardKeyDown;
        private readonly Subject<string> onKeyboardKeyDown = new();
        
        public IObservable<string> OnKeyboardUp => onKeyboardKeyUp;
        private readonly Subject<string> onKeyboardKeyUp = new();

        public IObservable<string> OnGamepadButtonDown => onGamepadButtonDown;
        private readonly Subject<string> onGamepadButtonDown = new();
        
        public IObservable<string> OnGamepadButtonUp => onGamepadButtonUp;
        private readonly Subject<string> onGamepadButtonUp = new();

        public void InvokeOnKeyboardDown(string key) 
            => FeatureLocker.InvokeWithFeatureLock(() => onKeyboardKeyDown.OnNext(key));
        public void InvokeOnKeyboardUp(string key)
            => FeatureLocker.InvokeWithFeatureLock(() => onKeyboardKeyUp.OnNext(key));

        public void InvokeOnGamepadButtonDown(string key) 
            => FeatureLocker.InvokeWithFeatureLock(() => onGamepadButtonDown.OnNext(key));
        public void InvokeOnGamepadButtonUp(string key)
            => FeatureLocker.InvokeWithFeatureLock(() => onGamepadButtonUp.OnNext(key));
        
        
    }
 }