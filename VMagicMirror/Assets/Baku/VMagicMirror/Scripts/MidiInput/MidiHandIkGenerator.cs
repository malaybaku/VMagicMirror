using System.Collections;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// MIDI入力から手のIK動作を作るすごいやつだよ
    /// </summary>
    public class MidiHandIkGenerator : MonoBehaviour
    {
        [SerializeField] private MidiControllerProvider provider = null;

        //NOTE: キーの押し+離しで2モーションぶんの長さです
        [SerializeField] private float noteMotionDuration = 0.3f;
        
        [SerializeField] private float knobMotionLerpFactor = 12.0f;
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        private Coroutine _leftHandCoroutine = null;
        private Coroutine _rightHandCoroutine = null;

        public IIKGenerator RightHand => _rightHand;
        public IIKGenerator LeftHand => _leftHand;
        
        public float WristToTipLength { get; set; } = 0.12f;
        public float HandOffsetAlways { get; set; } = 0.03f;
        public float HandOffsetAfterKeyDown { get; set; } = 0.02f;

        private bool _isLeftHandOnKnob = false;
        private bool _isRightHandOnKnob = false;
        private MidiKnobTargetData _leftHandKnobTarget;
        private MidiKnobTargetData _rightHandKnobTarget;

        private void Start()
        {
            //NOTE: ここは初期位置をふっとばさないための処置なので、あんまり高精度ではないです
            KnobValueChange(provider.LeftKnobCenterIndex, 0);
            KnobValueChange(provider.RightKnobCenterIndex, 0);
        }
        
        private void Update()
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
                    knobMotionLerpFactor * Time.deltaTime
                );
                _leftHand.Rotation = Quaternion.Slerp(
                    _leftHand.Rotation,
                    leftHandRot,
                    knobMotionLerpFactor * Time.deltaTime
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
                    knobMotionLerpFactor * Time.deltaTime
                );
                _rightHand.Rotation = Quaternion.Slerp(
                    _rightHand.Rotation,
                    rightHandRot,
                    knobMotionLerpFactor * Time.deltaTime
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
            var data = provider.GetKnobTargetData(knobNumber, value);
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
            var data = provider.GetNoteTargetData(noteNumber);
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
            while (Time.time - start < noteMotionDuration)
            {
                float rate = (Time.time - start) / noteMotionDuration;

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
    }
}
