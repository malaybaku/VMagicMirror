using System.Text;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> VMagicMirrorのインストールフォルダに全角文字が入ってないか調べるすごいやつだよ </summary>
    static class InstallPathChecker
    {
        public static bool HasMultiByteCharInInstallPath()
        {
            string source = SpecialFilePath.UnityAppPath;
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            //シンプルに、バイト数が文字数より上回る→どれかの文字がマルチバイトだからダメ、という判断
            return Encoding.UTF8.GetBytes(source).Length > source.Length;
        }
    }
}
