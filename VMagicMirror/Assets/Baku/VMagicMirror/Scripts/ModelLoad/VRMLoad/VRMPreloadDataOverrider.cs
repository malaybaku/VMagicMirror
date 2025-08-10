using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// プリロード対象のVRMがあるときにロード要求を発火させたり、
    /// 通常のファイル/VRoidHubからのロードを無視したりすることを要求するクラス
    /// </summary>
    public class VRMPreloadDataOverrider : PresenterBase
    {
        public readonly struct PreloadDataLoadRequest
        {
            public PreloadDataLoadRequest(byte[] data)
            {
                Data = data;
            }

            public byte[] Data { get; }
        }
        
        private readonly IMessageReceiver _receiver;
        private readonly CancellationTokenSource _cts = new();

        [Inject]
        public VRMPreloadDataOverrider(
            IMessageReceiver receiver,
            [InjectOptional] IVRMPreloadData preloadData)
        {
            _receiver = receiver;
            PreloadData = preloadData ?? new EmptyVRMPreloadData();
            ShouldIgnoreNonPreloadData = PreloadData.HasData;
        }

        public IVRMPreloadData PreloadData { get; }
        public bool ShouldIgnoreNonPreloadData { get; private set; }

        private readonly Subject<PreloadDataLoadRequest> _loadRequested = new();
        public IObservable<PreloadDataLoadRequest> LoadRequested => _loadRequested;
        
        public override void Initialize()
        {
            // NOTE: このSubscribeは起動直後よりも後で発火するぶんを処理する
            PreloadData.ReloadRequested
                .Subscribe(_ =>
                {
                    var request = new PreloadDataLoadRequest(PreloadData.GetData());
                    _loadRequested.OnNext(request);
                })
                .AddTo(this);

            // ここから下では起動時にすでにロード要求があった場合を処理する
            if (!PreloadData.HasData)
            {
                return;
            }

            // WPFの起動時に投げられたVRMのロードリクエストは無視する(プリロードデータのほうを優先する)。その後にモデルを交代するのはOK
            _receiver.AssignCommandHandler(
                VmmCommands.StartupEnded,
                _ => ShouldIgnoreNonPreloadData = false
            );

            // NOTE: 遅延をつけるのは、GUIからのリクエストでモデルをロードするときの挙動にちょっと近づけるため
            RequestLoadAsync(_cts.Token).Forget();
        }

        public override void Dispose()
        {
            base.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }

        private async UniTaskVoid RequestLoadAsync(CancellationToken ct)
        {
            await UniTask.NextFrame(ct);
            _loadRequested.OnNext(new PreloadDataLoadRequest(PreloadData.GetData()));
        }
    }
}
