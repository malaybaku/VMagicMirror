using System;
using R3;

namespace Baku.VMagicMirror
{
    public static class UniRxExtension
    {
        public static IObservable<Unit> AsUnitWithoutLatest<T>(this ReactiveProperty<T> rp)
            => rp.SkipLatestValueOnSubscribe().AsUnitObservable();

        public static IObservable<Unit> AsUnitWithoutLatest<T>(this ReadOnlyReactiveProperty<T> rp)
            => rp.SkipLatestValueOnSubscribe().AsUnitObservable();
    }
}
