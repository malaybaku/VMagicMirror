namespace Baku.VMagicMirrorConfig
{
    static class ModelInstaller
    {
        //1回だけ呼ぶことで、アプリの生存期間と同じ期間だけ生きるモデルを生成し、他から依存可能にする
        //内容
        // 1. Sender/Receiverと関係ないもの
        // 2. Sender/Receiver
        // 3. Sender/Receiverに依存するが、Settingには依存しないもの
        // 4. Setting
        // 5. Setting (と、場合によってはSender/Receiverにも)に依存するもの
        public static void Initialize()
        {
            var resolver = ModelResolver.Instance;
            var messageIo = new MessageIo();

            resolver.Add(new InstallPathChecker());
            resolver.Add(new AppQuitSetting());

            resolver.Add(messageIo);
            resolver.Add(messageIo.Receiver);
            resolver.Add(messageIo.Sender);

            resolver.Add(new LoadedAvatarInfo());
            resolver.Add(new ScreenshotTaker());

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

            resolver.Add(new AvatarLoader());

            resolver.Add(new RuntimeHelper());
            resolver.Add(new DeviceListSource());
            resolver.Add(new CustomMotionList());
            resolver.Add(new ImageQualitySetting());
            resolver.Add(new WordToMotionRuntimeConfig());
            resolver.Add(new ExternalTrackerRuntimeConfig());
            resolver.Add(new LargePointerVisibility());
            resolver.Add(new MicrophoneStatus());
        }
    }
}
