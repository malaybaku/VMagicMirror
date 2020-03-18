using UnityEngine;
using Zenject;

//NOTE: 角度について
//今回は「手のひらがほぼカメラ側を向いてて顔付近にある」というケースのみケアするので、ヒトの自然な姿勢がとーっても限定される。
//ので、
// * 適当な基準位置: 手のひらがほんとに真正面を向く
// * 基準からの+X, -X : IKのヨー
// * 基準からの+Y, -Y : IKのピッチ


namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像処理で得た手の姿勢をキャラのIK情報にしてくれるやつ。
    /// </summary>
    /// <remarks>
    /// 手が検出できなくなったときは適当に手を下げた状態に持っていきます。
    /// </remarks>
    public class ImageBaseHandIkGenerator : MonoBehaviour
    {
        //NOTE: この角度は手が真正面に向く回転を指定してればOKで、最終的に定数にしてもいいです
        private static readonly Vector3 RightHandForwardRotEuler = new Vector3(0, -90, 90);
        private static readonly Vector3 LeftHandForwardRotEuler = new Vector3(0, 90, -90);

        [SerializeField] private HandShapeSetter handShapeSetter = null;
        
        [Tooltip("ウェブカメラ上での顔と手が画面の両端にあるとしたら頭と手をどのくらいの距離にしますか、という値(m)")]
        [SerializeField] private float handPositionScale = 1.0f;
        
        [Tooltip("この時間だけ手のトラッキングが切断したばあい、検出されなくなったと判断して手を下げ始める")]
        [SerializeField] private float handNonTrackedCountDown = 0.6f;

        [Tooltip("トラッキング切断後、この時間だけ経過したら手を少しずつ下にずり下げる、という制限時間。")]
        [SerializeField] private float handNonTrackedSlideDownCount = 0.2f;
        
        [Tooltip("右の手のひらが真正面に向くときの手と顔の相対位置")]
        [SerializeField] private Vector2 rightBaseRotAppliedDistance = new Vector2(0.3f, 0.0f);

        //左の手のひらが真正面に向くときの手と顔の相対位置
        private Vector2 leftBaseRotAppliedDistance =>
            new Vector2(-rightBaseRotAppliedDistance.x, rightBaseRotAppliedDistance.y);
        
        [Tooltip("手がロストしたときちょっとだけ動かすための速度")]
        [SerializeField] private Vector3 trackLostSpeed = new Vector3(0, -0.05f, 0);

        [Tooltip("基準の距離と比べて今の距離がどうなってるかを元に手のロール、ピッチを変える度合い。[deg/distance]")]
        [SerializeField] private Vector2 rotRateByDistanceFromBase = new Vector2(-200, -100);
        
        [Tooltip("ハンドトラッキングがロスしたときにAポーズへ落とし込むときの、腕の下げ角度(手首の曲げもコレに準拠します")]
        [SerializeField] private float aPoseArmDownAngleDeg = 70f;
        [Tooltip("Aポーズから少しだけ手首の位置を斜め前方上にズラすオフセット")]
        [SerializeField] private Vector3 aPoseArmPositionOffset = new Vector3(0f, 0.05f, 0.05f);

        [Tooltip("通常のIK変化時に使うLerpファクタ")]
        [SerializeField] private float ikLerpFactor = 12f;
        [Tooltip("手の検出/未検出のあいだでジャンプさせる時に使うLerpファクタ")]
        [SerializeField] private float nonTrackRotLerpFactor = 3f;

        [Tooltip("トラッキング中かどうかによらず、速度をローパスで適用する値")]
        [SerializeField] private float speedLowPassFactor = 8f;

        [Tooltip("ローパス前に速度の基準を決めるのに使う時定数")]
        [SerializeField] private float speedTimeRate = 0.3f;

        [Tooltip("手の横方向の速度(m/s)を手首のロール(deg)に変えるファクター")] 
        [SerializeField] private float speedRotRate = 40f;

        [Tooltip("手の移動速度の目標値の限度(m/s)")]
        [SerializeField] private float ikSpeedLimit = 1.0f;

        [Tooltip("この秒数だけトラッキングロストしたあとは手の位置をがっちり決めてしまう")]
        [SerializeField] private float nonTrackCountMax = 1.0f;
        
        private HandTracker _handTracker = null;
        private IVRMLoadable _vrmLoadable = null;

        private Vector3 _rightHandHipOffsetWhenNotTrack;
        private Vector3 _leftHandHipOffsetWhenNotTrack;
        private Quaternion _rightHandRotWhenNotTrack;
        private Quaternion _leftHandRotWhenNotTrack;
        
        private bool _hasVrmInfo = false;
        private Transform _head = null;
        private Transform _hips = null;

        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        //ほぼ生の検出位置および姿勢
        private Vector3 _rawLeftHandPos;
        private Quaternion _rawLeftHandRot;
        private Vector3 _rawRightHandPos;
        private Quaternion _rawRightHandRot;

        private Vector3 _rightHandPosSpeed;
        private Vector3 _leftHandPosSpeed;

        private HandShapeSetter.HandShapeTypes _leftHandShape = HandShapeSetter.HandShapeTypes.Default;
        private HandShapeSetter.HandShapeTypes _nextLeftHandShape = HandShapeSetter.HandShapeTypes.Default;
        private int _nextLeftHandShapeCount = 0;
     
        private HandShapeSetter.HandShapeTypes _rightHandShape = HandShapeSetter.HandShapeTypes.Default;
        private HandShapeSetter.HandShapeTypes _nextRightHandShape = HandShapeSetter.HandShapeTypes.Default;
        private int _nextRightHandShapeCount = 0;
        
        private float _leftHandNonTrackCountDown = 0f;
        private float _rightHandNonTrackCountDown = 0f;
        private float _leftHandNonTrackCount = 0f;
        private float _rightHandNonTrackCount = 0f;
        

        /// <summary>
        /// 左手の手検出データが更新されるとtrueになります。値を読んだらフラグを下げることで、次に検出されるのを待つことができます。
        /// </summary>
        public bool HasLeftHandUpdate { get; set; } = false;
        
        /// <summary>
        /// 右手の手検出データが更新されるとtrueになります。値を読んだらフラグを下げることで、次に検出されるのを待つことができます。
        /// </summary>
        public bool HasRightHandUpdate { get; set; } = false;


        private void AccumulateLeftHandShape(HandShapeSetter.HandShapeTypes type)
        {
            //今と同じ値を指定: 無視
            if (type == _leftHandShape)
            {
                _nextLeftHandShape = type;
                _nextLeftHandShapeCount = 0;
                return;
            }
            
            if (type != _nextLeftHandShape)
            {
                _nextLeftHandShapeCount = 0;
                _nextLeftHandShape = type;
            }
            
            //デフォルトへ戻す: 一発許可
            if (type == HandShapeSetter.HandShapeTypes.Default)
            {
                _leftHandShape = HandShapeSetter.HandShapeTypes.Default;
                _nextLeftHandShape = HandShapeSetter.HandShapeTypes.Default;
                _nextLeftHandShapeCount = 0;
                handShapeSetter.SetHandShape(HandShapeSetter.HandTypes.Left, HandShapeSetter.HandShapeTypes.Default);
                return;
            }
            
            //デフォルト以外: 5回連続したらOK
            _nextLeftHandShapeCount++;
            if (_nextLeftHandShapeCount > 5)
            {
                _leftHandShape = type;
                _nextLeftHandShapeCount = 0;
                handShapeSetter.SetHandShape(HandShapeSetter.HandTypes.Left, _leftHandShape);
            }
        }
        
        private void AccumulateRightHandShape(HandShapeSetter.HandShapeTypes type)
        {
            //今と同じ値を指定: ガン無視
            if (type == _rightHandShape)
            {
                _nextRightHandShape = type;
                _nextRightHandShapeCount = 0;
                return;
            }
            
            if (type != _nextRightHandShape)
            {
                _nextRightHandShapeCount = 0;
                _nextRightHandShape = type;
            }
            
            //デフォルトへ戻す: 一発許可
            if (type == HandShapeSetter.HandShapeTypes.Default)
            {
                _rightHandShape = HandShapeSetter.HandShapeTypes.Default;
                _nextRightHandShape = HandShapeSetter.HandShapeTypes.Default;
                _nextRightHandShapeCount = 0;
                handShapeSetter.SetHandShape(HandShapeSetter.HandTypes.Right, HandShapeSetter.HandShapeTypes.Default);
                return;
            }
            
            //デフォルト以外: 5回連続したらOK
            _nextRightHandShapeCount++;
            if (_nextRightHandShapeCount > 5)
            {
                _rightHandShape = type;
                _nextRightHandShapeCount = 0;
                handShapeSetter.SetHandShape(HandShapeSetter.HandTypes.Right, _rightHandShape);
            }
        }

        [Inject]
        public void Initialize(HandTracker handTracker, IVRMLoadable vrmLoadable)
        {
            _handTracker = handTracker;
            _vrmLoadable = vrmLoadable;
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private void LateUpdate()
        {
            if (!_hasVrmInfo)
            {
                return;
            }

            //HandTrackerからのデータ吸い上げるのがこっち
            ConsumeUpdatedHandPosture();

            UpdateTrackCount();
            
            //IKをいい感じにするのはこっち
            UpdateIk();
        }

        private void ConsumeUpdatedHandPosture()
        {
            ConsumeLeftHandUpdate();
            ConsumeRightHandUpdate();
        }

        private void ConsumeLeftHandUpdate()
        {
            //NOTE: 画像上で右側に写ってるのが左手です。
            var src = _handTracker.RightSideHand;
            
            if (!src.HasValidHandDetectResult)
            {
                return;
            }

            //フラグを下げることで、今入っているデータを消費する。Produce/Consumeみたいなアレですね。
            src.HasValidHandDetectResult = false;

            var handShape =
                src.ConvexDefectCount > 1 ? HandShapeSetter.HandShapeTypes.Paper :
                src.ConvexDefectCount > 0 ? HandShapeSetter.HandShapeTypes.Scissors :
                HandShapeSetter.HandShapeTypes.Rock;
           
            var handPos = src.HandPosition - src.ReferenceFacePosition;
            //NOTE: ここで反転処理をかます事により、鏡像反転していない状態に持ち込む。
            //鏡像反転をさせちゃうと「左手をふりながらマウスを動かす」とかの動きがすげー破綻するので、それを防ぐのが狙い
            handPos.x = -handPos.x;
            
            HasLeftHandUpdate = true;
            _rawLeftHandPos =
                _head.position +
                new Vector3(0, 0, 0.2f) +
                handPositionScale * new Vector3(handPos.x, handPos.y, 0);

            _rawLeftHandRot =
                Quaternion.AngleAxis((handPos.x - leftBaseRotAppliedDistance.x) * rotRateByDistanceFromBase.x, Vector3.forward) * 
                ((handPos.y - leftBaseRotAppliedDistance.y > 0) 
                    ? Quaternion.identity 
                    : Quaternion.AngleAxis((handPos.y - leftBaseRotAppliedDistance.y) * rotRateByDistanceFromBase.y, Vector3.right)
                ) * 
                Quaternion.Euler(LeftHandForwardRotEuler);
            
            _leftHandNonTrackCountDown = handNonTrackedCountDown;
            AccumulateLeftHandShape(handShape);
        }
     
        private void ConsumeRightHandUpdate()
        {
            //NOTE: 画像上で右側に写ってるのが左手です。
            var src = _handTracker.LeftSideHand;
            
            if (!src.HasValidHandDetectResult)
            {
                return;
            }

            //フラグを下げることで、今入っているデータを消費する。Produce/Consumeみたいなアレですね。
            src.HasValidHandDetectResult = false;

            var handShape =
                src.ConvexDefectCount > 1 ? HandShapeSetter.HandShapeTypes.Paper :
                src.ConvexDefectCount > 0 ? HandShapeSetter.HandShapeTypes.Scissors :
                HandShapeSetter.HandShapeTypes.Rock;
           
            var handPos = src.HandPosition - src.ReferenceFacePosition;
            //NOTE: ここで反転処理をかます事により、鏡像反転していない状態に持ち込む。
            //鏡像反転をさせちゃうと「左手をふりながらマウスを動かす」とかの動きがすげー破綻するので、それを防ぐのが狙い
            handPos.x = -handPos.x;
            
            HasRightHandUpdate = true;
            _rawRightHandPos =
                _head.position +
                new Vector3(0, 0, 0.2f) +
                handPositionScale * new Vector3(handPos.x, handPos.y, 0);

            _rawRightHandRot =
                Quaternion.AngleAxis((handPos.x - rightBaseRotAppliedDistance.x) * rotRateByDistanceFromBase.x, Vector3.forward) * 
                ((handPos.y - rightBaseRotAppliedDistance.y > 0) 
                    ? Quaternion.identity 
                    : Quaternion.AngleAxis((handPos.y - rightBaseRotAppliedDistance.y) * rotRateByDistanceFromBase.y, Vector3.right)
                ) * 
                Quaternion.Euler(RightHandForwardRotEuler);

            _rightHandNonTrackCountDown = handNonTrackedCountDown;

            AccumulateRightHandShape(handShape);
        }
        
        private void UpdateTrackCount()
        {
            if (_leftHandNonTrackCountDown > 0)
            {
                _leftHandNonTrackCount = 0;
                _leftHandNonTrackCountDown -= Time.deltaTime;
            }

            if (_rightHandNonTrackCountDown > 0)
            {
                _rightHandNonTrackCount = 0;
                _rightHandNonTrackCountDown -= Time.deltaTime;
            }

            if (_leftHandNonTrackCountDown <= 0 && _leftHandNonTrackCount < nonTrackCountMax)
            {
                _leftHandNonTrackCount += Time.deltaTime;
            }
            
            if (_rightHandNonTrackCountDown <= 0 && _rightHandNonTrackCount < nonTrackCountMax)
            {
                _rightHandNonTrackCount += Time.deltaTime;
            }
        }
        
        private void UpdateIk()
        {
            var hipsPos = _hips.position;
            UpdateLeftHandIk(hipsPos);
            UpdateRightHandIk(hipsPos);
            
        }
        
        private void UpdateLeftHandIk(Vector3 hipPos)
        {
            if (_leftHandNonTrackCount >= nonTrackCountMax)
            {
                _leftHand.Position = hipPos + _leftHandHipOffsetWhenNotTrack;
            }
            else
            {
                var targetPos = _leftHandNonTrackCountDown > 0
                    ? _rawLeftHandPos
                    : hipPos + _leftHandHipOffsetWhenNotTrack;
            
                //トラッキングロストが起きた時間を使って対象位置をずり下げる
                if (_leftHandNonTrackCountDown > 0 && _leftHandNonTrackCountDown < handNonTrackedSlideDownCount)
                {
                    targetPos += (handNonTrackedSlideDownCount - _leftHandNonTrackCountDown) * trackLostSpeed;
                }
            
                //この速度で動かしてえな～という目標値
                var rawSpeed = (targetPos - _leftHand.Position) / speedTimeRate;
                if (rawSpeed.magnitude > ikSpeedLimit)
                {
                    rawSpeed *= ikSpeedLimit / rawSpeed.magnitude;
                }

                _leftHandPosSpeed = Vector3.Lerp(_leftHandPosSpeed, rawSpeed, Time.deltaTime * speedLowPassFactor);
                _leftHand.Position += _leftHandPosSpeed * Time.deltaTime;

                if (_leftHandNonTrackCount > 0)
                {
                    //NOTE: 徐々に固定位置に近づけるためのアレです
                    _leftHand.Position = Vector3.Lerp(
                        _leftHand.Position, targetPos, _leftHandNonTrackCount / nonTrackCountMax
                        );
                }
            }
            
            //回転についてはナイーブに捌く
            if (_leftHandNonTrackCountDown > 0)
            {
                _leftHand.Rotation = Quaternion.Slerp(
                    _leftHand.Rotation, 
                    Quaternion.AngleAxis(-_leftHandPosSpeed.x * speedRotRate, Vector3.forward) * _rawLeftHandRot,
                    Time.deltaTime * ikLerpFactor
                    );
            }
            else
            {
                AccumulateLeftHandShape(HandShapeSetter.HandShapeTypes.Default);
                _leftHand.Rotation = Quaternion.Slerp(
                    _leftHand.Rotation, 
                    _leftHandRotWhenNotTrack,
                    Time.deltaTime * nonTrackRotLerpFactor
                    );
            }
        }
        
        private void UpdateRightHandIk(Vector3 hipPos)
        {
            if (_rightHandNonTrackCount >= nonTrackCountMax)
            {
                _rightHand.Position = hipPos + _rightHandHipOffsetWhenNotTrack;
            }
            else
            {
                var targetPos = _rightHandNonTrackCountDown > 0
                    ? _rawRightHandPos
                    : hipPos + _rightHandHipOffsetWhenNotTrack;

                //トラッキングロストが起きた時間を使って対象位置をずり下げる
                if (_rightHandNonTrackCountDown > 0 && _rightHandNonTrackCountDown < handNonTrackedSlideDownCount)
                {
                    targetPos += (handNonTrackedSlideDownCount - _rightHandNonTrackCountDown) * trackLostSpeed;
                }

                //この速度で動かしてえな～という目標値
                var rawSpeed = (targetPos - _rightHand.Position) / speedTimeRate;
                if (rawSpeed.magnitude > ikSpeedLimit)
                {
                    rawSpeed *= ikSpeedLimit / rawSpeed.magnitude;
                }
            
                _rightHandPosSpeed = Vector3.Lerp(_rightHandPosSpeed, rawSpeed, Time.deltaTime * speedLowPassFactor);
                _rightHand.Position += _rightHandPosSpeed * Time.deltaTime;

                if (_rightHandNonTrackCount > 0)
                {
                    _rightHand.Position = Vector3.Lerp(
                        _rightHand.Position, targetPos, _rightHandNonTrackCount / nonTrackCountMax
                        );
                }
            }
            
            if (_rightHandNonTrackCountDown > 0)
            {
                _rightHand.Rotation = Quaternion.Slerp(
                    _rightHand.Rotation, 
                    Quaternion.AngleAxis(-_rightHandPosSpeed.x * speedRotRate, Vector3.forward) * _rawRightHandRot, 
                    Time.deltaTime * ikLerpFactor
                    );
            }
            else
            {
                AccumulateRightHandShape(HandShapeSetter.HandShapeTypes.Default);
                _rightHand.Rotation = Quaternion.Slerp(
                    _rightHand.Rotation, 
                    _rightHandRotWhenNotTrack,
                    Time.deltaTime * nonTrackRotLerpFactor
                    );
            }
        }

        private void OnVrmDisposing()
        {
            _hasVrmInfo = false;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            var animator = info.animator;
            
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var hipsPos = _hips.position;

            var rightUpperArmPos = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            var rightWristPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;

            _rightHandHipOffsetWhenNotTrack =
                rightUpperArmPos
                + Quaternion.AngleAxis(-aPoseArmDownAngleDeg, Vector3.forward) * (rightWristPos - rightUpperArmPos)
                - hipsPos
                + aPoseArmPositionOffset;

            var leftUpperArmPos = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            var leftWristPos = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;

            _leftHandHipOffsetWhenNotTrack =
                leftUpperArmPos
                + Quaternion.AngleAxis(aPoseArmDownAngleDeg, Vector3.forward) * (leftWristPos - leftUpperArmPos)
                - hipsPos
                + aPoseArmPositionOffset;

            _rightHandRotWhenNotTrack = Quaternion.Euler(0, 0, -aPoseArmDownAngleDeg);
            _leftHandRotWhenNotTrack = Quaternion.Euler(0, 0, aPoseArmDownAngleDeg);

            _hasVrmInfo = true;
        }
    }
}
