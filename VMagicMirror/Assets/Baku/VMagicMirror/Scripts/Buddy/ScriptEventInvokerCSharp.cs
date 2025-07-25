using System;
using System.Collections.Generic;
using System.Threading;
using Baku.VMagicMirror.Buddy.Api;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{ 
    // NOTE: このクラス自体はアバター出力イベントの監視をしない。個別の ApiImplement 系のクラスがイベントの発火のon/offを制御している
    /// <summary>
    /// VMMの内部的なイベントを監視して、スクリプトから登録されたコールバック関数の呼び出しにつなげて呼び出すクラス。
    /// <see cref="ScriptCallerCSharp"/>と同じライフサイクルで動くのが期待値
    /// </summary>
    public class ScriptEventInvokerCSharp : PresenterBase
    {
        private readonly RootApi _api;
        private readonly BuddyRuntimeObjectRepository _runtimeObjectRepository;
        private readonly ApiImplementBundle _apiImplements;
        private readonly BuddySprite2DUpdater _spriteUpdater;
        private readonly BuddyLogger _logger;

        private readonly Queue<Action> _callbackQueue = new();
        private readonly CancellationTokenSource _cts = new();
            
        private bool _startCalled;
        
        [Inject]
        public ScriptEventInvokerCSharp(
            RootApi api,
            ApiImplementBundle apiImplements,
            BuddySprite2DUpdater spriteUpdater,
            BuddyRuntimeObjectRepository runtimeObjectRepository
            )
        {
            _api = api;
            _apiImplements = apiImplements;
            _logger = apiImplements.Logger;
            _spriteUpdater = spriteUpdater;
            _runtimeObjectRepository = runtimeObjectRepository;
        }
        
        public override void Initialize()
        {
            ConnectNoArgFunc(
                _apiImplements.AvatarLoadApi.Loaded, 
                () => _api.AvatarLoadEventInternal.InvokeLoadedInternal
            );
            ConnectNoArgFunc(
                _apiImplements.AvatarLoadApi.Unloaded, 
                () => _api.AvatarLoadEventInternal.InvokeUnloadedInternal
            );

            ConnectOneArgFunc(
                _apiImplements.AvatarMotionEventApi.KeyboardKeyDown,
                () => _api.AvatarMotionEventInternal.InvokeOnKeyboardKeyDownInternal
            );
            
            ConnectNoArgFunc(
                _apiImplements.AvatarMotionEventApi.TouchPadMouseButtonDown,
                () => _api.AvatarMotionEventInternal.InvokeOnTouchPadMouseButtonDownInternal
            );
            ConnectNoArgFunc(
                _apiImplements.AvatarMotionEventApi.PenTabletMouseButtonDown,
                () => _api.AvatarMotionEventInternal.InvokeOnPenTabletMouseButtonDownInternal
            );
            
            ConnectOneArgFunc(
                _apiImplements.AvatarMotionEventApi.GamepadButtonDown,
                () => _api.AvatarMotionEventInternal.InvokeOnGamepadButtonDownInternal,
                v => v.Item2
            );

            ConnectOneArgFunc(
                _apiImplements.AvatarMotionEventApi.ArcadeStickButtonDown,
                () => _api.AvatarMotionEventInternal.InvokeOnArcadeStickButtonDownInternal
            );
            
            ConnectOneArgFunc(
                _apiImplements.InputApi.OnKeyboardKeyDown,
                () => _api.InputInternal.InvokeKeyboardKeyDown
            );
            ConnectOneArgFunc(
                _apiImplements.InputApi.OnKeyboardKeyUp,
                () => _api.InputInternal.InvokeKeyboardKeyUp
            );
            
            ConnectOneArgFunc(
                _apiImplements.InputApi.GamepadButtonDown,
                () => _api.InputInternal.InvokeGamepadButtonDown
            );
            ConnectOneArgFunc(
                _apiImplements.InputApi.GamepadButtonUp,
                () => _api.InputInternal.InvokeGamepadButtonUp
            );
            
            ConnectNoArgFunc(
                _apiImplements.AvatarFacialApi.Blinked,
                () => _api.AvatarFacialInternal.InvokeOnBlinkedInternal
            );
            
            // NOTE: このへんはBuddyIdでフィルタして「このサブキャラ用のイベントだけ持ってきたIO<T>」になってる
            ConnectOneArgFunc(
                _apiImplements.BuddyPropertyActionBroker.ActionRequestedForBuddy(_api.BuddyId),
                () => _api.PropertyInternal.InvokeActionInternal
                );
            ConnectOneArgFunc(
                _apiImplements.BuddyAudioEventBroker.AudioStartedForBuddy(_api.BuddyId),
                () => _api.AudioInternal.InvokeAudioStarted
            );
            ConnectOneArgFunc(
                _apiImplements.BuddyAudioEventBroker.AudioStoppedForBuddy(_api.BuddyId),
                () => _api.AudioInternal.InvokeAudioStopped
            );
            
            ConnectOneArgFunc(
                _apiImplements.BuddySpriteEventBroker.OnPointerEnterForBuddy(_api.BuddyId),
                src => src.api.InvokePointerEnter,
                src => src.data
            );
            ConnectOneArgFunc(
                _apiImplements.BuddySpriteEventBroker.OnPointerLeaveForBuddy(_api.BuddyId),
                src => src.api.InvokePointerLeave,
                src => src.data
            );
            ConnectOneArgFunc(
                _apiImplements.BuddySpriteEventBroker.OnPointerDownForBuddy(_api.BuddyId),
                src => src.api.InvokePointerDown,
                src => src.data
            );
            ConnectOneArgFunc(
                _apiImplements.BuddySpriteEventBroker.OnPointerUpForBuddy(_api.BuddyId),
                src => src.api.InvokePointerUp,
                src => src.data
            );
            ConnectOneArgFunc(
                _apiImplements.BuddySpriteEventBroker.OnPointerClickForBuddy(_api.BuddyId),
                src => src.api.InvokePointerClick,
                src => src.data
            );
            
            ConnectOneArgFunc(
                _apiImplements.BuddyTalkTextEventBroker.ItemDequeued(_api.BuddyId),
                src => src.api.InvokeItemDequeued,
                src => src.item
            );
            ConnectOneArgFunc(
                _apiImplements.BuddyTalkTextEventBroker.ItemFinished(_api.BuddyId),
                src => src.api.InvokeItemFinished,
                src => src.item
            );
            
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
                InvokeStartEventsIfNeeded();
                ApiUtils.Try(_api.BuddyFolder, _logger, () => _api.InvokeUpdated(Time.deltaTime));

                while (_callbackQueue.TryDequeue(out var callback))
                {
                    ApiUtils.Try(_api.BuddyFolder, _logger, () => callback());
                }
                
                // 最後の最後でスプライトの状態を更新する
                UpdateInstances();
            }
        }

        private void UpdateInstances()
        {
            if (!_runtimeObjectRepository.TryGet(_api.BuddyId, out var repo))
            {
                // NOTE: サブキャラが1つもspriteやオブジェクトを生成していない場合に加えて、
                // (なるべく通過しないでほしいが) サブキャラのDispose後のタイミング次第で通る可能性もある
                return;
            }
            
            foreach (var sprite in repo.Sprite2Ds)
            {
                _spriteUpdater.UpdateSprite(sprite);
            }

            foreach (var sprite3d in repo.Sprite3Ds)
            {
                // NOTE: この辺も条件が複雑になったらUpdaterを分けた方がヨサソウ
                sprite3d.DoTransition(Time.deltaTime);
            }

            foreach (var vrm in repo.Vrms)
            {
                // Sprite3Dに同じ
                vrm.UpdateInstance();
            }
        }
        
        private void InvokeStartEventsIfNeeded()
        {
            if (_startCalled)
            {
                return;
            }

            _startCalled = true;
            ApiUtils.Try(_api.BuddyFolder, _logger, () => _api.InvokeStarted());

            // NOTE: 書いてる通りだが、Scriptの起動時にすでにアバターがロード済みだった場合、明示的にロードイベントのコールバックを呼ぶ。
            // ノリは MonoBehaviour.OnEnable に少し似てるが、Startより後で発火することには注意
            if (_api.AvatarLoadEventInternal.IsLoaded)
            {
                ApiUtils.Try(_api.BuddyFolder, _logger, () => _api.AvatarLoadEventInternal.InvokeLoadedInternal());
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

        // TSource == TArg であり、値の変換も不要なケース。なるべくこれに帰着するのがシンプルで望ましい
        private void ConnectOneArgFunc<TSource>(IObservable<TSource> source, Func<Action<TSource>> funcGetter)
            => ConnectOneArgFunc(source, funcGetter, v => v);

        // TSourceがApi用の型ではないので変換が必要なケース
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

        // IO<T>のTの部分が(TCaller, TArg)のタプルになっているケース。動的生成したインスタンスのイベントハンドラを扱うのに使う
        private void ConnectOneArgFunc<TSource, TArg>(
            IObservable<TSource> source, Func<TSource, Action<TArg>> funcGetter, Func<TSource, TArg> argConverter)
        {
            source.Subscribe(v =>
                {
                    var func = funcGetter(v);
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
