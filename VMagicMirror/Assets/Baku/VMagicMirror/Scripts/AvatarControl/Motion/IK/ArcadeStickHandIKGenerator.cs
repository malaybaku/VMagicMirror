using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ゲームパッドの入力状況に対してアーケードスティック型の腕IKを生成するやつ。
    /// </summary>
    public class ArcadeStickHandIKGenerator : HandIkGeneratorBase
    {
        // 左手の追従速度。かなりスピーディ
        private const float RightHandSpeedFactor = 12.0f;
        // 左手の追従速度。そこそこスピーディ
        private const float LeftHandSpeedFactor = 12.0f;
        
        //ボタンを押すとき手ごと下に下げる移動量。
        private const float ButtonDownY = 0.01f;

        public ArcadeStickHandIKGenerator(
            MonoBehaviour coroutineResponder, 
            IVRMLoadable vrmLoadable,
            ArcadeStickProvider stickProvider) : base(coroutineResponder)
        {
            _stickProvider = stickProvider;

            //モデルロード時、身長を参照することで「コントローラの移動オフセットはこんくらいだよね」を初期化
            vrmLoadable.VrmLoaded += info =>
            {
                var h = info.animator.GetBoneTransform(HumanBodyBones.Head);
                var f = info.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                CacheHandOffsets(info.animator);
            };
        }

        //右手の5本の指について、手首から指先までのオフセットを大まかにチェックしたもの。
        //指に対してはIKをあまり使いたくないため、FKを大まかに合わせるのに使う
        //NOTE: Tポーズのときのワールド座標で見た差分が入る
        private readonly Vector3[] _wristToFingerOffsets = new Vector3[5];
        //こっちはスティックの握り込みっぽくなるよう位置を調整するやつ
        private Vector3 _leftHandPalmOffset;
        
        private readonly ArcadeStickProvider _stickProvider;
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;
        
        private Vector2 _rawStickPos = Vector2.zero;
        private Vector2 _filterStickPos = Vector2.zero;
        
        private float _offsetY = 0;
        private int _buttonDownCount = 0;

        private Vector3 _latestButtonPos;
        private Quaternion _latestButtonRot;
        
        public void ButtonDown(GamepadKey key)
        {
            if (!ArcadeStickProvider.IsArcadeStickKey(key))
            {
                return;
            }

            _buttonDownCount++;

            (_latestButtonPos, _latestButtonRot) = _stickProvider.GetRightHand(key);

            //NOTE: 指をだいたい揃えるためにズラす動きがコレ
            int fingerNumber = ArcadeStickFingerController.KeyToFingerNumber(key);
            int offsetIndex = fingerNumber - 5;
            //1倍ぴったりを適用すると指の曲げのぶんのズレで絵面がイマイチになる可能性もあるが、
            //余程手が大きくなければ大丈夫なはず
            _latestButtonPos -= _latestButtonRot * _wristToFingerOffsets[offsetIndex];
        }

        /// <summary>
        /// 手首IKにするためのオフセットを考慮しないような、ボタンそのものの位置、角度を取得します。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public (Vector3, Quaternion) GetButtonPose(GamepadKey key)
        {
            return _stickProvider.GetRightHandRaw(key);
        }

        public void ButtonUp(GamepadKey key)
        {
            if (!ArcadeStickProvider.IsArcadeStickKey(key))
            {
                return;
            }
            _buttonDownCount--;

            //通常起きないハズだが一応
            if (_buttonDownCount < 0)
            {
                _buttonDownCount = 0;
            }
        }
        
        public void LeftStick(Vector2 stickPos)
        {
            _rawStickPos = stickPos;
        }

        public void ButtonStick(Vector2Int buttonStickPos)
        {
            _rawStickPos = NormalizedStickPos(buttonStickPos);

            Vector2 NormalizedStickPos(Vector2Int v)
            {
                const float factor = 1.0f / 32768.0f;
                return new Vector2(v.x * factor, v.y * factor);
            }
        }
        
        public override void Start()
        {
            //NOTE: 初期値が原点とかだと流石にキツいので、値を拾わせておく
            (_latestButtonPos, _latestButtonRot) = _stickProvider.GetRightHand(GamepadKey.A);
        }
        
        public override void Update()
        {
            //スティックについてはスティック値自体をLerpすることで平滑化
            _filterStickPos = Vector2.Lerp(_filterStickPos, _rawStickPos, LeftHandSpeedFactor * Time.deltaTime);
            var (leftPos, leftRot) = _stickProvider.GetLeftHand(_filterStickPos);
            _leftHand.Position = leftPos - _stickProvider.GetStickBaseRotation() * _leftHandPalmOffset;
            _leftHand.Rotation = leftRot;
            
            //ボタンを押してる/押してないでy方向に移動が入ることに注意しつつ、素朴にLerp/Slerp
            //y軸方向の移動だけ更にシャープにするのもアリだが、書くのがめんどくさい…
            var offsetY = (_buttonDownCount > 0) ? -ButtonDownY : 0;
            var posWithOffset = _latestButtonPos + offsetY * _stickProvider.GetYAxis();
            _rightHand.Position = Vector3.Lerp(_rightHand.Position, posWithOffset, RightHandSpeedFactor * Time.deltaTime);
            _rightHand.Rotation = Quaternion.Slerp(_rightHand.Rotation, _latestButtonRot, RightHandSpeedFactor * Time.deltaTime);
        }
        
        private void CacheHandOffsets(Animator animator)
        {
            //左手 : 手のひら中央の位置を推定するため、中指の付け根を見に行く
            var leftWrist = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var leftMiddle = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            _leftHandPalmOffset = leftMiddle != null ? 0.5f * (leftMiddle.position - leftWrist.position) : Vector3.zero;

            //右手 : 指の位置を一通りチェックする
            var rightWristPos = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            var intermediateBones = new Transform[]
            {
                //NOTE: 指が約30度曲がったとき関節1個ぶんのズレが発生するので、あえてDistalではなくIntermediateを見る
                animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate),
                animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate),
                animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate),
                animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate),
                animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate),
            };
            
            //NOTE: さらに、指が30度曲がるとだいたい関節2つぶんy方向にオフセットがつくので、その分を計算する
            var proximalBones = new Transform[]
            {
                animator.GetBoneTransform(HumanBodyBones.RightThumbProximal),
                animator.GetBoneTransform(HumanBodyBones.RightIndexProximal),
                animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal),
                animator.GetBoneTransform(HumanBodyBones.RightRingProximal),
                animator.GetBoneTransform(HumanBodyBones.RightLittleProximal),
            };
            
            var distalBones = new Transform[]
            {
                animator.GetBoneTransform(HumanBodyBones.RightThumbDistal),
                animator.GetBoneTransform(HumanBodyBones.RightIndexDistal),
                animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal),
                animator.GetBoneTransform(HumanBodyBones.RightRingDistal),
                animator.GetBoneTransform(HumanBodyBones.RightLittleDistal),
            };
            
            for (int i = 0; i < intermediateBones.Length; i++)
            {
                //NOTE: 指が一部なくてもエラーにはならないが、指の一部だけが欠けていると計算としてはかなり崩れる。
                //指が全部 or 全部あるモデルが大多数派であると考えてこのくらいで妥協してます
                _wristToFingerOffsets[i] =
                    (proximalBones[i] != null && intermediateBones[i] != null && distalBones[i] != null)
                        ? intermediateBones[i].position - 
                          rightWristPos - 
                          Vector3.up * (intermediateBones[i].localPosition.magnitude + distalBones[i].localPosition.magnitude) 
                        : Vector3.zero;
            }
        }

    }
}
