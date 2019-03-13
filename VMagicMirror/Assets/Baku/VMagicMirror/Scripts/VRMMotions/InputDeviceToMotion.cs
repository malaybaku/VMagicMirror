using UnityEngine;
using System.Collections;
using System;

namespace Baku.VMagicMirror
{
    public class InputDeviceToMotion : MonoBehaviour
    {
        private const float HeadTargetForwardOffsetWhenLookKeyboard = 0.3f;

        private const string RDown = "RDown";
        private const string MDown = "MDown";
        private const string LDown = "LDown";

        #region settings 

        public KeyboardProvider keyboard = null;

        public TouchPadProvider touchPad = null;

        public Transform head = null;

        public FingerAnimator fingerAnimator = null;

        [SerializeField]
        private Transform cam = null;

        [SerializeField]
        private Transform leftHandTarget = null;

        [SerializeField]
        private Transform rightHandTarget = null;

        [SerializeField]
        private Transform headTarget = null;       

        [SerializeField]
        private Transform headLookTargetWhenTouchTyping = null;

        [SerializeField]
        private AnimationCurve keyboardHorizontalApproachCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0.0f, 1, -1, -1),
            new Keyframe(0.5f, 0, -1, 0),
            new Keyframe(1.0f, 0, 0, 0),
        });

        [SerializeField]
        private AnimationCurve keyboardVerticalApproachCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0.0f, 1, 0, 0),
            new Keyframe(0.5f, 0, -1, 1),
            new Keyframe(1.0f, 1, 0, 0),
        });

        //高さ方向についてはキー連打時に手が強引な動きに見えないよう、二重に重みづけする
        [SerializeField]
        private AnimationCurve keyboardVerticalWeightCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0, 0.2f),
            new Keyframe(0.5f, 1.0f),
        });

        [SerializeField]
        private float yOffsetAlways = 0.05f;

        private Vector3 yOffsetAlwaysVec => yOffsetAlways * Vector3.up;

        [SerializeField]
        private float yOffsetAfterKeyDown = 0.08f;

        [SerializeField]
        private float keyboardMotionDuration = 0.25f;

        [SerializeField]
        private float touchPadApproachSpeedFactor = 0.2f;

        [SerializeField]
        private float headTargetMoveSpeedFactor = 0.2f;

        [SerializeField]
        private AnimationCurve clickHandRotationAnimation = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0, 0, 0, 1),
            new Keyframe(0.1f, 10, 1, -1),
            new Keyframe(0.2f, 0, 0, 0),
        });

        [SerializeField]
        private float clickHandRotationDuration = 0.2f;

        //手首をIKすると(指先じゃなくて)手首がキー位置に行ってしまうので、その手首位置を原点方向にずらすための長さ。
        //できれば決め打ちじゃなくてVRMの読み込み直後に長さを調べてほしい
        public float handToTipLength = 0.1f;

        //こっちはマウス用
        public float handToPalmLength = 0.05f;

        //コレがtrueのときは
        public bool enableTouchTypingHeadMotion;

        //クリック時にクイッとさせたいので。
        public Transform rightHandBone = null;

        #endregion

        private Coroutine _leftHandMoveCoroutine = null;
        private Coroutine _rightHandMoveCoroutine = null;
        private Coroutine _clickMoveCoroutine = null;

        private bool _touchPadTargetEnabled = false;
        private Vector3 _touchPadTargetPosition = Vector3.zero;
        private Transform _headTrackTargetWhenNotTouchTyping = null;

        private void Update()
        {
            if (_touchPadTargetEnabled)
            {
                rightHandTarget.position = Vector3.Lerp(
                    rightHandTarget.position,
                    _touchPadTargetPosition,
                    touchPadApproachSpeedFactor
                    );
            }

            Transform headTargetTo =
                enableTouchTypingHeadMotion ?
                headLookTargetWhenTouchTyping :
                _headTrackTargetWhenNotTouchTyping;

            Vector3 targetPos =
                enableTouchTypingHeadMotion ?
                headLookTargetWhenTouchTyping.position + HeadTargetForwardOffsetWhenLookKeyboard * Vector3.forward :
                _headTrackTargetWhenNotTouchTyping.position;

            if (headTargetTo != null)
            {
                headTarget.position = Vector3.Lerp(
                    headTarget.position,
                    targetPos,
                    headTargetMoveSpeedFactor
                    );
            }
        }

        public void PressKeyMotion(string key)
        {
            Vector3 targetPos = keyboard.GetPositionOfKey(key) + yOffsetAlwaysVec;
            targetPos -= handToTipLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            if (keyboard.IsLeftHandPreffered(key))
            {
                if (_leftHandMoveCoroutine != null)
                {
                    StopCoroutine(_leftHandMoveCoroutine);
                }
                _leftHandMoveCoroutine = StartCoroutine(MoveTargetToKeyboard(leftHandTarget, targetPos));
                _headTrackTargetWhenNotTouchTyping = leftHandTarget;
            }
            else
            {
                _touchPadTargetEnabled = false;
                if (_rightHandMoveCoroutine != null)
                {
                    StopCoroutine(_rightHandMoveCoroutine);
                }
                _rightHandMoveCoroutine = StartCoroutine(MoveTargetToKeyboard(rightHandTarget, targetPos));
                _headTrackTargetWhenNotTouchTyping = rightHandTarget;
            }

            int fingerNumber = keyboard.GetFingerNumberOfKey(key);
            fingerAnimator?.StartMoveFinger(fingerNumber);
        }

        public void UpdateMouseBasedHeadTarget(int x, int y)
        {
            float xClamped = Mathf.Clamp(x - Screen.width * 0.5f, -1000, 1000) / 1000.0f;
            float yClamped = Mathf.Clamp(y - Screen.height * 0.5f, -1000, 1000) / 1000.0f;

            //xClamped *= 0.8f;
            //yClamped *= 0.8f;

            //画面中央 = カメラ位置なのでコレで空間的にだいたい正しくなる
            headLookTargetWhenTouchTyping.position =
                cam.TransformPoint(xClamped, yClamped, 0);

        }

        public void GrabMouseMotion(int x, int y)
        {
            float xClamped = Mathf.Clamp(x - Screen.width * 0.5f, -1000, 1000) / 1000.0f;
            float yClamped = Mathf.Clamp(y - Screen.height * 0.5f , -1000, 1000) / 1000.0f;
            var targetPos = touchPad.GetHandTipPosFromScreenPoint(xClamped, yClamped) + yOffsetAlwaysVec;
            targetPos -= handToPalmLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            if (_rightHandMoveCoroutine != null)
            {
                StopCoroutine(_rightHandMoveCoroutine);
            }
            _touchPadTargetPosition = targetPos;
            _touchPadTargetEnabled = true;

            _headTrackTargetWhenNotTouchTyping = rightHandTarget;
        }

        public void ClickMotion(string info)
        {
            if (_clickMoveCoroutine != null)
            {
                StopCoroutine(_clickMoveCoroutine);
            }

            _clickMoveCoroutine = StartCoroutine(ClickMotionByRightHand());

            if (fingerAnimator != null)
            {
                if (info == RDown)
                {
                    fingerAnimator.StartMoveFinger(FingerConsts.RightMiddle);
                }
                else if (info == MDown || info == LDown)
                {
                    fingerAnimator.StartMoveFinger(FingerConsts.RightIndex);
                }
            }
        }

        private IEnumerator MoveTargetToKeyboard(Transform t, Vector3 targetPos)
        {
            float startTime = Time.time;
            Vector3 startPos = t.position;

            while (Time.time - startTime < keyboardMotionDuration)
            {
                float rate = (Time.time - startTime) / keyboardMotionDuration;

                Vector3 horizontal = Vector3.Lerp(startPos, targetPos, keyboardHorizontalApproachCurve.Evaluate(rate));

                if (rate < 0.5f)
                {
                    //アプローチ中: yのカーブに重みを付けつつ近づく
              
                    float verticalTarget = Mathf.Lerp(startPos.y, targetPos.y, keyboardVerticalApproachCurve.Evaluate(rate));
                    float vertical = Mathf.Lerp(t.position.y, verticalTarget, keyboardVerticalWeightCurve.Evaluate(rate));
                    t.position = new Vector3(horizontal.x, vertical, horizontal.z);
                }
                else
                {
                    //離れるとき: yを引き上げる。気持ち的には(targetPos.y + yOffset)がスタート位置側にあたるので、ウェイトを1から0に引き戻す感じ
                    float verticalTarget = Mathf.Lerp(targetPos.y + yOffsetAfterKeyDown, targetPos.y, keyboardVerticalApproachCurve.Evaluate(rate));
                    t.position = new Vector3(horizontal.x, verticalTarget, horizontal.z);
                }

                yield return null;
            }

            //ターゲットを動かすやつはいないので不要…なはず…
            //var finalTarget = new Vector3(targetPos.x, targetPos.y + yOffset, targetPos.z);
            //while (true)
            //{
            //    t.position = finalTarget;
            //    yield return null;
            //}
        }

        private IEnumerator ClickMotionByRightHand()
        {
            if (rightHandBone == null)
            {
                yield break;
            }

            float startTime = Time.time;
            while (Time.time - startTime < clickHandRotationDuration)
            {
                rightHandBone.localRotation = Quaternion.Euler(
                    0,
                    0, 
                    clickHandRotationAnimation.Evaluate(Time.time - startTime)
                    );
                yield return null;
            }

            //note: 無くてもいいかも
            rightHandBone.localRotation = Quaternion.Euler(0, 0, 0);
        }

    }

}
