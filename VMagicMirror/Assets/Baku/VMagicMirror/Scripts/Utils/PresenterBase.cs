using System;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public abstract class PresenterBase : IInitializable, IDisposable
    {
        private readonly CompositeDisposable disposables = new();

        public abstract void Initialize();

        public virtual void Dispose() => disposables.Dispose();

        public void AddToDisposable(IDisposable disposable) => disposables.Add(disposable);
    }

    public static class PresenterBaseObservableExtensions
    {
        public static void AddTo(this IDisposable disposable, PresenterBase target)
        {
            target.AddToDisposable(disposable);
        }
    }
}
