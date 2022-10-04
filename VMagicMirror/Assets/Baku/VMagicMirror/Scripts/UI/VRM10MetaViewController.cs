using UniGLTF.Extensions.VRMC_vrm;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VRM1.0用のメタ情報を表示するUI
    /// </summary>
    public class VRM10MetaViewController : PresenterBase
    {
        private readonly IFactory<VRM10MetaView> _viewFactory;
        private readonly VRMPreviewLanguage _previewLanguage;
        private VRM10MetaView _view = null;

        [Inject]
        public VRM10MetaViewController(VRMPreviewLanguage previewLanguage, IFactory<VRM10MetaView> viewFactory)
        {
            _viewFactory = viewFactory;
            _previewLanguage = previewLanguage;
        }
        
        public override void Initialize()
        {
            //何も無いかも
        }

        public void Show(Meta metaData, Texture2D thumbnail)
        {
            if (_view == null)
            {
                _view = _viewFactory.Create();
            }

            _view.SetLocale(LanguageNameToLocale(_previewLanguage.Language));
            _view.SetMeta(metaData);
            _view.SetThumbnail(thumbnail);
            _view.SetActive(true);
        }

        public void Hide()
        {
            if (_view != null)
            {
                _view.SetActive(false);
            }
            
            //TODO: ここで実施してもよければサムネを破棄したい
            
        }
        
        private static PreviewUILocale LanguageNameToLocale(string languageName)
        {
            switch (languageName)
            {
                case "Japanese":
                    return PreviewUILocale.Japanese;
                //case "English":
                default:
                    return PreviewUILocale.English;
            }
        }
    }
}
