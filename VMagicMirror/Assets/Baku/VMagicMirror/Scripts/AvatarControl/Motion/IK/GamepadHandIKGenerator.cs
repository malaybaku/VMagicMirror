using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
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
        
        public GamepadHandIKGenerator(
            MonoBehaviour coroutineResponder, 
            LipSyncIntegrator lipSyncIntegrator,
            GamepadProvider gamepadProvider,
            GamepadHandIkGeneratorSetting setting) : base(coroutineResponder)
        {
            _lipSync = lipSyncIntegrator;
            _gamePad = gamepadProvider;
            _setting = setting;
        }

        private readonly InputBasedJitter _inputJitter = new InputBasedJitter();
        private readonly TimeBasedJitter _timeJitter = new TimeBasedJitter();
        private readonly VoiceBasedJitter _voiceJitter = new VoiceBasedJitter();

        private readonly LipSyncIntegrator _lipSync;
        private readonly GamepadProvider _gamePad;
        private readonly GamepadHandIkGeneratorSetting _setting;
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        private const float ButtonDownAnimationY = 0.01f;

        private Vector2 _rawStickPos = Vector2.zero;
        private Vector2 _filterStickPos = Vector2.zero;

        private float _offsetY = 0;
        private int _buttonDownCount = 0;
        private GamepadLeanModes _leanMode = GamepadLeanModes.GamepadLeanLeftStick;

        public bool ReverseGamepadStickLeanHorizontal { get; set; } = false;
        public bool ReverseGamepadStickLeanVertical { get; set; } = false;

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
        
        public void ButtonDown(GamepadKey key)
        {
            _inputJitter.AddButtonInput();
            if (GamepadProvider.IsSideKey(key))
            {
                return;
            }

            _buttonDownCount++;
        }

        public void ButtonUp(GamepadKey key)
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
        
        public void LeftStick(Vector2 stickPos)
        {
            _inputJitter.SetLeftStickPos(stickPos);
            if (_leanMode == GamepadLeanModes.GamepadLeanLeftStick)
            {
                ApplyStickPosition(stickPos);
            }
        }

        public void RightStick(Vector2 stickPos)
        {
            _inputJitter.SetRightStickPos(stickPos);
            if (_leanMode == GamepadLeanModes.GamepadLeanRightStick)
            {
                ApplyStickPosition(stickPos);
            }
        }

        public void ButtonStick(Vector2Int buttonStickPos)
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

        public override void Start()
        {
            //とりあえず初期位置までゲームコントローラIKの場所を持ち上げておく:
            //やらないとIK位置が0,0,0のままになって良くない
            ApplyStickPosition(Vector2.zero);
        }
        
        public override void Update()
        {
            UpdateButtonDownYOffset();            
            _voiceJitter.Update(_lipSync.VoiceRate, Time.deltaTime);
            _timeJitter.Update(Time.deltaTime);
            _inputJitter.Update(Time.deltaTime);
            
            //とりあえずLerp
            _filterStickPos = Vector2.Lerp(_filterStickPos, _rawStickPos, SpeedFactor * Time.deltaTime);

            //いろいろな理由で後処理が載るのを計算
            var posOffset =
                Vector3.up * _offsetY +
                _setting.imageBasedBodyMotion.BodyIkOffset * BodyMotionToGamepadPosApplyFactor +
                _timeJitter.PosOffset +
                _voiceJitter.PosOffset +
                _inputJitter.PosOffset;

            var rotOffset = _timeJitter.Rotation * _voiceJitter.Rotation * _inputJitter.Rotation;

            //フィルタリングとオフセットを考慮してゲームパッドの位置自体をちょっと調整
            _gamePad.SetFilteredPositionAndRotation(_filterStickPos, posOffset, rotOffset);
            
            //調整後のコントローラ位置に合わせて手を持っていく
            var (leftPos, leftRot) = _gamePad.GetLeftHand();
            var (rightPos,rightRot) = _gamePad.GetRightHand();
            _leftHand.Position = leftPos;
            _leftHand.Rotation = leftRot;
            _rightHand.Position = rightPos;
            _rightHand.Rotation = rightRot;
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
        sealed class InputBasedJitter
        {
            public Vector3 PosOffset { get; private set; }
            public Quaternion Rotation { get; private set; }

            private const float TargetChangeProbability = 0.25f;
            private static readonly Vector3 MotionScale = new Vector3(0.01f, 0.02f, 0.01f);
            private static readonly Vector3 RotScaleEuler = new Vector3(8f, 1f, 8f);
            private const float TimeLerpFactor = 6.0f;

            //ガチャガチャしてるとコントローラが動きやすい、という処理で使うしきい値
            private const float StickDiffToChangeTargetThreshold = 0.8f;
            private const float StickDiffDecreaseRate = 0.1f;

            private float _stickDiffCount = 0f;
            
            private Vector3 _offsetTarget = Vector3.zero;
            private Quaternion _rotationTarget = Quaternion.identity;

            private Vector2 _leftPos = Vector2.zero;
            private Vector2 _rightPos = Vector2.zero;
            
            public void AddButtonInput()
            {
                TryChangeTarget();
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
                
                PosOffset = Vector3.Lerp(PosOffset, _offsetTarget, TimeLerpFactor * deltaTime);
                Rotation = Quaternion.Slerp(Rotation, _rotationTarget, TimeLerpFactor * deltaTime);
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
        sealed class TimeBasedJitter
        {
            public Vector3 PosOffset { get; private set; }
            public Quaternion Rotation { get; private set; }

            private const float TargetUpdateIntervalMin = 2f;
            private const float TargetUpdateIntervalMax = 6f;
            
            private static readonly Vector3 MotionScale = new Vector3(0.01f, 0.03f, 0.01f);
            private static readonly Vector3 RotScaleEuler = new Vector3(5f, 0, 5f);

            private const float TimeLerpFactor = 3f;

            private float _timeCount = 0f;
            private float _interval = TargetUpdateIntervalMax;
            private Vector3 _offsetTarget = Vector3.zero;
            private Quaternion _rotationTarget = Quaternion.identity;
            
            public void Update(float deltaTime)
            {
                PosOffset = Vector3.Lerp(PosOffset, _offsetTarget, TimeLerpFactor * deltaTime);
                Rotation = Quaternion.Slerp(Rotation, _rotationTarget, TimeLerpFactor * deltaTime);

                _timeCount += deltaTime;
                if (_timeCount < _interval)
                {
                    return;
                }

                _timeCount = 0;
                _interval = Random.Range(TargetUpdateIntervalMin, TargetUpdateIntervalMax);
                _offsetTarget = RandomVec(MotionScale);
                _rotationTarget = Quaternion.Euler(RandomVec(RotScaleEuler));
            }
        } 
        
        
        /// <summary> 声が出てるときにかかる補正。Y方向に上がる+ピッチのみ動かす、ろくろ回し？的な補正 </summary>
        sealed class VoiceBasedJitter
        {
            public Vector3 PosOffset { get; private set; }
            public Quaternion Rotation { get; private set; }

            private const float TimeLerpFactor = 3f;
            private const float PosOffsetMax = 0.02f;
            private const float PitchMaxDeg = 10f;

            private float _rate = 0f;

            public void Update(float voiceRate, float deltaTime)
            {
                _rate = Mathf.Lerp(_rate, voiceRate, TimeLerpFactor * deltaTime);
                PosOffset = new Vector3(0, _rate * PosOffsetMax);
                Rotation = Quaternion.AngleAxis(-_rate * PitchMaxDeg, Vector3.right);
            }
        }
        
        
        private static Vector3 RandomVec(Vector3 scale)
        {
            return new Vector3(
                Random.Range(-scale.x, scale.x),
                Random.Range(-scale.y, scale.y),
                Random.Range(-scale.z, scale.z)
            );
        }
    }
    
}
