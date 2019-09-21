using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 頭のIKというかLookAtIk的なところを制御するやつ
    /// </summary>
    public class HeadIkIntegrator : MonoBehaviour
    {
        private const string UseLookAtPointNone = nameof(UseLookAtPointNone);
        private const string UseLookAtPointMousePointer = nameof(UseLookAtPointMousePointer);
        private const string UseLookAtPointMainCamera = nameof(UseLookAtPointMainCamera);
        //手のIKよりLookAtのIKをやや前方にずらして見栄えを調整する決め打ちのパラメータ
        private const float ZOffsetOnHandIk = 0.3f;

        [SerializeField] private Transform cam = null;
        [SerializeField] private Transform lookAtTarget = null;
        [SerializeField] private float lookAtSpeedFactor = 6.0f;
        
        private readonly IKDataRecord _mouseBasedLookAt = new IKDataRecord();
        public IIKGenerator MouseBasedLookAt => _mouseBasedLookAt;

        private readonly CameraBasedLookAtIk _camBasedLookAt = new CameraBasedLookAtIk();
        public IIKGenerator CameraBasedLookAt => _camBasedLookAt;

        //TODO: ここに画像ベースでLateUpdate時に首曲げるやつを持ってきたほうがよさそう

        private LookAtStyles _lookAtStyle = LookAtStyles.MousePointer;
        private Transform _head = null;
        
        public void OnVrmLoaded(VrmLoadedInfo info)
        {
            _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
        }

        public void OnVrmDisposing()
        {
            _head = null;
        }
        
        public void MoveMouse(int x, int y)
        {
            //画面中央 = カメラ位置なのでコレで空間的にだいたい正しいハズ
            float xClamped = Mathf.Clamp(x - Screen.width * 0.5f, -1000, 1000) / 1000.0f;
            float yClamped = Mathf.Clamp(y - Screen.height * 0.5f, -1000, 1000) / 1000.0f;

            _mouseBasedLookAt.Position =
                cam.TransformPoint(xClamped, yClamped, 0) + 
                ZOffsetOnHandIk * Vector3.forward;
        }
        
        public void SetLookAtStyle(string content)
        {
            _lookAtStyle =
                (content == UseLookAtPointNone) ? LookAtStyles.Fixed :
                (content == UseLookAtPointMousePointer) ? LookAtStyles.MousePointer :
                (content == UseLookAtPointMainCamera) ? LookAtStyles.MainCamera :
                LookAtStyles.MousePointer;
        }

        private void Start()
        {
            _camBasedLookAt.Camera = cam;
        }

        private void Update()
        {
            Vector3 pos = 
                (_lookAtStyle == LookAtStyles.MousePointer) ? _mouseBasedLookAt.Position :
                (_lookAtStyle == LookAtStyles.MainCamera) ? _camBasedLookAt.Position :
                (_head != null) ? _head.position + _head.forward * 1.0f : 
                new Vector3(1, 0, 1);


            lookAtTarget.localPosition = Vector3.Lerp(
                lookAtTarget.localPosition,
                pos,
                lookAtSpeedFactor * Time.deltaTime
            );
        }

        class CameraBasedLookAtIk : IIKGenerator
        {
            public Transform Camera { get; set; }

            public Vector3 Position => Camera.position;
                
            public Quaternion Rotation => Camera.rotation;

            public IKTargets Target => IKTargets.HeadLookAt;
        }

        enum LookAtStyles
        {
            Fixed,
            MousePointer,
            MainCamera,
        }
    }
}
