using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class VRMPreviewPresenter : PresenterBase
    {
        private readonly VRMPreviewCanvas _vrm0view;
        private readonly VRM10MetaViewController _vrm1view;
        private readonly VrmLoadProcessBroker _broker;
        private Texture2D _copiedThumbnail = null;

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
                .Subscribe(v =>
                {
                    GetCopiedThumbnail(v.thumbnail);
                    _vrm0view.Show(v.meta, _copiedThumbnail);
                })
                .AddTo(this);
            _broker.ShowVrm1MetaRequested
                .Subscribe(v =>
                {
                    GetCopiedThumbnail(v.thumbnail);
                    _vrm1view.Show(v.meta, _copiedThumbnail);
                })
                .AddTo(this);

            _broker.HideRequested
                .Subscribe(_ =>
                {
                    _vrm0view.Hide();
                    _vrm1view.Hide();
                    if (_copiedThumbnail != null)
                    {
                        Object.Destroy(_copiedThumbnail);
                    }
                    _copiedThumbnail = null;
                })
                .AddTo(this);
        }

        private void GetCopiedThumbnail(Texture2D thumbnail)
        {
            if (_copiedThumbnail != null)
            {
                Object.Destroy(_copiedThumbnail);
                _copiedThumbnail = null;
            }

            //元画像が無いのでコピーも無しのままでよい
            if (thumbnail == null)
            {
                return;
            }

            _copiedThumbnail = new Texture2D(thumbnail.width, thumbnail.height, thumbnail.format, false);
            Graphics.CopyTexture(thumbnail, _copiedThumbnail);
        }
    }
}
