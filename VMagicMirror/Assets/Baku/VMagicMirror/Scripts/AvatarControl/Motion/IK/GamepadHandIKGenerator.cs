using System;
using UnityEngine;
using Random = UnityEngine.Random;
using UniRx;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// ゲームパッドの入力状況に対して望ましい腕IKを指定するやつ。
    /// 従来版と違い、小さなゲームパッドを握っている状態を再現する狙いで実装している
    /// </summary>
    public class GamepadHandIKGenerator : HandIkGeneratorBase
    {
        [Serializable]
        public class GamepadHandIkGeneratorSetting
        {
            public ImageBasedBodyMotion imageBasedBodyMotion;            
        }

        // ゲームパッド全体を動かす速度ファクタ
        private const float SpeedFactor = 3.0f;
        
        // ボタンを押す/押してないに依存して手を上下させる動きの速度ファクタ
        private const float ButtonDownSpeedFactor = 8f;
        
        //体が動いた量をゲームパッドの移動量に反映するファクター
        private const float BodyMotionToGamepadPosApplyFactor = 0.5f;

        private const float OffsetResetLerpFactor = 6f;

        //posOffsetのサイズを身長ベースで補正するのに使う、足から頭ボーンまでの高さ
        private const float ReferenceHeight = 1.3f;
        
        private const float ButtonDownAnimationY = 0.01f;
        
        private readonly GamepadHandIkState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        private readonly GamepadHandIkState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState;
        
        private readonly InputBasedJitter _inputJitter = new InputBasedJitter();
        private readonly TimeBasedJitter _timeJitter = new TimeBasedJitter();

        private readonly GamepadProvider _gamePad;
        private readonly GamepadHandIkGeneratorSetting _setting;
        private readonly WaitingBodyMotion _waitingBody;
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        private readonly IKDataRecord _rightHand = new IKDataRecord();

        private float _posOffsetScale = 1.0f;

        private Vector2 _rawStickPos = Vector2.zero;
        private Vector2 _filterStickPos = Vector2.zero;

        private Vector3 _posOffset = Vector3.zero;
        private Quaternion _rotOffset = Quaternion.identity;

        private float _offsetY = 0;
        private int _buttonDownCount = 0;
        private GamepadLeanModes _leanMode = GamepadLeanModes.GamepadLeanLeftStick;

        // 右手か左手のうち少なくとも片方はコントローラを掴んでいるか、というフラグ
        private bool _handIsOnController;
        
        public bool ReverseGamepadStickLeanHorizontal { get; set; } = false;
        public bool ReverseGamepadStickLeanVertical { get; set; } = false;

        
        public GamepadHandIKGenerator(
            HandIkGeneratorDependency dependency, 
            IVRMLoadable vrmLoadable,
            WaitingBodyMotion waitingBodyMotion,
            GamepadProvider gamepadProvider,
            GamepadHandIkGeneratorSetting setting) : base(dependency)
        {
            _gamePad = gamepadProvider;
            _setting = setting;
            _waitingBody = waitingBodyMotion;

            _leftHandState = new GamepadHandIkState(this, ReactedHand.Left);
            _rightHandState = new GamepadHandIkState(this, ReactedHand.Right);

            //モデルロード時、身長を参照することで「コントローラの移動オフセットはこんくらいだよね」を初期化
            vrmLoadable.VrmLoaded += info =>
            {
                var h = info.controlRig.GetBoneTransform(HumanBodyBones.Head);
                var f = info.controlRig.GetBoneTransform(HumanBodyBones.LeftFoot);
                float height = h.position.y - f.position.y;
                _posOffsetScale = Mathf.Clamp(height / ReferenceHeight, 0.1f, 5f);
            };

            dependency.Events.MoveLeftGamepadStick += v =>
            {
                if (dependency.Config.IsAlwaysHandDown.Value || 
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.Gamepad)
                {
                    return;
                }

                LeftStick(v);
                dependency.Reactions.GamepadFinger.LeftStick(v);
                _leftHandState.RaiseRequest();
            };

            dependency.Events.MoveRightGamepadStick += v =>
            {
                if (dependency.Config.IsAlwaysHandDown.Value || 
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.Gamepad)
                {
                    return;
                }

                RightStick(v);
                dependency.Reactions.GamepadFinger.RightStick(v);
                _rightHandState.RaiseRequest();
            };

            dependency.Events.GamepadButtonDown += key =>
            {
                ButtonDown(key);
                if (dependency.Config.IsAlwaysHandDown.Value || 
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.Gamepad)
                {
                    return;
                }
                
                var hand = GamepadProvider.GetPreferredReactionHand(key);
                if (hand == ReactedHand.Left)
                {
                    _leftHandState.RaiseRequest();
                    dependency.Reactions.GamepadFinger.ButtonDown(key);
                }
                else if (hand == ReactedHand.Right)
                {
                    _rightHandState.RaiseRequest();
                    dependency.Reactions.GamepadFinger.ButtonDown(key);
                }
            };

            dependency.Events.GamepadButtonUp += key =>
            {
                ButtonUp(key);
                if (dependency.Config.IsAlwaysHandDown.Value || 
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.Gamepad)
                {
                    return;
                }
                
                var hand = GamepadProvider.GetPreferredReactionHand(key);
                if (hand == ReactedHand.Left)
                {
                    _leftHandState.RaiseRequest();
                    dependency.Reactions.GamepadFinger.ButtonUp(key);
                }
                else if (hand == ReactedHand.Right)
                {
                    _rightHandState.RaiseRequest();
                    dependency.Reactions.GamepadFinger.ButtonUp(key);
                }
            };

            dependency.Events.GamepadButtonStick += pos =>
            {
                if (dependency.Config.IsAlwaysHandDown.Value || 
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.Gamepad)
                {
                    return;
                }

                ButtonStick(pos);
                _leftHandState.RaiseRequest();
            };

            dependency.Config.LeftTarget
                .Subscribe(t =>
                {
                    _handIsOnController =
                        t == HandTargetType.Gamepad ||
                        dependency.Config.RightTarget.Value == HandTargetType.Gamepad;
                })
                .AddTo(dependency.Component);

            dependency.Config.RightTarget
                .Subscribe(t =>
                {
                    _handIsOnController =
                        t == HandTargetType.Gamepad ||
                        dependency.Config.LeftTarget.Value == HandTargetType.Gamepad;
                })
                .AddTo(dependency.Component);
            
        }
        
        public void SetGamepadLeanMode(string leanModeName)
        {
            _leanMode =
                Enum.TryParse<GamepadLeanModes>(leanModeName, out var result) ?
                    result :
                    GamepadLeanModes.GamepadLeanNone;

            if (_leanMode == GamepadLeanModes.GamepadLeanNone)
            {
                ApplyStickPosition(Vector2Int.zero);
            }
        }
        
        public override void Start()
        {
            //とりあえず初期位置までゲームコントローラIKの場所を持ち上げておく:
            //やらないとIK位置が0,0,0のままになって良くない
            ApplyStickPosition(Vector2.zero);
        }
        
        public override void Update()
        {
            UpdateButtonDownYOffset();            
            _timeJitter.Update(Time.deltaTime, _waitingBody.Phase);
            _inputJitter.Update(Time.deltaTime);
            
            //とりあえずLerp
            _filterStickPos = Vector2.Lerp(_filterStickPos, _rawStickPos, SpeedFactor * Time.deltaTime);

            //いろいろな理由で後処理が載るのを計算: ただし手が乗っかってないとリセットされていく
            if (_handIsOnController)
            {
                _posOffset =
                    _setting.imageBasedBodyMotion.BodyIkOffset * BodyMotionToGamepadPosApplyFactor +
                    _posOffsetScale * (
                        Vector3.up * _offsetY +
                        _timeJitter.PosOffset +
                        _inputJitter.PosOffset
                    );

                _rotOffset = _timeJitter.Rotation * _inputJitter.Rotation;
            }
            else
            {
                _posOffset = Vector3.Lerp(_posOffset, Vector3.zero, OffsetResetLerpFactor * Time.deltaTime);
                _rotOffset = Quaternion.Slerp(_rotOffset, Quaternion.identity, OffsetResetLerpFactor * Time.deltaTime);
            }
            

            //フィルタリングとオフセットを考慮してゲームパッドの位置自体を調整
            _gamePad.SetFilteredPositionAndRotation(_filterStickPos, _posOffset, _rotOffset);
            
            //調整後のコントローラ位置に合わせて手を持っていく
            var (leftPos, leftRot) = _gamePad.GetLeftHand();
            var (rightPos,rightRot) = _gamePad.GetRightHand();
            _leftHand.Position = leftPos;
            _leftHand.Rotation = leftRot;
            _rightHand.Position = rightPos;
            _rightHand.Rotation = rightRot;
        }


        private void ButtonDown(GamepadKey key)
        {
            _inputJitter.AddButtonInput();
            if (GamepadProvider.IsSideKey(key))
            {
                return;
            }

            _buttonDownCount++;
        }

        private void ButtonUp(GamepadKey key)
        {
            if (GamepadProvider.IsSideKey(key))
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
            _inputJitter.SetLeftStickPos(stickPos);
            if (_leanMode == GamepadLeanModes.GamepadLeanLeftStick)
            {
                ApplyStickPosition(stickPos);
            }
        }

        private void RightStick(Vector2 stickPos)
        {
            _inputJitter.SetRightStickPos(stickPos);
            if (_leanMode == GamepadLeanModes.GamepadLeanRightStick)
            {
                ApplyStickPosition(stickPos);
            }
        }

        private void ButtonStick(Vector2Int buttonStickPos)
        {
            if (_leanMode == GamepadLeanModes.GamepadLeanLeftButtons)
            {
                ApplyStickPosition(NormalizedStickPos(buttonStickPos));
            }
        }
        
        private void ApplyStickPosition(Vector2 stickPos)
        {
            var pos = new Vector2(
                stickPos.x * (ReverseGamepadStickLeanHorizontal ? -1f : 1f),
                stickPos.y * (ReverseGamepadStickLeanVertical ? -1f : 1f)
                );
            _rawStickPos = pos;
        }

        private static Vector2 NormalizedStickPos(Vector2Int v)
        {
            const float factor = 1.0f / 32768.0f;
            return new Vector2(v.x * factor, v.y * factor);
        }
        
        private void UpdateButtonDownYOffset()
        {
            float offsetGoal =
                (_buttonDownCount > 0) ? -ButtonDownAnimationY : 0;

            _offsetY = Mathf.Lerp(
                _offsetY,
                offsetGoal,
                ButtonDownSpeedFactor * Time.deltaTime
                );
        }

        private static Vector3 RandomVec(Vector3 scale)
        {
            return new Vector3(
                Random.Range(-scale.x, scale.x),
                Random.Range(-scale.y, scale.y),
                Random.Range(-scale.z, scale.z)
            );
        }


        /// <summary> どの入力をBodyLeanの値に反映するか考慮するやつ </summary>
        private enum GamepadLeanModes
        {
            GamepadLeanNone,
            GamepadLeanLeftButtons,
            GamepadLeanLeftStick,
            GamepadLeanRightStick,
        }

        //色々な理由による手の動き
        
        /// <summary> コントローラ入力があると動くぶんの補正。細かくXYZ全方向に動き、やや素早い </summary>
        private sealed class InputBasedJitter
        {
            public Vector3 PosOffset { get; private set; }
            public Quaternion Rotation { get; private set; }

            private const float TargetChangeProbability = 0.4f;
            private static readonly Vector3 MotionScale = new Vector3(0.01f, 0.02f, 0.01f);
            private static readonly Vector3 RotScaleEuler = new Vector3(6f, 1f, 6f);
            private const float TimeLerpFactor = 4.0f;
            
            //ボタンを素早く押すとコントローラが動きやすい、という処理で使うしきい値
            private const float ButtonDiffToChangeTargetThreshold = 1.5f;
            private const float ButtonDiffDecreaseRate = 0.4f;

            //スティックをガチャガチャしてるとコントローラが動きやすい、という処理で使うしきい値
            private const float StickDiffToChangeTargetThreshold = 0.8f;
            private const float StickDiffDecreaseRate = 0.1f;

            //入力がない時間がこれだけ続いたら位置を元に戻す。ズレっぱなしも見栄えがしないので
            private const float NoInputCountMax = 6f;
            //入力なしで戻すときはゆっくりと。
            private const float ResetTimeLerpFactor = 2f;

            private float _noInputCount = 0f;
            private float _buttonCount = 0f;
            private float _stickDiffCount = 0f;
            
            private Vector3 _offsetTarget = Vector3.zero;
            private Quaternion _rotationTarget = Quaternion.identity;

            private Vector2 _leftPos = Vector2.zero;
            private Vector2 _rightPos = Vector2.zero;
            
            public void AddButtonInput()
            {
                _noInputCount = 0f;
                _buttonCount += 1f;
                if (_buttonCount > ButtonDiffToChangeTargetThreshold)
                {
                    _buttonCount -= ButtonDiffToChangeTargetThreshold;
                    TryChangeTarget();
                }
            }

            public void SetLeftStickPos(Vector2 pos)
            {
                AddStickInput(Vector2.Distance(_leftPos, pos));
                _leftPos = pos;
            }

            public void SetRightStickPos(Vector2 pos)
            {
                AddStickInput(Vector2.Distance(_rightPos, pos));
                _rightPos = pos;
            }

            private void AddStickInput(float diff)
            {
                _noInputCount = 0f;
                _stickDiffCount += diff;
                if (_stickDiffCount > StickDiffToChangeTargetThreshold)
                {
                    _stickDiffCount -= StickDiffToChangeTargetThreshold;
                    TryChangeTarget();
                }
            }
        
            public void Update(float deltaTime)
            {
                _stickDiffCount -= StickDiffDecreaseRate * deltaTime;
                if (_stickDiffCount < 0) _stickDiffCount = 0f;

                _buttonCount -= ButtonDiffDecreaseRate * deltaTime;
                if (_buttonCount < 0) _buttonCount = 0f;

                if (_noInputCount <= NoInputCountMax)
                {
                    _noInputCount += deltaTime;
                    PosOffset = Vector3.Lerp(PosOffset, _offsetTarget, TimeLerpFactor * deltaTime);
                    Rotation = Quaternion.Slerp(Rotation, _rotationTarget, TimeLerpFactor * deltaTime);
                }
                else
                {
                    PosOffset *= 1.0f - ResetTimeLerpFactor * deltaTime;
                    Rotation = Quaternion.Slerp(Rotation, Quaternion.identity, ResetTimeLerpFactor * deltaTime);
                }
                
            }

            private void TryChangeTarget()
            {
                if (Random.Range(0f, 1f) > TargetChangeProbability)
                {
                    return;
                }
                _offsetTarget = RandomVec(MotionScale);
                _rotationTarget = Quaternion.Euler(RandomVec(RotScaleEuler));
            }
        }

        /// <summary> 純粋に時間依存で動く補正。ほぼYZ平面上に限定で、ゆっくりで、ほどほど動く </summary>
        private sealed class TimeBasedJitter
        {
            public Vector3 PosOffset { get; private set; }
            public Quaternion Rotation { get; private set; }
            
            private const float XInterval = 15f;
            private const float ZInterval = 17f;
            private const float PitchInterval = 19f;
            private const float RollInterval = 22f;
            
            private static readonly Vector3 MotionScale = new Vector3(0.01f, 0.02f, 0.01f);
            private static readonly Vector3 RotScaleEuler = new Vector3(2f, 0, 2f);

            private float _count = 0f;

            public void Update(float deltaTime, float waitingBodyPhase)
            {
                //Yだけ待機モーションの位相をリファレンスにする。これによって呼吸と手揺れがズレながら揃って見栄えがよい
                float yPhase = waitingBodyPhase - 0.23f;
                _count += deltaTime;
                PosOffset = new Vector3(
                    MotionScale.x * Mathf.Sin(_count / XInterval * Mathf.PI * 2f),
                    MotionScale.y * 0.5f * (1f - Mathf.Cos(yPhase * Mathf.PI * 2f)),
                    MotionScale.z * Mathf.Sin(_count / ZInterval * Mathf.PI * 2f)
                    );
                Rotation = Quaternion.Euler(
                    RotScaleEuler.x * Mathf.Sin(_count / PitchInterval * Mathf.PI * 2f),
                    0f,
                    RotScaleEuler.z * Mathf.Sin(_count / RollInterval * Mathf.PI * 2f)
                );
            }
        }

        private class GamepadHandIkState : IHandIkState
        {
            public GamepadHandIkState(GamepadHandIKGenerator parent, ReactedHand hand)
            {
                _parent = parent;
                Hand = hand;
                _data = hand == ReactedHand.Right ? _parent._rightHand : _parent._leftHand;
            }
            private readonly GamepadHandIKGenerator _parent;
            private readonly IIKData _data;

            public bool SkipEnterIkBlend => false;
            public void RaiseRequest() => RequestToUse?.Invoke(this);

            public Vector3 Position => _data.Position;
            public Quaternion Rotation => _data.Rotation;
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.Gamepad;
            public event Action<IHandIkState> RequestToUse;
            
            public void Enter(IHandIkState prevState)
            {
                if (Hand == ReactedHand.Left)
                {
                    _parent.Dependency.Reactions.GamepadFinger.GripLeftHand();
                }
                else
                {
                    _parent.Dependency.Reactions.GamepadFinger.GripRightHand();
                }
            }

            public void Quit(IHandIkState nextState)
            {
                if (Hand == ReactedHand.Left)
                {
                    _parent.Dependency.Reactions.GamepadFinger.ReleaseLeftHand();
                }
                else
                {
                    _parent.Dependency.Reactions.GamepadFinger.ReleaseRightHand();
                }
            }
        }
    }
}
