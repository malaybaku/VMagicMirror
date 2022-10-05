using UniRx;

namespace Baku.VMagicMirror
{
    public class VRMPreviewPresenter : PresenterBase
    {
        private readonly VRMPreviewCanvas _vrm0view;
        private readonly VRM10MetaViewController _vrm1view;
        private readonly VrmLoadProcessBroker _broker;
        
        public VRMPreviewPresenter(
            VRMPreviewCanvas vrm0view,
            VRM10MetaViewController vrm1view,
            VrmLoadProcessBroker broker
        )
        {
            _vrm0view = vrm0view;
            _vrm1view = vrm1view;
            _broker = broker;
        }

        public override void Initialize()
        {
            _broker.ShowVrm0MetaRequested
                .Subscribe(v => _vrm0view.Show(v.meta, v.thumbnail))
                .AddTo(this);
            _broker.ShowVrm1MetaRequested
                .Subscribe(v => _vrm1view.Show(v.meta, v.thumbnail))
                .AddTo(this);

            _broker.HideRequested
                .Subscribe(_ =>
                {
                    _vrm0view.Hide();
                    _vrm1view.Hide();
                })
                .AddTo(this);
        }
    }
}
