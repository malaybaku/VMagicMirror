namespace Baku.VMagicMirror.MediaPipeTracker
{
    public interface ILandmarkTask
    { 
        void SetTaskActive(bool isActive);
        void StopTask();
    }
    
    public interface IHandLandmarkTask : ILandmarkTask
    {
    }
    
    public interface IHandAndFaceLandmarkTask : ILandmarkTask
    {
    }
}
