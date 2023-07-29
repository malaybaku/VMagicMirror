using System;
using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror.IK
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

        
        //右手の5本の指について、手首から指先までのオフセットを大まかにチェックしたもの。
        //指に対してはIKをあまり使いたくないため、FKを大まかに合わせるのに使う
        //NOTE: Tポーズのときのワールド座標で見た差分が入る
        private readonly Vector3[] _wristToFingerOffsets = new Vector3[5];
        //こっちはスティックの握り込みっぽくなるよう位置を調整するやつ
        private Vector3 _leftHandPalmOffset;
        
        private readonly ArcadeStickProvider _stickProvider;
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        
        private Vector2 _rawStickPos = Vector2.zero;
        private Vector2 _filterStickPos = Vector2.zero;
        
        private int _buttonDownCount = 0;

        private Vector3 _latestButtonPos;
        private Quaternion _latestButtonRot;

        private readonly ArcadeStickHandIkState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        private readonly ArcadeStickHandIkState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState;

        public ArcadeStickHandIKGenerator(
            HandIkGeneratorDependency dependency, 
            IVRMLoadable vrmLoadable,
            ArcadeStickProvider stickProvider) : base(dependency)
        {
            _stickProvider = stickProvider;
            _leftHandState = new ArcadeStickHandIkState(this, ReactedHand.Left);
            _rightHandState = new ArcadeStickHandIkState(this, ReactedHand.Right);

            //モデルロード時、身長を参照することで「コントローラの移動オフセットはこんくらいだよね」を初期化
            vrmLoadable.VrmLoaded += info =>
            {
                var h = info.controlRig.GetBoneTransform(HumanBodyBones.Head);
                var f = info.controlRig.GetBoneTransform(HumanBodyBones.LeftFoot);
                CacheHandOffsets(info.controlRig);
            };

            dependency.Events.MoveLeftGamepadStick += v =>
            {
                LeftStick(v);
                if (dependency.Config.GamepadMotionMode.Value == GamepadMotionModes.ArcadeStick)
                {
                    _leftHandState.RaiseRequest();
                }
            };
            
            
            dependency.Events.GamepadButtonStick += pos =>
            {
                if (dependency.Config.WordToMotionDevice.Value != WordToMotionDeviceAssign.Gamepad && 
                    dependency.Config.GamepadMotionMode.Value == GamepadMotionModes.ArcadeStick)
                {
                    ButtonStick(pos);
                    _leftHandState.RaiseRequest();
                }
            };

            
            dependency.Events.GamepadButtonDown += key =>
            {
                ButtonDown(key);
                
                if (dependency.Config.WordToMotionDevice.Value == WordToMotionDeviceAssign.Gamepad || 
                    dependency.Config.IsAlwaysHandDown.Value || 
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.ArcadeStick
                    )
                {
                    return;
                }
                
                _rightHandState.RaiseRequest();
                if (dependency.Config.RightTarget.Value == HandTargetType.ArcadeStick)
                {
                    dependency.Reactions.ArcadeStickFinger.ButtonDown(key);
                    if (CheckKeySupportReactionEffect(key))
                    {
                        var (pos, rot) = _stickProvider.GetRightHandRaw(key);
                        dependency.Reactions.ParticleStore.RequestArcadeStickParticleStart(pos, rot);
                    }
                }
            };

            dependency.Events.GamepadButtonUp += key =>
            {
                ButtonUp(key); 

                if (dependency.Config.WordToMotionDevice.Value == WordToMotionDeviceAssign.Gamepad || 
                    dependency.Config.IsAlwaysHandDown.Value ||
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.ArcadeStick)
                {
                    return;
                }

                if (dependency.Config.RightTarget.Value == HandTargetType.ArcadeStick)
                {
                    dependency.Reactions.ArcadeStickFinger.ButtonUp(key);
                }
            };
        }

        private bool CheckKeySupportReactionEffect(GamepadKey key)
        {
            //NOTE: 意図はコメントアウトしてるほうのが近いが、数値比較のほうがシンプルなので…
            return key >= GamepadKey.A && key <= GamepadKey.LTrigger;
            // return 
            //     key is GamepadKey.A || key is GamepadKey.B || key is GamepadKey.X || key is GamepadKey.Y ||
            //     key is GamepadKey.LTrigger || key is GamepadKey.RTrigger || 
            //     key is GamepadKey.LShoulder || key is GamepadKey.RShoulder;
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

        private void ButtonDown(GamepadKey key)
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
        
        private void ButtonUp(GamepadKey key)
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

        private void LeftStick(Vector2 stickPos)
        {
            _rawStickPos = stickPos;
        }

        private void ButtonStick(Vector2Int buttonStickPos)
        {
            _rawStickPos = NormalizedStickPos(buttonStickPos);

            Vector2 NormalizedStickPos(Vector2Int v)
            {
                const float factor = 1.0f / 32768.0f;
                return new Vector2(v.x * factor, v.y * factor);
            }
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
            
            //NOTE: 指が一部なくてもエラーにはならないが、指の一部だけが欠けていると計算としてはかなり崩れる。
            //指が全部 or 全部あるモデルが大多数派であると考えての実装になっています
            for (int i = 0; i < intermediateBones.Length; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                
                //親指以外はすべてが同一軸に曲がる(FingerControllerがそうなってる)ので割と簡単に計算可能
                
                //3つの関節をすべて30度曲げたときの指先位置について、およそ以下で見積もれる
                // - 水平方向 : だいたい関節1つぶん手前になる(= intermediateBones[i].position - rightWristPos)
                // - 垂直方向 : だいたい関節2つぶん下に下がる
                _wristToFingerOffsets[i] =
                    (proximalBones[i] != null && intermediateBones[i] != null && distalBones[i] != null)
                        ? intermediateBones[i].position - rightWristPos - 
                          Vector3.up * (intermediateBones[i].localPosition.magnitude + distalBones[i].localPosition.magnitude) 
                        : Vector3.zero;
            }
            
            //親指は付け根の関節の回転方向が違うので少し見積もり方法を変えます…めんどくさっ！
            // - 水平方向 :「長さはそのままだが、人差し指の下に指が潜り込んでいる」みたいな値を作って使う
            // - 垂直方向 : 人差し指に完全に合わす
            //   つまり、AボタンとYボタンを順に押すとき手の動きがほぼ無くなるようにする
            if (proximalBones[0] != null && intermediateBones[0] != null && distalBones[0] != null &&
                proximalBones[1] != null && intermediateBones[1] != null && distalBones[1] != null)
            {
                _wristToFingerOffsets[0] =
                    (proximalBones[1].position - rightWristPos).normalized * (distalBones[0].position - rightWristPos).magnitude -
                    Vector3.up * (intermediateBones[1].localPosition.magnitude + distalBones[1].localPosition.magnitude);
            }
            else
            {
                _wristToFingerOffsets[0] = Vector3.zero;
            }
            


        }

        private void EnterState(ReactedHand hand)
        {
            if (hand == ReactedHand.Left)
            {
                Dependency.Reactions.ArcadeStickFinger.GripLeftHand();
            }
            else if (hand == ReactedHand.Right)
            {
                Dependency.Reactions.ArcadeStickFinger.GripRightHand();
            }
        }

        private void QuitState(ReactedHand hand)
        {
            if (hand == ReactedHand.Left)
            {
                Dependency.Reactions.ArcadeStickFinger.ReleaseLeftHand();
            }
            else if (hand == ReactedHand.Right)
            {
                Dependency.Reactions.ArcadeStickFinger.ReleaseRightHand();
            } 
        }


        //NOTE: private classだし…ということでバリバリに相互参照します
        private class ArcadeStickHandIkState : IHandIkState
        {
            public ArcadeStickHandIkState(ArcadeStickHandIKGenerator parent, ReactedHand hand)
            {
                _parent = parent;
                Hand = hand;
                _data = Hand == ReactedHand.Right ? _parent._rightHand : _parent._leftHand;
            }
            private readonly ArcadeStickHandIKGenerator _parent;
            private readonly IIKData _data;

            public bool SkipEnterIkBlend => false;

            public void RaiseRequest() => RequestToUse?.Invoke(this);

            public Vector3 Position => _data.Position;
            public Quaternion Rotation => _data.Rotation;
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.ArcadeStick;
            public event Action<IHandIkState> RequestToUse;
            public void Enter(IHandIkState prevState)
            {
                _parent.EnterState(Hand);
            }
            public void Quit(IHandIkState nextState)
            {
                _parent.QuitState(Hand);
            }
        }
    }
}
