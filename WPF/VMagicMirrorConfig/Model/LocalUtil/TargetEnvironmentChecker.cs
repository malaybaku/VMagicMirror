namespace Baku.VMagicMirrorConfig.Model
{
    public static class TargetEnvironmentChecker
    {
        public static bool CheckDevEnvFlagEnabled()
        {
#if DEV_ENV
            //DEV_ENV フラグは、dev系のpublish profileでビルドすると定義される
            return true;
#else
            return false;
#endif

        }

        public static bool CheckIsDebugEnv()
        {
#if DEV_ENV
            //DEV_ENV フラグは、dev系のpublish profileでビルドすると定義される
            return true;
#else
            //Unityからパイプ情報が渡されてない = Unity側がエディタ実行であると考えられる時、デバッグ実行と判断できる
            return !CommandLineArgParser.TryLoadMmfFileName(out _);
#endif
        }
    }
}
