using R3;

namespace Baku.VMagicMirror
{
    public static class UniRxExtension
    {
        public static Observable<Unit> AsUnitWithoutLatest<T>(this ReactiveProperty<T> rp)
            => rp.Skip(1).AsUnitObservable();

        public static Observable<Unit> AsUnitWithoutLatest<T>(this ReadOnlyReactiveProperty<T> rp)
            => rp.Skip(1).AsUnitObservable();
    }
}
