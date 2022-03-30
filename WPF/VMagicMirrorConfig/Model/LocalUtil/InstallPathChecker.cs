using System.Text;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> VMagicMirrorのインストールフォルダに全角文字が入ってないか調べるすごいやつだよ </summary>
    class InstallPathChecker
    {
        private bool _initialized;

        private bool _hasMultiByteCharInInstallPath;
        public bool HasMultiByteCharInInstallPath
        {
            get
            {
                if (!_initialized)
                {
                    Initialize();
                }
                return _hasMultiByteCharInInstallPath;
            }
        }

        private void Initialize()
        {
            _initialized = true;
            string source = SpecialFilePath.UnityAppPath;
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            //シンプルに、バイト数が文字数より上回る→どれかの文字がマルチバイトだからダメ、という判断
            _hasMultiByteCharInInstallPath = Encoding.UTF8.GetBytes(source).Length > source.Length;
        }
    }
}
