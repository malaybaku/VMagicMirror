using System;
using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary> MIDI入力から手のIK動作を作るすごいやつだよ </summary>
    public sealed class MidiHandIkGenerator : HandIkGeneratorBase
    {
        //NOTE: キーの押し+離しで2モーションぶんの長さです
        private const float NoteMotionDuration = 0.3f;
        private const float KnobMotionLerpFactor = 12.0f;
        
        private readonly MidiControllerProvider _provider;
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        private Coroutine _leftHandCoroutine = null;
        private Coroutine _rightHandCoroutine = null;

        public float WristToTipLength { get; set; } = 0.12f;
        public float HandOffsetAlways { get; set; } = 0.03f;
        public float HandOffsetAfterKeyDown { get; set; } = 0.02f;

        private bool _isLeftHandOnKnob = false;
        private bool _isRightHandOnKnob = false;
        private MidiKnobTargetData _leftHandKnobTarget;
        private MidiKnobTargetData _rightHandKnobTarget;

        private readonly MidiHandIkState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        private readonly MidiHandIkState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState;

        public MidiHandIkGenerator(HandIkGeneratorDependency dependency, MidiControllerProvider provider)
            :base(dependency)
        {
            _provider = provider;
            _leftHandState = new MidiHandIkState(this, ReactedHand.Left);
            _rightHandState = new MidiHandIkState(this, ReactedHand.Right);

            dependency.Events.NoteOn += noteNumber =>
            {
                var (hand, pos) = NoteOn(noteNumber);
                if (hand == ReactedHand.Left)
                {
                    _leftHandState.RaiseRequest();
                }
                else
                {
                    _rightHandState.RaiseRequest();
                }
                dependency.Reactions.ParticleStore.RequestMidiParticleStart(pos);
            };

            dependency.Events.KnobValueChange += (knobNumber, value) =>
            {
                var hand = KnobValueChange(knobNumber, value);
                if (hand == ReactedHand.Left)
                {
                    _leftHandState.RaiseRequest();
                }
                else
                {
                    _rightHandState.RaiseRequest();
                }
            };
        }

        public override void Start()
        {
            //NOTE: ここは初期位置をふっとばさないための処置なので、あんまり高精度ではないです
            KnobValueChange(_provider.LeftKnobCenterIndex, 0);
            KnobValueChange(_provider.RightKnobCenterIndex, 0);
        }
        
        public override void Update()
        {
            if (_isLeftHandOnKnob)
            {
                Vector3 leftHandPos =
                    _leftHandKnobTarget.position -
                    _leftHandKnobTarget.knobTransform.forward * WristToTipLength;
                Quaternion leftHandRot = 
                    _leftHandKnobTarget.knobTransform.rotation * 
                    Quaternion.AngleAxis(90, Vector3.up);

                _leftHand.Position = Vector3.Lerp(
                    _leftHand.Position,
                    leftHandPos,
                    KnobMotionLerpFactor * Time.deltaTime
                );
                _leftHand.Rotation = Quaternion.Slerp(
                    _leftHand.Rotation,
                    leftHandRot,
                    KnobMotionLerpFactor * Time.deltaTime
                );
            }

            if (_isRightHandOnKnob)
            {
                Vector3 rightHandPos =
                    _rightHandKnobTarget.position -
                    _rightHandKnobTarget.knobTransform.forward * WristToTipLength;
                Quaternion rightHandRot = 
                    _rightHandKnobTarget.knobTransform.rotation * 
                    Quaternion.AngleAxis(-90, Vector3.up);

                _rightHand.Position = Vector3.Lerp(
                    _rightHand.Position,
                    rightHandPos,
                    KnobMotionLerpFactor * Time.deltaTime
                );
                _rightHand.Rotation = Quaternion.Slerp(
                    _rightHand.Rotation,
                    rightHandRot,
                    KnobMotionLerpFactor * Time.deltaTime
                );
            }
        }
        
        /// <summary>
        /// ノブの値が変わったとき呼び出します。
        /// </summary>
        /// <param name="knobNumber"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ReactedHand KnobValueChange(int knobNumber, float value)
        {
            var data = _provider.GetKnobTargetData(knobNumber, value);
            if (data.isLeftHandPreferred)
            {
                _isLeftHandOnKnob = true;
                if (_leftHandCoroutine != null)
                {
                    StopCoroutine(_leftHandCoroutine);
                }
                _leftHandKnobTarget = data;
                return ReactedHand.Left;
            }
            else
            {
                _isRightHandOnKnob = true;
                if (_rightHandCoroutine != null)
                {
                    StopCoroutine(_rightHandCoroutine);
                }
                _rightHandKnobTarget = data;
                return ReactedHand.Right;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="noteNumber"></param>
        /// <returns></returns>
        public (ReactedHand, Vector3) NoteOn(int noteNumber)
        {
            var data = _provider.GetNoteTargetData(noteNumber);
            var notePos = data.positionWithOffset
                          - data.noteTransform.forward * WristToTipLength
                          + data.noteTransform.up * HandOffsetAlways;
            var upDif = data.noteTransform.up * HandOffsetAfterKeyDown;
            
            if (data.IsLeftHandPreffered)
            {
                _isLeftHandOnKnob = false;
                var noteRot = data.noteTransform.rotation * Quaternion.AngleAxis(90, Vector3.up);
                SetLeftHandCoroutine(MoveToNote(_leftHand, notePos, noteRot, upDif));
                return (ReactedHand.Left, data.position);
            }
            else
            {
                _isRightHandOnKnob = false;
                var noteRot = data.noteTransform.rotation * Quaternion.AngleAxis(-90, Vector3.up);
                SetRightHandCoroutine(MoveToNote(_rightHand, notePos, noteRot, upDif));
                return (ReactedHand.Right, data.position);
            }
        }

        private IEnumerator MoveToNote(IKDataRecord ik, Vector3 notePosition, Quaternion noteRot, Vector3 upDiff)
        {
            float start = Time.time;
            while (Time.time - start < NoteMotionDuration)
            {
                float rate = (Time.time - start) / NoteMotionDuration;

                if (rate < 0.5f)
                {
                    //接近動作: このとき既存位置を考慮してウェイトをいじる点に注意
                    Vector3 target = Vector3.Lerp(notePosition + upDiff, notePosition, rate * 2.0f);
                    ik.Position = Vector3.Lerp(ik.Position, target, rate * 2.0f);
                    ik.Rotation = Quaternion.Slerp(ik.Rotation, noteRot, rate * 2.0f);
                }
                else
                {
                    //上がる動作
                    ik.Position = Vector3.Lerp(
                        notePosition, 
                        notePosition + upDiff,
                        1 - 4.0f * (rate - 1.0f) * (rate - 1.0f)
                        );
                    ik.Rotation = noteRot;
                }

                yield return null;
            }
            
            //最後はぴったり揃えて終了
            ik.Position = notePosition + upDiff;
            ik.Rotation = noteRot;
        }
        
        private void SetLeftHandCoroutine(IEnumerator enumerator)
        {
            if (_leftHandCoroutine != null)
            {
                StopCoroutine(_leftHandCoroutine);
            }
            _leftHandCoroutine = StartCoroutine(enumerator);
        }

        private void SetRightHandCoroutine(IEnumerator enumerator)
        {
            if (_rightHandCoroutine != null)
            {
                StopCoroutine(_rightHandCoroutine);
            }

            _rightHandCoroutine = StartCoroutine(enumerator);
        }

        private sealed class MidiHandIkState : IHandIkState
        {
            public MidiHandIkState(MidiHandIkGenerator parent, ReactedHand hand)
            {
                _parent = parent;
                Hand = hand;
                _data = hand == ReactedHand.Right ? _parent._rightHand : _parent._leftHand;
            }

            private readonly MidiHandIkGenerator _parent;
            private readonly IIKData _data;

            public bool SkipEnterIkBlend => false;
            public void RaiseRequest() => RequestToUse?.Invoke(this);
            
            public Vector3 Position => _data.Position;
            public Quaternion Rotation => _data.Rotation;
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.MidiController;
            public event Action<IHandIkState> RequestToUse;

            //NOTE: Quitのとき指を開放
            public void Enter(IHandIkState prevState)
            {
            }

            public void Quit(IHandIkState nextState)
            {
            }
        }
        
    }
}
