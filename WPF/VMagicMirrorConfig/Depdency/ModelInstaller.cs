namespace Baku.VMagicMirrorConfig
{
    static class ModelInstaller
    {
        //1回だけ呼ぶことで、アプリの生存期間と同じ期間だけ生きるモデルを生成し、他から依存可能にする
        public static void Initialize()
        {
            var resolver = ModelResolver.Instance;
            var messageIo = new MessageIo();

            resolver.Add(messageIo);
            resolver.Add(messageIo.Receiver);
            resolver.Add(messageIo.Sender);

            resolver.Add(new WindowSettingModel());
            resolver.Add(new MotionSettingModel());
            resolver.Add(new LayoutSettingModel());
            resolver.Add(new GamepadSettingModel());
            resolver.Add(new LightSettingModel());
            resolver.Add(new WordToMotionSettingModel());
            resolver.Add(new ExternalTrackerSettingModel());
            resolver.Add(new AutomationSettingModel());
            resolver.Add(new AccessorySettingModel());
            resolver.Add(new RootSettingModel());

            //NOTE: 設定ファイル系の処理のモデルも必要なら入れてよい
            resolver.Add(new SettingFileIo());
            resolver.Add(new SaveFileManager());


        }
    }
}
