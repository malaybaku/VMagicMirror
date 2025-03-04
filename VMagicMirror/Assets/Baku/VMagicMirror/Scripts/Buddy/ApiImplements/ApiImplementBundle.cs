namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// RootApiから使うAPI実装クラスをDI時に集めて保持するだけのクラス
    /// </summary>
    public class ApiImplementBundle
    {
        public ApiImplementBundle(
            BuddyPropertyRepository buddyPropertyRepository,
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
            BuddyGuiCanvas buddyGuiCanvas
        )
        {
            BuddyPropertyRepository = buddyPropertyRepository;
            AvatarLoadApi = loadApi;
            AvatarFacialApi = facialApi;
            AvatarPoseApi = poseApi;
            AvatarMotionEventApi = motionEventApi;
            DeviceLayoutApi = deviceLayoutApi;
            RawInputApi = rawInputApi;
            WordToMotionEventApi = wordToMotionEventApi;
            AudioApi = audioApi;
            ScreenApi = screenApi;
            Buddy3DInstanceCreator = buddy3DInstanceCreator;
            BuddyGuiCanvas = buddyGuiCanvas;
        }
        
        public BuddyPropertyRepository BuddyPropertyRepository { get; }
        public AvatarLoadApiImplement AvatarLoadApi { get; }
        public AvatarFacialApiImplement AvatarFacialApi { get; }
        public AvatarPoseApiImplement AvatarPoseApi { get; }
        public AvatarMotionEventApiImplement AvatarMotionEventApi { get; }
        public DeviceLayoutApiImplement DeviceLayoutApi { get; }
        public InputApiImplement RawInputApi { get; }
        public WordToMotionEventApiImplement WordToMotionEventApi { get; }
        public AudioApiImplement AudioApi { get; }
        public ScreenApiImplement ScreenApi { get; }
        
        // NOTE: コレと同列の扱いでSpriteCanvasとかも置きたい
        public Buddy3DInstanceCreator Buddy3DInstanceCreator { get; }
        public BuddyGuiCanvas BuddyGuiCanvas { get; }
    }
}
