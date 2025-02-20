using System;
using System.Collections.Generic;
using System.Threading;
using Baku.VMagicMirror.Buddy.Api;
using Cysharp.Threading.Tasks;
using NLua;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    public readonly struct BuddyLuaCallbackItem
    {
        /// <summary>
        /// NOTE: 引数が2つ以下のときにobject[]を生成するのをケチって避けておく
        /// </summary>
        public enum ArgCountType
        {
            Zero,
            One,
            Two,
            Multiple,
        }
        
        public LuaFunction Function { get; }

        public ArgCountType ArgType { get; }
        /// <summary> ArgTypeがOneかTwoのときに値が入っている </summary>
        public object Arg0 { get; }
        /// <summary> ArgTypeがTwoのときに値が入っている </summary>
        public object Arg1 { get; }
        /// <summary> ArgTypeがMultipleの場合、全ての引数がこの配列に入っている </summary>
        public object[] Args { get; }

        private BuddyLuaCallbackItem(
            LuaFunction func, ArgCountType argType, object arg0, object arg1, object[] args
            )
        {
            Function = func;
            ArgType = argType;
            Arg0 = arg0;
            Arg1 = arg1;
            Args = args;
        }

        public static BuddyLuaCallbackItem NoArg(LuaFunction func) 
            => new(func, ArgCountType.Zero, null, null, Array.Empty<object>());

        public static BuddyLuaCallbackItem OneArg(LuaFunction func, object arg)
            => new(func, ArgCountType.One, arg, null, Array.Empty<object>());

        public static BuddyLuaCallbackItem TwoArg(LuaFunction func, object arg0, object arg1)
            => new(func, ArgCountType.Two, arg0, arg1, Array.Empty<object>());

        public static BuddyLuaCallbackItem MultipleArg(LuaFunction func, object[] args)
            => new(func, ArgCountType.Multiple, null, null, args);
    }
    
    /// <summary>
    /// VMMの内部的なイベントを監視して、スクリプトから登録されたコールバック関数の呼び出しにつなげて呼び出すクラス。
    /// <see cref="ScriptCaller"/>と同じライフサイクルで動くのが期待値
    /// </summary>
    public class ScriptEventInvoker : PresenterBase
    {
        private readonly RootApi _api;

        private readonly AvatarLoadApiImplement _avatarLoad;
        private readonly AvatarMotionEventApiImplement _avatarMotionEvent;
        private readonly AvatarFacialApiImplement _avatarFacial;

        private readonly Queue<BuddyLuaCallbackItem> _callbackQueue = new();
        private readonly CancellationTokenSource _cts = new();

        private readonly object[] _oneArgCache = new object[1];
        private readonly object[] _twoArgCache = new object[2];
        
        public ScriptEventInvoker(
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
        
        //NOTE: 下記のInitializeと本クラスのDisposeはScriptCallerから明示的に呼ばれるのが期待値
        public override void Initialize()
        {
            //NOTE: 「コールバックが未登録ならSubscribeもしないでおく」というスタイルもあるかも
            // (Buddyが増えたときにSubscribeの総量が絞りやすい)
         
            //TODO: Unloadが遅延発火することあるの難しいな… / Loadedは遅延しても害が少ないから良い気がするけども
            ConnectNoArgFunc(_avatarLoad.Loaded, () => _api.AvatarLoadEvent.Loaded);
            ConnectNoArgFunc(_avatarLoad.Unloaded, () => _api.AvatarLoadEvent.UnLoaded);
            
            ConnectOneArgFunc(
                _avatarMotionEvent.KeyboardKeyDown,
                () => _api.AvatarMotionEvent.OnKeyboardKeyDown
                );

            ConnectNoArgFunc(
                _avatarMotionEvent.TouchPadMouseButtonDown,
                () => _api.AvatarMotionEvent.OnTouchPadMouseButtonDown
            );
            ConnectNoArgFunc(
                _avatarMotionEvent.PenTabletMouseButtonDown,
                () => _api.AvatarMotionEvent.OnPenTabletMouseButtonDown
            );
            
            ConnectOneArgFunc(
                _avatarMotionEvent.GamepadButtonDown,
                () => _api.AvatarMotionEvent.OnGamepadButtonDown,
                v => v.Item2
            );
            ConnectOneArgFunc(
                _avatarMotionEvent.ArcadeStickButtonDown,
                () => _api.AvatarMotionEvent.OnArcadeStickButtonDown
            );
            
            ConnectNoArgFunc(
                _avatarFacial.Blinked,
                () => _api.AvatarFacial.OnBlinked
                );
            
            InvokeCallbackAsync(_cts.Token).Forget();
        }

        public override void Dispose()
        {
            base.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }

        private async UniTaskVoid InvokeCallbackAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: cancellationToken);
                while (_callbackQueue.TryDequeue(out var item))
                {
                    Invoke(item);
                }
            }
        }

        private void Invoke(BuddyLuaCallbackItem item)
        {
            switch (item.ArgType)
            {
                case BuddyLuaCallbackItem.ArgCountType.Zero:
                    ApiUtils.Try(() => item.Function.Call());
                    break;
                case BuddyLuaCallbackItem.ArgCountType.One:
                    _oneArgCache[0] = item.Arg0;
                    ApiUtils.Try(() => item.Function.Call(_oneArgCache));
                    break;
                case BuddyLuaCallbackItem.ArgCountType.Two:
                    _twoArgCache[0] = item.Arg0;
                    _twoArgCache[1] = item.Arg1;
                    ApiUtils.Try(() => item.Function.Call(_twoArgCache));
                    break;
                case BuddyLuaCallbackItem.ArgCountType.Multiple:
                    ApiUtils.Try(() => item.Function.Call(item.Args));
                    break;
            }
        }
        
        private void ConnectNoArgFunc(IObservable<Unit> source, Func<LuaFunction> funcGetter)
        {
            source.Subscribe(v =>
                {
                    var func = funcGetter();
                    if (func == null)
                    {
                        return;
                    }
                    _callbackQueue.Enqueue(BuddyLuaCallbackItem.NoArg(func));
                })
                .AddTo(this);
        }

        private void ConnectOneArgFunc<T>(IObservable<T> source, Func<LuaFunction> funcGetter)
            => ConnectOneArgFunc(source, funcGetter, v => v);

        private void ConnectOneArgFunc<T>(
            IObservable<T> source, Func<LuaFunction> funcGetter, Func<T, object> argConverter)
        {
            source.Subscribe(v =>
                {
                    var func = funcGetter();
                    if (func == null)
                    {
                        return;
                    }
                    var arg = argConverter(v);
                    _callbackQueue.Enqueue(BuddyLuaCallbackItem.OneArg(func, arg));
                })
                .AddTo(this);
        }

        private void ConnectTwoArgFunc<T>(
            IObservable<T> source, Func<LuaFunction> funcGetter, Func<T, (object, object)> argConverter)
        {
            source.Subscribe(v =>
                {
                    var func = funcGetter();
                    if (func == null)
                    {
                        return;
                    }
                    var args = argConverter(v);
                    _callbackQueue.Enqueue(BuddyLuaCallbackItem.TwoArg(func, args.Item1, args.Item2));
                })
                .AddTo(this);
        }

        private void ConnectMultipleArgFunc<T>(
            IObservable<T> source, Func<LuaFunction> funcGetter, Func<T, object[]> argConverter)
        {
            source.Subscribe(v =>
                {
                    var func = funcGetter();
                    if (func == null)
                    {
                        return;
                    }
                    var args = argConverter(v);
                    _callbackQueue.Enqueue(BuddyLuaCallbackItem.MultipleArg(func, args));
                })
                .AddTo(this);
        }

    }
}
