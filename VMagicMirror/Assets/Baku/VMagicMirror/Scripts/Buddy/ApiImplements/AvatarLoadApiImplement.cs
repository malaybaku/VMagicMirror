using System;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// このImplを使って公開するAPIの期待挙動
    ///
    /// - アバターのロード/アンロードのコールバックを適宜呼ぶ
    /// - とくに、スクリプトの最初のロード(=start()の呼び出し)が終わった時点でアバターがロード済みの場合、ロードのコールバックを呼ぶ
    /// </summary>
    public class AvatarLoadApiImplement
    {
        private readonly IVRMLoadable _vrmLoadable;

        [Inject]
        public AvatarLoadApiImplement(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
            
            // ちょっと遅めにしとく
            _vrmLoadable.PostVrmLoaded += _ =>
            {
                _loaded.OnNext(Unit.Default);
                IsLoaded = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                IsLoaded = false;
                _unloaded.OnNext(Unit.Default);
            };
        }

        private readonly Subject<Unit> _loaded = new();
        private readonly Subject<Unit> _unloaded = new();
        public IObservable<Unit> Loaded => _loaded;
        public IObservable<Unit> Unloaded => _unloaded;

        public bool IsLoaded { get; private set; }
    }
}
