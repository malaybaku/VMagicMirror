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
            CameraApiImplement cameraApi,
            DeviceLayoutApiImplement deviceLayoutApi,
            RawInputApiImplement rawInputApi,
            WordToMotionEventApiImplement wordToMotionEventApi
        )
        {
            BuddyPropertyRepository = buddyPropertyRepository;
            LoadApi = loadApi;
            FacialApi = facialApi;
            PoseApi = poseApi;
            MotionEventApi = motionEventApi;
            CameraApi = cameraApi;
            DeviceLayoutApi = deviceLayoutApi;
            RawInputApi = rawInputApi;
            WordToMotionEventApi = wordToMotionEventApi;
        }
        
        // SpriteCanvasとかもここに帰着できる方が体裁が良いかも
        public BuddyPropertyRepository BuddyPropertyRepository { get; }
        public AvatarLoadApiImplement LoadApi { get; }
        public AvatarFacialApiImplement FacialApi { get; }
        public AvatarPoseApiImplement PoseApi { get; }
        public AvatarMotionEventApiImplement MotionEventApi { get; }
        public CameraApiImplement CameraApi { get; }
        public DeviceLayoutApiImplement DeviceLayoutApi { get; }
        public RawInputApiImplement RawInputApi { get; }
        public WordToMotionEventApiImplement WordToMotionEventApi { get; }

    }
}
