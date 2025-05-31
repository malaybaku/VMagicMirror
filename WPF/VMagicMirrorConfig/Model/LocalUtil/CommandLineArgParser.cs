using System;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> コマンドライン引数のうちVMagicMirrorで興味のある値を引っ張ってくるやつ </summary>
    static class CommandLineArgParser
    {
        /// <summary> 
        /// コマンドライン引数で、空ではないMemoryMappedFileが指定されていればそれを返します。
        /// </summary>
        /// <param name="result">結果のMemoryMappedFile名称。送受信に関わる"_receiver"とか"_sender"のsuffixはついてない状態です。</param>
        /// <returns></returns>
        public static bool TryLoadMmfFileName(out string result)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "/channelId")
                {
                    result = args[i + 1];
                    return !string.IsNullOrEmpty(result);
                }
            }
            result = "";
            return false;
        }

        /// <summary>
        /// NOTE: v4.0.0でエディタ環境でのデバッグ目的で使うようになった引数。
        /// Unity側がビルドされている場合はこの引数は取得できない。
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetUnityStreamingAssetsPath(out string result)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "/streamingAssetsDir")
                {
                    result = args[i + 1];
                    return !string.IsNullOrEmpty(result);
                }
            }
            result = "";
            return false;
        }
    }
}
