using System.Collections;
using UnityEngine;
using XinputGamePad;

namespace Baku.VMagicMirror
{
    //TODO: もっと綺麗にボタンとかスティックをあれしてほしいよね

    /// <summary>
    /// ゲームパッドの入力状況に対して望ましい腕IKを指定するやつ
    /// </summary>
    public class GamepadHandIKGenerator : MonoBehaviour
    {
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        //手をあまり厳格にキーボードに沿わせると曲がり過ぎるのでゼロ回転側に寄せるファクター
        private const float WristYawApplyFactor = 0.5f;
        private const float WristYawSpeedFactor = 0.2f;

        #region settings (WPFから飛んでくる想定のもの)

        //この辺のパラメータはキーボードやマウスのIKで使ってる補正値と被っている点に注意
        public float HandToTipLength { get; set; } = 0.1f;
        public float HandToPalmLength { get; set; } = 0.05f;
        public float YOffsetAlways { get; set; } = 0.05f;
        public float YOffsetAfterKeyDown { get; set; } = 0.08f;

        #endregion

        #region settings (Unityで閉じてる想定のもの)

        [SerializeField]
        private GamepadProvider _gamePad = null;

        [SerializeField]
        private AnimationCurve _horizontalApproachCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0.0f, 1, -1, -1),
            new Keyframe(0.5f, 0, -1, 0),
            new Keyframe(1.0f, 0, 0, 0),
        });

        [SerializeField]
        private AnimationCurve _verticalApproachCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0.0f, 1, 0, 0),
            new Keyframe(0.5f, 0, -1, 1),
            new Keyframe(1.0f, 1, 0, 0),
        });

        //高さ方向についてはキー連打時に手が強引な動きに見えないよう、二重に重みづけする
        [SerializeField]
        private AnimationCurve _verticalWeightCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0, 0.2f),
            new Keyframe(0.5f, 1.0f),
        });

        [SerializeField]
        private float _keyboardMotionDuration = 0.25f;

        [SerializeField]
        private float _horizontalSpeedFactor = 0.2f;

        private Vector3 yOffsetAlwaysVec => YOffsetAlways * Vector3.up;

        #endregion

        private Coroutine _leftHandMoveCoroutine = null;
        private Coroutine _rightHandMoveCoroutine = null;

        private int _leftHandPressedGamepadKeyCount = 0;
        private int _rightHandPressedGamepadKeyCount = 0;
        private Vector3 _leftGamepadStickPosition = Vector3.zero;
        private Vector3 _rightGamepadStickPosition = Vector3.zero;

        #region API

        public ReactedHand ButtonDown(XinputKey key)
        {
            Vector3 targetPos = _gamePad.GetButtonPosition(key) + yOffsetAlwaysVec;
            targetPos -= HandToTipLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            if (GamepadProvider.IsLeftHandPreferred(key))
            {
                _leftHandPressedGamepadKeyCount++;
                if (_leftHandPressedGamepadKeyCount == 1)
                {
                    StartLeftHandCoroutine(
                        ButtonDownRoutine(_leftHand, targetPos, true)
                        );
                    return ReactedHand.Left;
                }
            }
            else
            {
                _rightHandPressedGamepadKeyCount++;
                if (_rightHandPressedGamepadKeyCount == 1)
                {
                    StartRightHandCoroutine(
                        ButtonDownRoutine(_rightHand, targetPos, false)
                        );
                    return ReactedHand.Right;
                }
            }

            return ReactedHand.None;
        }

        public ReactedHand ButtonUp(XinputKey key)
        {
            Vector3 targetPos = _gamePad.GetButtonPosition(key) + yOffsetAlwaysVec;
            targetPos -= HandToTipLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            if (GamepadProvider.IsLeftHandPreferred(key))
            {
                _leftHandPressedGamepadKeyCount--;
                if (_leftHandPressedGamepadKeyCount == 0)
                {
                    StartLeftHandCoroutine(
                        ButtonUpRoutine(_leftHand, targetPos, true)
                        );
                    return ReactedHand.Left;
                }
            }
            else
            {
                _rightHandPressedGamepadKeyCount--;
                if (_rightHandPressedGamepadKeyCount == 0)
                {
                    StartRightHandCoroutine(
                        ButtonUpRoutine(_rightHand, targetPos, false)
                        );
                    return ReactedHand.Right;
                }
            }

            return ReactedHand.None;
        }

        public void LeftStick(Vector2 stickPos)
        {
            var targetPos = _gamePad.GetLeftStickPosition(stickPos.x, stickPos.y) + yOffsetAlwaysVec;
            targetPos -= HandToPalmLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            _leftGamepadStickPosition = targetPos;

            StopLeftHandCoroutine();
        }

        public void RightStick(Vector2 stickPos)
        {
            var targetPos = _gamePad.GetRightStickPosition(stickPos.x, stickPos.y) + yOffsetAlwaysVec;
            targetPos -= HandToPalmLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            _rightGamepadStickPosition = targetPos;

            StopRightHandCoroutine();
        }

        #endregion

        private void Update()
        {
            UpdateLeftHand();
            UpdateRightHand();
        }

        private void UpdateLeftHand()
        {
            _leftHand.Position = Vector3.Lerp(
                _leftHand.Position,
                _leftGamepadStickPosition,
                _horizontalSpeedFactor
                );

            var lhRot = Quaternion.Slerp(
                Quaternion.Euler(Vector3.up * (
                    Mathf.Atan2(_leftHand.Position.z, _leftHand.Position.x) *
                    Mathf.Rad2Deg)),
                Quaternion.Euler(Vector3.up * 90),
                WristYawApplyFactor
                );

            _leftHand.Rotation = Quaternion.Slerp(_leftHand.Rotation, lhRot, WristYawSpeedFactor);
        }

        private void UpdateRightHand()
        {
            _rightHand.Position = Vector3.Lerp(
                _rightHand.Position,
                _rightGamepadStickPosition,
                _horizontalSpeedFactor
                );

            var rhRot2 = Quaternion.Slerp(
                Quaternion.Euler(Vector3.up * (
                    -Mathf.Atan2(_rightHand.Position.z, _rightHand.Position.x) *
                    Mathf.Rad2Deg)),
                Quaternion.Euler(Vector3.up * (-90)),
                WristYawApplyFactor
                );
            _rightHand.Rotation = Quaternion.Slerp(_rightHand.Rotation, rhRot2, WristYawSpeedFactor);
        }

        private IEnumerator ButtonDownRoutine(IKDataRecord hand, Vector3 targetPos, bool isLeftHand)
        {
            float startTime = Time.time;
            Vector3 startPos = hand.Position;

            //NOTE: KeyPressRoutineの前半に相当する押下のモーションまででストップ
            float duration = _keyboardMotionDuration * 0.5f;
            while (Time.time - startTime < duration)
            {
                float rate = (Time.time - startTime) / _keyboardMotionDuration;

                Vector3 horizontal = Vector3.Lerp(startPos, targetPos, _horizontalApproachCurve.Evaluate(rate));

                //yのカーブに重みを付けつつ近づく
                float verticalTarget = Mathf.Lerp(startPos.y, targetPos.y, _verticalApproachCurve.Evaluate(rate));
                float vertical = Mathf.Lerp(hand.Position.y, verticalTarget, _verticalWeightCurve.Evaluate(rate));
                hand.Position = new Vector3(horizontal.x, vertical, horizontal.z);

                //放射方向にターゲットを向かせる。左手は方向が180度ずれてしまうので直す
                hand.Rotation = Quaternion.Euler(
                    0,
                    -Mathf.Atan2(hand.Position.z, hand.Position.x) * Mathf.Rad2Deg +
                        (isLeftHand ? 180 : 0),
                    0);
                yield return null;
            }

        }

        private IEnumerator ButtonUpRoutine(IKDataRecord hand, Vector3 targetPos, bool isLeftHand)
        {
            float startTime = Time.time;
            Vector3 startPos = hand.Position;

            //NOTE: KeyPressRoutineの後半に相当するモーション
            float duration = _keyboardMotionDuration * 0.5f;
            while (Time.time - startTime < duration)
            {
                float rate = 0.5f + (Time.time - startTime) / _keyboardMotionDuration;

                Vector3 horizontal = Vector3.Lerp(startPos, targetPos, _horizontalApproachCurve.Evaluate(rate));

                //rate == 0.5の時点でLerpのWeightが1になっているので、それを0側に戻すことで手が上がる
                float verticalTarget = Mathf.Lerp(targetPos.y + YOffsetAfterKeyDown, targetPos.y, _verticalApproachCurve.Evaluate(rate));
                hand.Position = new Vector3(horizontal.x, verticalTarget, horizontal.z);

                //どちらの場合でも放射方向にターゲットを向かせる必要がある。
                //かつ、左手は方向が180度ずれてしまうので直す
                hand.Rotation = Quaternion.Euler(
                    0,
                    -Mathf.Atan2(hand.Position.z, hand.Position.x) * Mathf.Rad2Deg +
                        (isLeftHand ? 180 : 0),
                    0);
                yield return null;
            }
        }

        private void StartLeftHandCoroutine(IEnumerator routine)
        {
            StopLeftHandCoroutine();
            _leftHandMoveCoroutine = StartCoroutine(routine);
        }

        private void StartRightHandCoroutine(IEnumerator routine)
        {
            StopRightHandCoroutine();
            _rightHandMoveCoroutine = StartCoroutine(routine);
        }

        private void StopLeftHandCoroutine()
        {
            if (_leftHandMoveCoroutine != null)
            {
                StopCoroutine(_leftHandMoveCoroutine);
            }
        }

        private void StopRightHandCoroutine()
        {
            if (_rightHandMoveCoroutine != null)
            {
                StopCoroutine(_rightHandMoveCoroutine);
            }
        }
    }
}
