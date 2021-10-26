using System;
using System.Diagnostics;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// URLをブラウザで開く処理のラッパー
    /// </summary>
    public static class UrlNavigate
    {
        public static void Open(string url)
        {
            try
            {
                //NOTE: 既定ブラウザをわざわざ指定しないでWindowsに任せる
                Process.Start(new ProcessStartInfo()
                {
                    FileName = url,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
    }
}
