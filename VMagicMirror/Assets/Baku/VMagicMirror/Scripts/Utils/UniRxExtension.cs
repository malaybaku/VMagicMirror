using System;
using UniRx;

namespace Baku.VMagicMirror
{
    public static class UniRxExtension
    {
        public static IObservable<Unit> AsUnitWithoutLatest<T>(this IReactiveProperty<T> rp)
            => rp.SkipLatestValueOnSubscribe().AsUnitObservable();

        public static IObservable<Unit> AsUnitWithoutLatest<T>(this IReadOnlyReactiveProperty<T> rp)
            => rp.SkipLatestValueOnSubscribe().AsUnitObservable();
    }
}
