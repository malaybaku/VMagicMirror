using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 手IKの移動差分を元にして、キャラの肩をFKで動かす処理。
    /// この処理自体はIKではない事に注意！
    /// </summary>
    /// <remarks>
    /// この処理は実はビルトインモーションと競合するんだけど、効きがそこまで強くないので有効になりっぱなしでもOKとします
    /// </remarks>
    public class ShoulderRotationModifier : MonoBehaviour
    {

        //基準長はMegumi Baxterさんの体型。(https://hub.vroid.com/characters/9003440353945198963/models/7418874241157618732)
        //Headボーンの高さ. コレ以外の値はSettingAutoAdjusterとかにも載ってます
        public const float ReferenceHeadHeight = 1.176175f;
        
        [SerializeField] private HandIKIntegrator handIk = null;
        [SerializeField] private WaitingBodyMotion waitMotion = null;

        #region アプリケーション内で固定する値

        //肩を落とす/上げる角度のリミット
        [SerializeField] private float rollMinDeg = 0f;
        [SerializeField] private float rollMaxDeg = 20f;

        //肩ごと腕を後ろに回す/前に突き出す角度のリミット
        [SerializeField] private float yawMinDeg = -15f;
        [SerializeField] private float yawMaxDeg = 10f;

        [Tooltip("ヒジの向き先の角度にあわせて肩を曲げるときの適用率")] [Range(0f, 1f)] [SerializeField]
        private float angleScale = 0.2f;

        [Tooltip("リファレンス体型(Megumi Baxterちゃん)の場合に手IKのY座標が一気にこの値だけ動いたら、肩のロール角をmaxにする(単位:m)")] 
        [Range(0.01f, 0.5f)] [SerializeField]
        private float handDiffMaxBase = 0.03f;

        [Tooltip("手IKの速度ベースで肩に対して追加できる最大のロール角度(プラスマイナス共通)")] 
        [Range(0f, 20f)] [SerializeField]
        private float handDiffMaxRollDeg = 3.0f;

        [Tooltip("手のIKの積分値を減衰させていくファクターで、大きいほどすばやく減衰する")] 
        [Range(0.2f, 20f)] [SerializeField]
        private float handDiffYDecreaseFactor = 0.6f;

        [Tooltip("手IKのY座標の移動量について、積分した値をhandDiffMaxの何倍までの値で保持するか")]
        [Range(1f, 5f)] [SerializeField]
        private float handDiffIntegrateFactor = 2f;

        [Range(0f, 30f)] [SerializeField] 
        private float waitMotionBasedAngleDeg = 3f;

        [Tooltip("待機モーションで、腰や胸よりも肩に位相遅れをつける度合い(大きな値ほど遅れる)")]
        [Range(0f, 1f)] [SerializeField] 
        private float waitMotionPhaseDelay = 0.2f;
        
        #endregion

        private IVRMLoadable _vrmLoadable = null;

        #region モデルロード時に値を決めるやつ

        private bool _hasValidShoulderBone = false;
        private Transform _leftShoulder = null;
        private Transform _leftUpperArm = null;
        private Transform _leftLowerArm = null;

        private Transform _rightShoulder = null;
        private Transform _rightUpperArm = null;
        private Transform _rightLowerArm = null;

        //handDiffMaxBaseに身長分の補正がかかった値です
        private float _handDiffMax = 0.01f;
        
        #endregion

        #region 毎フレーム変わる値

        private Vector3 _leftElbowOrientation = Vector3.left;
        private Vector3 _rightElbowOrientation = Vector3.right;

        //ここで言うstaticはヒジの向きが一定だと肩の角度も同じ(静的)だよ、ということ
        private Vector3 _staticLeftShoulderEuler = Vector3.zero;
        private Vector3 _staticRightShoulderEuler = Vector3.zero;

        //ここで言うdynamicは手IKの速度に応じて角度が決まり、IKが動かなくなると値がゼロに戻るよ、ということ
        private float _diffBasedLeftRollDeg = 0f;
        private float _diffBasedRightRollDeg = 0f;

        //上記とは独立に、待機モーションの状態から決まる追加の角度
        private float _waitMotionBasedLeftRollDeg = 0f;
        private float _waitMotionBasedRightRollDeg = 0f;

        private bool _hasPrevFrameHandIkPosition = false;

        //1フレーム前の手IKのY座標
        private float _prevLeftHandY = 0f;
        private float _prevRightHandY = 0f;

        //手IKのY座標が移動した量。積分しながらスローに減衰させた値が入ります
        private float _leftHandDiffY = 0f;
        private float _rightHandDiffY = 0f;

        #endregion
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private bool _enableRotationModification = true;
        public bool EnableRotationModification
        {
            get => _enableRotationModification;
            set
            {
                if (_enableRotationModification == value)
                {
                    return;
                }

                _enableRotationModification = value;
                if (!value && _hasValidShoulderBone)
                {
                    _leftShoulder.localRotation = Quaternion.identity;
                    _rightShoulder.localRotation = Quaternion.identity;
                }
            }
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _leftShoulder = info.animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            _leftUpperArm = info.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _leftLowerArm = info.animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);

            _rightShoulder = info.animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            _rightUpperArm = info.animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _rightLowerArm = info.animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            
            _handDiffMax = handDiffMaxBase * 
                info.animator.GetBoneTransform(HumanBodyBones.Head).position.y / ReferenceHeadHeight;
            //値が0寄りすぎると危ないので念のため。
            if (_handDiffMax < 0.001f)
            {
                _handDiffMax = 0.001f;
            }
            
            _hasValidShoulderBone = (handIk != null && _leftShoulder != null && _rightShoulder != null);


            InitializeBoneParameters(info.animator);
        }

        private void OnVrmDisposing()
        {
            _hasValidShoulderBone = false;
            _hasPrevFrameHandIkPosition = false;
            
            _leftShoulder = null;
            _leftUpperArm = null;
            _leftLowerArm = null;
            
            _rightShoulder = null;
            _rightUpperArm = null;
            _rightLowerArm = null;
        }

        private void InitializeBoneParameters(Animator animator)
        {
            _staticLeftShoulderEuler = Vector3.zero;
            _staticRightShoulderEuler = Vector3.zero;
            _diffBasedLeftRollDeg = 0;
            _diffBasedRightRollDeg = 0;

            _leftElbowOrientation = Vector3.left;
            _rightElbowOrientation = Vector3.right;
        }

        private void Start()
        {
            StartCoroutine(CheckElbowPostureOnEndOfFrame());
        }

        private void LateUpdate()
        {
            if (!_hasValidShoulderBone || !_enableRotationModification)
            {
                return;
            }

            UpdateStaticRotation();
            UpdateDynamicRotation();
            UpdateWaitMotionBasedRotation();

            _leftShoulder.localRotation = Quaternion.Euler(
                0,
                _staticLeftShoulderEuler.y ,
                _staticLeftShoulderEuler.z + _diffBasedLeftRollDeg + _waitMotionBasedLeftRollDeg
            );

            _rightShoulder.localRotation = Quaternion.Euler(
                0,
                _staticRightShoulderEuler.y,
                _staticRightShoulderEuler.z + _diffBasedRightRollDeg + _waitMotionBasedRightRollDeg
            );
            
            void UpdateStaticRotation()
            {
                _staticLeftShoulderEuler = new Vector3(
                    0, 
                    Mathf.Clamp(
                        Mathf.Asin(_leftElbowOrientation.z) * Mathf.Rad2Deg * angleScale, 
                        yawMinDeg,
                        yawMaxDeg
                    ),
                    Mathf.Clamp(
                        -Mathf.Asin(_leftElbowOrientation.y) * Mathf.Rad2Deg * angleScale,
                        -rollMaxDeg, 
                        -rollMinDeg
                    )
                );
            
                _staticRightShoulderEuler = new Vector3(
                    0, 
                    Mathf.Clamp(
                        -Mathf.Asin(_rightElbowOrientation.z) * Mathf.Rad2Deg * angleScale,
                        -yawMaxDeg,
                        -yawMinDeg
                    ),
                    Mathf.Clamp(
                        Mathf.Asin(_rightElbowOrientation.y) * Mathf.Rad2Deg * angleScale,
                        rollMinDeg, 
                        rollMaxDeg
                    )
                );
            }

            //NOTE: 手のIK自体がキレイな値になっている前提で書かれています
            void UpdateDynamicRotation()
            {
                //最初のフレームは初期化だけで終わり
                if (!_hasPrevFrameHandIkPosition)
                {
                    _prevLeftHandY = handIk.LeftHandPosition.y;
                    _prevRightHandY = handIk.RightHandPosition.y;

                    _leftHandDiffY = 0f;
                    _rightHandDiffY = 0f;
                    
                    _hasPrevFrameHandIkPosition = true;
                    return;
                }

                //書いてる手順の通りだが、積分値を角度にしたあとで範囲制限とか減衰をやっていく
                
                float leftY =  handIk.LeftHandPosition.y;
                _leftHandDiffY += leftY - _prevLeftHandY;
                _diffBasedLeftRollDeg =
                    Mathf.Clamp(-_leftHandDiffY / _handDiffMax, -1, 1) * handDiffMaxRollDeg;
                
                float rightY =  handIk.RightHandPosition.y;
                _rightHandDiffY += rightY - _prevRightHandY;
                _diffBasedRightRollDeg =
                    Mathf.Clamp(_rightHandDiffY / _handDiffMax, -1, 1) * handDiffMaxRollDeg;

                
                _leftHandDiffY = Mathf.Clamp(
                    _leftHandDiffY,
                    -handDiffIntegrateFactor * _handDiffMax,
                    handDiffIntegrateFactor * _handDiffMax
                    );
                _leftHandDiffY = Mathf.Lerp(_leftHandDiffY, 0f, handDiffYDecreaseFactor * Time.deltaTime);

                _rightHandDiffY = Mathf.Clamp(
                    _rightHandDiffY,
                    -handDiffIntegrateFactor * _handDiffMax,
                    handDiffIntegrateFactor * _handDiffMax
                );
                _rightHandDiffY = Mathf.Lerp(_rightHandDiffY, 0f, handDiffYDecreaseFactor * Time.deltaTime);

                _prevLeftHandY = leftY;
                _prevRightHandY = rightY;
            }

            void UpdateWaitMotionBasedRotation()
            {
                float phase = Mathf.Repeat(
                    (waitMotion.Phase - waitMotionPhaseDelay) * Mathf.PI * 2.0f,
                    Mathf.PI * 2.0f
                );
                
                //半角公式みたいな形にする: 肩は落とすと見栄えがわるいので、上げるほうにだけ動かすための式がコレです。
                // float angle = waitMotionBasedAngleDeg * 0.5f * (1f - Mathf.Cos(phase));
                float angle = - waitMotionBasedAngleDeg * Mathf.Cos(phase);
                _waitMotionBasedLeftRollDeg = -angle;
                _waitMotionBasedRightRollDeg = angle;
            }
        }

        private IEnumerator CheckElbowPostureOnEndOfFrame()
        {
            while (true)
            {
                //NOTE: EndOfFrameにするのはIKが適用された状態の値が知りたいから。
                yield return new WaitForEndOfFrame();
                if (!_hasValidShoulderBone)
                {
                    continue;
                }

                //NOTE:
                // - rotationベースで計算するほうが軽いけど、ボトルネックじゃないので雑にやってます
                // - Shoulder-LowerArmで計算するのとUpperArm-LowerArmで計算するのと2択ある。前者のほうがCCDIKのノリには近い。
                _leftElbowOrientation = (_leftLowerArm.position - _leftShoulder.position).normalized;
                _rightElbowOrientation = (_rightLowerArm.position - _rightShoulder.position).normalized;
            }
        } 
        
    }
}
