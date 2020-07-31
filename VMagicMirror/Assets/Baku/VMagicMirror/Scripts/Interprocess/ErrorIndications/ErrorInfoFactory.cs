using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// エラー情報の一部を多言語化してくれるやつ
    /// </summary>
    public class ErrorInfoFactory
    {
        [Inject]
        public ErrorInfoFactory(VRMPreviewLanguage language)
        {
            _language = language;
        }

        private readonly VRMPreviewLanguage _language;

        public string LoadVrmErrorTitle() 
        {
            switch (_language.Language)
            {
                case VRMPreviewLanguage.Japanese:
                    return "VRMのロード中にエラーが発生しました";
                case VRMPreviewLanguage.English:
                default:
                    return "Error happened when loading VRM";
            }
        }

        public string LoadVrmErrorContentPrefix() 
        {
            switch (_language.Language)
            {
                case VRMPreviewLanguage.Japanese:
                    return
                        "繰り返しエラーが発生し、原因が不明な場合は製作者までお問い合わせ下さい。\n" +
                        "以下のエラー詳細情報は'log.txt'ファイルにも保存されます。\n\n";
                case VRMPreviewLanguage.English:
                default:
                    return 
                        "Please contact to the author if the error continues to happen and the cause is unclear.\n" +
                        "The error message below is also recorded to the file 'log.txt.'\n\n";
            }
        }
    }
}
