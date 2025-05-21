namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// RootApiから使うAPI実装クラスをDI時に集めて保持するだけのクラス
    /// </summary>
    public class ApiImplementBundle
    {
        public ApiImplementBundle(
            LanguageSettingRepository languageSettingRepository,
            BuddySettingsRepository settingsRepository,
            BuddyPropertyRepository buddyPropertyRepository,
            BuddyPropertyActionBroker buddyPropertyActionBroker,
            BuddyAudioEventBroker buddyAudioEventBroker,
            BuddyLogger logger,
            AvatarLoadApiImplement loadApi,
            AvatarFacialApiImplement facialApi,
            AvatarPoseApiImplement poseApi,
            AvatarMotionEventApiImplement motionEventApi,
            DeviceLayoutApiImplement deviceLayoutApi,
            InputApiImplement rawInputApi,
            WordToMotionEventApiImplement wordToMotionEventApi,
            AudioApiImplement audioApi,
            ScreenApiImplement screenApi,
            Buddy3DInstanceCreator buddy3DInstanceCreator,
            BuddySpriteCanvas buddySpriteCanvas,
            BuddyGuiCanvas buddyGuiCanvas
        )
        {
            LanguageSettingRepository = languageSettingRepository;
            SettingsRepository = settingsRepository;
            Logger = logger;
            BuddyPropertyRepository = buddyPropertyRepository;
            BuddyPropertyActionBroker = buddyPropertyActionBroker;
            BuddyAudioEventBroker = buddyAudioEventBroker;
            AvatarLoadApi = loadApi;
            AvatarFacialApi = facialApi;
            AvatarPoseApi = poseApi;
            AvatarMotionEventApi = motionEventApi;
            DeviceLayoutApi = deviceLayoutApi;
            InputApi = rawInputApi;
            WordToMotionEventApi = wordToMotionEventApi;
            AudioApi = audioApi;
            ScreenApi = screenApi;
            Buddy3DInstanceCreator = buddy3DInstanceCreator;
            BuddySpriteCanvas = buddySpriteCanvas;
            BuddyGuiCanvas = buddyGuiCanvas;
        }
        
        public LanguageSettingRepository LanguageSettingRepository { get; }
        public BuddySettingsRepository SettingsRepository { get; }
        public BuddyLogger Logger { get; }
        public BuddyPropertyRepository BuddyPropertyRepository { get; }
        public BuddyPropertyActionBroker BuddyPropertyActionBroker { get; }
        public BuddyAudioEventBroker BuddyAudioEventBroker { get; }
        public AvatarLoadApiImplement AvatarLoadApi { get; }
        public AvatarFacialApiImplement AvatarFacialApi { get; }
        public AvatarPoseApiImplement AvatarPoseApi { get; }
        public AvatarMotionEventApiImplement AvatarMotionEventApi { get; }
        public DeviceLayoutApiImplement DeviceLayoutApi { get; }
        public InputApiImplement InputApi { get; }
        public WordToMotionEventApiImplement WordToMotionEventApi { get; }
        public AudioApiImplement AudioApi { get; }
        public ScreenApiImplement ScreenApi { get; }
        
        // NOTE: コレと同列の扱いでSpriteCanvasとかも置きたい
        public Buddy3DInstanceCreator Buddy3DInstanceCreator { get; }
        public BuddySpriteCanvas BuddySpriteCanvas { get; }
        public BuddyGuiCanvas BuddyGuiCanvas { get; }
    }
}
