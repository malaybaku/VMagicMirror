using System;
using System.Collections.Generic;
using System.Threading;
using Baku.VMagicMirror.Buddy.Api;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{ 
    /// <summary>
    /// VMMの内部的なイベントを監視して、スクリプトから登録されたコールバック関数の呼び出しにつなげて呼び出すクラス。
    /// <see cref="ScriptCallerCSharp"/>と同じライフサイクルで動くのが期待値
    /// </summary>
    public class ScriptEventInvokerCSharp : PresenterBase
    {
        private readonly RootApi _api;

        private readonly AvatarLoadApiImplement _avatarLoad;
        private readonly AvatarMotionEventApiImplement _avatarMotionEvent;
        private readonly AvatarFacialApiImplement _avatarFacial;
        private readonly BuddySpriteUpdater _spriteUpdater = new();

        private readonly Queue<Action> _callbackQueue = new();
        private readonly CancellationTokenSource _cts = new();

        private bool _startCalled;
        
        public ScriptEventInvokerCSharp(
            RootApi api,
            AvatarLoadApiImplement avatarLoad,
            AvatarMotionEventApiImplement avatarMotionEvent,
            AvatarFacialApiImplement avatarFacial
            )
        {
            _api = api;
            _avatarLoad = avatarLoad;
            _avatarMotionEvent = avatarMotionEvent;
            _avatarFacial = avatarFacial;
        }
        
        // NOTE: まだC#版は検証段階なので、一部のイベントにのみ対応している
        public override void Initialize()
        {
            ConnectNoArgFunc(_avatarLoad.Loaded, () => _api.AvatarLoadEvent.Loaded);
            ConnectNoArgFunc(_avatarLoad.Unloaded, () => _api.AvatarLoadEvent.UnLoaded);

            ConnectOneArgFunc(
                _avatarMotionEvent.KeyboardKeyDown,
                () => _api.AvatarMotionEvent.OnKeyboardKeyDown
                );

            // ConnectOneArgFunc(
            //     _avatarMotionEvent.KeyboardKeyDown,
            //     () => _api.AvatarMotionEvent.OnKeyboardKeyDown
            //     );
            //
            // ConnectNoArgFunc(
            //     _avatarMotionEvent.TouchPadMouseButtonDown,
            //     () => _api.AvatarMotionEvent.OnTouchPadMouseButtonDown
            // );
            // ConnectNoArgFunc(
            //     _avatarMotionEvent.PenTabletMouseButtonDown,
            //     () => _api.AvatarMotionEvent.OnPenTabletMouseButtonDown
            // );
            //
            // ConnectOneArgFunc(
            //     _avatarMotionEvent.GamepadButtonDown,
            //     () => _api.AvatarMotionEvent.OnGamepadButtonDown,
            //     v => v.Item2
            // );
            // ConnectOneArgFunc(
            //     _avatarMotionEvent.ArcadeStickButtonDown,
            //     () => _api.AvatarMotionEvent.OnArcadeStickButtonDown
            // );
            
            ConnectNoArgFunc(_avatarFacial.Blinked, () => _api.AvatarFacial.OnBlinked);
            
            InvokeCallbackAsync(_cts.Token).Forget();
        }

        public override void Dispose()
        {
            base.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }

        //TODO: スクリプトのロード時点でアバターがロード済みの場合、Loadedを発火させたい
        private async UniTaskVoid InvokeCallbackAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: cancellationToken);
                
                // Start/Updateもここで呼ぶことにより、アバター等の状態が完全に確定した状態でのみスクリプトが実行されるのを保証する
                InvokeStartIfNeeded();
                ApiUtils.Try(_api.BuddyId, () => _api.Update?.Invoke(Time.deltaTime));

                while (_callbackQueue.TryDequeue(out var callback))
                {
                    ApiUtils.Try(_api.BuddyId, () => callback());
                }
                
                // 最後の最後でスプライトの状態を更新する
                UpdateSprites();
            }
        }

        private void UpdateSprites()
        {
            foreach (var sprite in _api.Sprites)
            {
                _spriteUpdater.UpdateSprite(sprite);
            }
        }
        
        private void InvokeStartIfNeeded()
        {
            if (!_startCalled)
            {
                _startCalled = true;
                ApiUtils.Try(_api.BuddyId, () => _api.Start?.Invoke());
            }
        }
        
        private void ConnectNoArgFunc(IObservable<Unit> source, Func<Action> funcGetter)
        {
            source.Subscribe(v =>
                {
                    var func = funcGetter();
                    if (func == null)
                    {
                        return;
                    }
                    _callbackQueue.Enqueue(() => funcGetter()?.Invoke());
                })
                .AddTo(this);
        }

        // TSource == TArg であり、値の変換も不要なケース
        private void ConnectOneArgFunc<TSource>(IObservable<TSource> source, Func<Action<TSource>> funcGetter)
            => ConnectOneArgFunc(source, funcGetter, v => v);

        private void ConnectOneArgFunc<TSource, TArg>(
            IObservable<TSource> source, Func<Action<TArg>> funcGetter, Func<TSource, TArg> argConverter)
        {
            source.Subscribe(v =>
                {
                    var func = funcGetter();
                    if (func == null)
                    {
                        return;
                    }
                    var arg = argConverter(v);
                    _callbackQueue.Enqueue(() => func.Invoke(arg));
                })
                .AddTo(this);
        }

        private void ConnectTwoArgFunc<TSource, TArg0, TArg1>(
            IObservable<TSource> source,
            Func<Action<TArg0, TArg1>> funcGetter,
            Func<TSource, (TArg0, TArg1)> argConverter)
        {
            source.Subscribe(v =>
                {
                    var func = funcGetter();
                    if (func == null)
                    {
                        return;
                    }
                    var args = argConverter(v);
                    _callbackQueue.Enqueue(() => func.Invoke(args.Item1, args.Item2));
                })
                .AddTo(this);
        }

        // TODO: Genericで書きづらいので一旦コメントアウトで… (せいぜい4引数くらいまでで切り上げたい)
        // private void ConnectMultipleArgFunc<T>(
        //     IObservable<T> source, Func<LuaFunction> funcGetter, Func<T, object[]> argConverter)
        // {
        //     source.Subscribe(v =>
        //         {
        //             var func = funcGetter();
        //             if (func == null)
        //             {
        //                 return;
        //             }
        //             var args = argConverter(v);
        //             _callbackQueue.Enqueue(BuddyLuaCallbackItem.MultipleArg(func, args));
        //         })
        //         .AddTo(this);
        // }
    }
}
