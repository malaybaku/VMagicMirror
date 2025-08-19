using Baku.VMagicMirror.VMCP;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    //TODO: BodyOffsetの提供クラスをinterface-nizeすることを検討すべき
    /// <summary>
    /// VMCProtocolで頭部姿勢を受け取っているとき、そのオフセットを提供するクラス。
    /// ただし、<see cref="VMCPNaiveBoneTransfer"/> によって下半身ベースでの移動も考慮した場合は、
    /// <see cref="VMCPNaiveBoneTransfer"/> でアバターのHipsを最終的に制御した値のほうが優先になる
    /// </summary>
    public class VMCPBodyOffset : MonoBehaviour
    {
        [Tooltip("受け取った値の適用スケール。割と小さい方がいいかも")]
        [SerializeField] private Vector3 applyScale = new Vector3(0.3f, 0.3f, 0.3f);
        //NOTE: 移動量もフレーバー程度ということで小さめに。
        [SerializeField] private Vector3 applyMin = new Vector3(-0.05f, -0.05f, -0.02f);
        [SerializeField] private Vector3 applyMax = new Vector3(0.05f, 0.05f, 0.02f);

        [SerializeField] private float lerpFactor = 18f;
        [Tooltip("トラッキングロス時にゆっくり原点に戻すために使うLerpFactor")]
        [SerializeField] private float lerpFactorOnLost = 3f;
        [Tooltip("接続が持続しているあいだ、徐々にLerpFactorを引き上げるのに使う値")]
        [SerializeField] private float lerpFactorBlendDuration = 1f;
        
        private FaceControlConfiguration _config;
        private VMCPHeadPose _vmcpHeadPose;
        
        private Vector3 _scale = Vector3.zero;
        private Vector3 _min = Vector3.zero;
        private Vector3 _max = Vector3.zero;
        private Sequence _sequence = null;
        private float _currentLerpFactor;
        
        [Inject]
        public void Initialize(FaceControlConfiguration config, VMCPHeadPose vmcpHeadPose)
        {
            _config = config;
            _vmcpHeadPose = vmcpHeadPose;
        }
        
        public Vector3 BodyOffset { get; private set; }

        private void Start()
        {
            //初期状態は手下げモードじゃないため、それ用のパラメータを入れておく
            _scale = applyScale;
            _min = applyMin;
            _max = applyMax;
            
            //lerpFactorは接続の信頼性に応じて上がっていく
            _currentLerpFactor = lerpFactorOnLost;
        }
        
        private void Update()
        {
            if (_config.HeadMotionControlModeValue != FaceControlModes.VMCProtocol)
            {
                BodyOffset = Vector3.zero;
                return;
            }
            
            var offset = _vmcpHeadPose.PositionOffset;

            var goal = _vmcpHeadPose.IsConnected.CurrentValue
                ? new Vector3(
                    Mathf.Clamp(offset.x * _scale.x, _min.x, _max.x),
                    Mathf.Clamp(offset.y * _scale.y, _min.y, _max.y),
                    Mathf.Clamp(offset.z * _scale.z, _min.z, _max.z)
                )
                : Vector3.zero;

            if (_vmcpHeadPose.IsConnected.CurrentValue)
            {
                var addedLerp = 
                    _currentLerpFactor +
                    (lerpFactor - lerpFactorOnLost) * Time.deltaTime / lerpFactorBlendDuration;
                _currentLerpFactor = Mathf.Min(addedLerp, lerpFactor);
            }
            else
            {
                //ロスったら一番遅いとこまでリセットし、やり直してもらう
                _currentLerpFactor = lerpFactorOnLost;
            }
            
            BodyOffset = Vector3.Lerp(BodyOffset, goal, _currentLerpFactor * Time.deltaTime);
        }
    }
}
