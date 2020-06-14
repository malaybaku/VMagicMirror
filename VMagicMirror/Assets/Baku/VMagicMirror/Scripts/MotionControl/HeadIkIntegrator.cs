using UnityEngine;
using Zenject;

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
        private const float ZOffsetOnHeadIk = 0.6f;

        [SerializeField] private Transform cam = null;
        [SerializeField] private Transform lookAtTarget = null;
        [SerializeField] private float lookAtSpeedFactor = 6.0f;
        
        private readonly IKDataRecord _mouseBasedLookAt = new IKDataRecord();
        public IIKGenerator MouseBasedLookAt => _mouseBasedLookAt;

        private readonly CameraBasedLookAtIk _camBasedLookAt = new CameraBasedLookAtIk();
        public IIKGenerator CameraBasedLookAt => _camBasedLookAt;
        
        private LookAtStyles _lookAtStyle = LookAtStyles.MousePointer;
        private Transform _head = null;
        private bool _hasModel = false;

        private FaceControlConfiguration _faceControlConfig;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, FaceControlConfiguration faceControlConfig)
        {
            _faceControlConfig = faceControlConfig;
            vrmLoadable.VrmLoaded += info =>
            {
                _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
                _hasModel = true;
            };
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _head = null;
            };
        }
        
        
        public void MoveMouse(int x, int y)
        {
            //画面中央 = カメラ位置なのでコレで空間的にだいたい正しいハズ
            float xClamped = Mathf.Clamp(x - Screen.width * 0.5f, -1000, 1000) / 1000.0f;
            float yClamped = Mathf.Clamp(y - Screen.height * 0.5f, -1000, 1000) / 1000.0f;
            var baseLookAtPosition =
                cam.TransformPoint(xClamped, yClamped, 0) + 
                ZOffsetOnHeadIk * Vector3.forward;

            //Zの決め方に注意: キャラを正面から見ているときと後ろから見ているときで、手前にLookAtさせるか奥にLookAtさせるかを変更
            var camForward = cam.forward;
            var horizontalCamForward = new Vector3(camForward.x, 0, camForward.z).normalized;
            
            //zの値が小さい = カメラは真横、または後ろを向いている = キャラを正面から見ているハズ
            if (horizontalCamForward.z < 0.1f)
            {
                _mouseBasedLookAt.Position = baseLookAtPosition;
                return;
            }
            
            //カメラのZ成分が増える(=真後ろから見る)のに近づくにつれて奥側を向かせるようにする
            float depthFactor = 1.0f;
            if (horizontalCamForward.z < 0.5f)
            {
                depthFactor = (horizontalCamForward.z - 0.1f) * 2.5f;
            }
            
            //キャラを背後から映してるハズ: 奥行き方向にLookAtをずらしていく
            var camPosition = cam.position;
            //Vector3.Dotのとこ = カメラからみてキャラが立ってる位置の奥行き。Yを考慮すると面倒なことになるため、XZ平面でやってます
            float depth = (2 * depthFactor) * Mathf.Abs(Vector3.Dot(
                new Vector3(camPosition.x, 0, camPosition.z), horizontalCamForward
                ));
            _mouseBasedLookAt.Position = baseLookAtPosition + depth * camForward;
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
            //起動後にマウスを動かさないとLookAtの先が原点になっちゃうので、それを防ぐためにやる
            _mouseBasedLookAt.Position = cam.position;
            lookAtTarget.localPosition = _camBasedLookAt.Position;
        }

        //NOTE: タイミングがIKの適用前になることに注意: つまりTボーンっぽい状態
        private void Update()
        {
            _camBasedLookAt.CheckDepthAndWeight(_head);

            if (_hasModel && _faceControlConfig.ControlMode == FaceControlModes.ExternalTracker)
            {
                lookAtTarget.localPosition = _head.position + _head.forward * 10.0f;
                return;
            }
            
            Vector3 pos = 
                (_lookAtStyle == LookAtStyles.MousePointer) ? _mouseBasedLookAt.Position :
                (_lookAtStyle == LookAtStyles.MainCamera) ? _camBasedLookAt.Position :
                _hasModel ? _head.position + _head.forward * 10.0f : 
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

            public Vector3 Position => Vector3.Lerp(
                Camera.position + _depth * Camera.forward,
                _fixedLookAtPos,
                _fixedLookAtBlendWeight);
                
            public Quaternion Rotation => Camera.rotation;

            public void CheckDepthAndWeight(Transform head)
            {
                //Zの決め方に注意: キャラを正面から見ているときと後ろから見ているときで、手前にLookAtさせるか奥にLookAtさせるかを変更
                var forward = Camera.forward;
                var horizontalForward = new Vector3(forward.x, 0, forward.z).normalized;
            
                //zの値が小さい = カメラは真横、または後ろを向いている = キャラを正面から見ているハズ
                if (horizontalForward.z < 0.1f || head == null)
                {
                    _depth = 0;
                    _fixedLookAtBlendWeight = 0;
                    return;
                }
            
                //カメラのZ成分が増える(=真後ろから見る)のに近づくにつれて奥側を向かせる。
                //このとき、途中のブレンディングをするとき正面向き成分を混ぜることで、遷移中のLookAtを体にめり込みにくくする
                float depthFactor = 1.0f;
                if (horizontalForward.z < 0.5f)
                {
                    depthFactor = horizontalForward.z * 2;
                }

                //キャラを背後から映してるハズ: 奥行き方向にLookAtをずらしていく
                var camPosition = Camera.position;
                //Vector3.Dotのとこ = カメラからみてキャラが立ってる位置の奥行き。Yを考慮すると面倒なことになるため、XZ平面でやってます
                _depth = (2 * depthFactor) * Mathf.Abs(Vector3.Dot(
                                  new Vector3(camPosition.x, 0, camPosition.z), horizontalForward
                              ));

                
                //固定視点LookAtのウェイト (いわゆるテント写像)
                //depthFactor == 1つまりキャラの中心付近を通るときに1になって固定視点を経由し、両端(=通常のケース)ではゼロになる
                _fixedLookAtPos = head.position + head.forward * 1.0f;
                _fixedLookAtBlendWeight = 1.0f - 2.0f * Mathf.Abs(depthFactor - 0.5f);
            }

            //カメラが前方向き = キャラを後ろから映しているときの見栄えが破綻しないように奥行きを追加するやつ
            private float _depth = 0f;
            private Vector3 _fixedLookAtPos = Vector3.zero;
            private float _fixedLookAtBlendWeight = 0;

            public IKTargets Target => IKTargets.HeadLookAt;
        }

        private enum LookAtStyles
        {
            Fixed,
            MousePointer,
            MainCamera,
        }
    }
}
