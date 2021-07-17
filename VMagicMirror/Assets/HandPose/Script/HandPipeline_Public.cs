using UnityEngine;

namespace MediaPipe.HandPose {

//
// Public part of the hand pipeline class
//

partial class HandPipeline
{
    #region Detection data accessors

    public const int KeyPointCount = 21;

    public enum KeyPoint
    {
        //0
        Wrist,
        //1
        Thumb1,  Thumb2,  Thumb3,  Thumb4,
        //5
        Index1,  Index2,  Index3,  Index4,
        //9
        Middle1, Middle2, Middle3, Middle4,
        //13
        Ring1,   Ring2,   Ring3,   Ring4,
        //17
        Pinky1,  Pinky2,  Pinky3,  Pinky4
    }

    public Vector3 GetKeyPoint(KeyPoint point)
      => ReadCache[(int)point];

    public Vector3 GetKeyPoint(int index)
      => ReadCache[index];

    #endregion

    #region GPU-side resource accessors

    public ComputeBuffer KeyPointBuffer
      => _buffer.filter;

    public ComputeBuffer HandRegionBuffer
      => _buffer.region;

    public ComputeBuffer HandRegionCropBuffer
      => _buffer.crop;

    #endregion

    #region Public properties and methods

    public bool UseAsyncReadback { get; set; } = true;

    //Get confidence of landmark detection
    public float Score => _detector.landmark?.Score ?? -1f;

    public HandPipeline(ResourceSet resources)
      => AllocateObjects(resources);

    public void Dispose()
      => DeallocateObjects();

    public void ProcessImage(Texture image)
      => RunPipeline(image);

    public void DetectPalm(Texture image, float dt)
      => RunDetectPalm(image, dt);

    public void CalculateLandmarks(Texture image, float dt)
      => RunCalculateLandmarks(image, dt);
    
    #endregion
}

} // namespace MediaPipe.HandPose
