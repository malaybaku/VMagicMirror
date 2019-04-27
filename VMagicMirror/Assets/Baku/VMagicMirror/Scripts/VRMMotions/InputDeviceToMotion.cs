using UnityEngine;
using System.Collections;
using System;
using XinputGamePad;

namespace Baku.VMagicMirror
{
    public class InputDeviceToMotion : MonoBehaviour
    {
        private const float HeadTargetForwardOffsetWhenLookKeyboard = 0.3f;
        //この量だけ、プレゼンモードではスライドが背後にあるように扱う
        private const float PresentationSlideAssumedZOffset = 0.0f;

        private const float PresentationArmRollFixedAngle = 25.0f;

        private const string RDown = "RDown";
        private const string MDown = "MDown";
        private const string LDown = "LDown";

        #region settings (WPFから飛んでくる想定のもの)

        //手首をIKすると(指先じゃなくて)手首がキー位置に行ってしまうので、その手首位置を原点方向にずらすための長さ。
        public float handToTipLength = 0.1f;

        //こっちはマウス用
        public float handToPalmLength = 0.05f;

        //コレがtrueのときは頭の注視先がカーソルベースになる
        public bool enableTouchTypingHeadMotion;

        public float yOffsetAlways = 0.05f;

        public float yOffsetAfterKeyDown = 0.08f;

        public float presentationArmMotionScale = 0.3f;

        public float presentationArmRadiusMin = 0.4f;

        private bool _enablePresentationMotion = false;
        public bool EnablePresentationMotion
        {
            get => _enablePresentationMotion;
            set
            {
                if (_enablePresentationMotion != value)
                {
                    _enablePresentationMotion = value;
                    if (!value)
                    {
                        fingerAnimator.FixRightHandToPresentationMode(false);
                    }
                }
            }
        } 

        #endregion

        #region settings (Unityで閉じてる想定のもの)

        public KeyboardProvider keyboard = null;

        public TouchPadProvider touchPad = null;

        public GamepadProvider gamePad = null;

        public Transform head = null;

        //プレゼン中に肩からの位置ベースで手の向きを制御したいので
        public Transform rightShoulder = null;

        public FingerAnimator fingerAnimator = null;

        [SerializeField]
        private Transform cam = null;

        [SerializeField]
        private LayerMask raycastMask;

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

        //クリック時にクイッとさせたいので。
        public Transform rightHandBone = null;

        //アナログスティックに反応して傾ける為
        public Transform vrmRoot = null;
        public float bodyLeanSpeedFactor = 0.1f;
        public float bodyLeanMaxAngleDegree = 5.0f;

        private Vector3 yOffsetAlwaysVec => yOffsetAlways * Vector3.up;

        #endregion

        private Coroutine _leftHandMoveCoroutine = null;
        private Coroutine _rightHandMoveCoroutine = null;
        private Coroutine _clickMoveCoroutine = null;

        private HandTargetTypes _leftHandTargetType = HandTargetTypes.Keyboard;
        private HandTargetTypes _rightHandTargetType = HandTargetTypes.Keyboard;

        private Vector3 _touchPadTargetPosition = Vector3.zero;
        private Vector3 _presentationSlideTargetPosition = Vector3.zero;
        private Transform _headTrackTargetWhenNotTouchTyping = null;

        private int _leftHandPressedGamepadKeyCount = 0;
        private int _rightHandPressedGamepadKeyCount = 0;
        private Vector3 _leftGamepadStickPosition = Vector3.zero;
        private Vector3 _rightGamepadStickPosition = Vector3.zero;
        private Vector3 _bodyLeanTargetEulerAngle = Vector3.zero;

        private void Start()
        {
            //nullを避けておく
            _headTrackTargetWhenNotTouchTyping = rightHandTarget;
        }

        private void Update()
        {
            switch (_leftHandTargetType)
            {
                case HandTargetTypes.GamepadStick:
                    leftHandTarget.position = Vector3.Lerp(
                        leftHandTarget.position,
                        _leftGamepadStickPosition,
                        touchPadApproachSpeedFactor
                        );
                    leftHandTarget.rotation = Quaternion.Euler(
                        0, -Mathf.Atan2(leftHandTarget.position.z, leftHandTarget.position.x) * Mathf.Rad2Deg + 180, 0
                        );
                    break;
                default:
                    break;
            }

            switch (_rightHandTargetType)
            {
                case HandTargetTypes.MousePad:
                    if (EnablePresentationMotion)
                    {
                        rightHandTarget.position = Vector3.Lerp(
                            rightHandTarget.position,
                            _presentationSlideTargetPosition,
                            touchPadApproachSpeedFactor
                            );
                        //NOTE: 手首がスライドの方を向くようにしたい(何もしないと手首が水平になってしまう)
                        if (rightShoulder != null)
                        {
                            //NOTE: 40degまわしてるのは手の甲側を向ける為
                            rightHandTarget.rotation = Quaternion.FromToRotation(
                                Vector3.right,
                                (rightHandTarget.position - rightShoulder.position).normalized
                                ) * Quaternion.AngleAxis(PresentationArmRollFixedAngle, Vector3.right);
                        }
                        else
                        {
                            rightHandTarget.rotation = Quaternion.FromToRotation(
                                Vector3.right,
                                rightHandTarget.position.normalized
                                );
                        }
                    }
                    else
                    {
                        rightHandTarget.position = Vector3.Lerp(
                            rightHandTarget.position,
                            _touchPadTargetPosition,
                            touchPadApproachSpeedFactor
                            );
                        rightHandTarget.rotation = Quaternion.Euler(
                            0, -Mathf.Atan2(rightHandTarget.position.z, rightHandTarget.position.x) * Mathf.Rad2Deg, 0
                            );
                    }
                    break;
                case HandTargetTypes.GamepadStick:
                    rightHandTarget.position = Vector3.Lerp(
                        rightHandTarget.position,
                        _rightGamepadStickPosition,
                        touchPadApproachSpeedFactor
                        );
                    rightHandTarget.rotation = Quaternion.Euler(
                        0, -Mathf.Atan2(rightHandTarget.position.z, rightHandTarget.position.x) * Mathf.Rad2Deg, 0
                        );
                    break;
                default:
                    break;
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
            
            if (vrmRoot != null)
            {
                vrmRoot.localRotation = Quaternion.Slerp(
                    vrmRoot.localRotation,
                    Quaternion.Euler(_bodyLeanTargetEulerAngle),
                    bodyLeanSpeedFactor
                    );
            }
        }

        #region Keyboard

        public void PressKeyMotion(string key)
        {
            Vector3 targetPos = keyboard.GetPositionOfKey(key) + yOffsetAlwaysVec;
            targetPos -= handToTipLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            if (keyboard.IsLeftHandPreffered(key))
            {
                _leftHandTargetType = HandTargetTypes.Keyboard;
                UpdateLeftHandMoveCoroutine(
                    KeyPressRoutine(leftHandTarget, targetPos, true)
                    );
                _headTrackTargetWhenNotTouchTyping = leftHandTarget;
            }
            else
            {
                //右手でキーを触るため、指の形はプレゼン状態ではなくす(ただしマウスがまた動いたらプレゼン状態になる)
                fingerAnimator.FixRightHandToPresentationMode(false);

                _rightHandTargetType = HandTargetTypes.Keyboard;
                UpdateRightHandMoveCoroutine(
                    KeyPressRoutine(rightHandTarget, targetPos, false)
                    );
                _headTrackTargetWhenNotTouchTyping = rightHandTarget;
            }

            int fingerNumber = keyboard.GetFingerNumberOfKey(key);
            fingerAnimator?.StartMoveFinger(fingerNumber);
        }

        private IEnumerator KeyPressRoutine(Transform hand, Vector3 targetPos, bool isLeftHand)
        {
            float startTime = Time.time;
            Vector3 startPos = hand.position;

            while (Time.time - startTime < keyboardMotionDuration)
            {
                float rate = (Time.time - startTime) / keyboardMotionDuration;

                Vector3 horizontal = Vector3.Lerp(startPos, targetPos, keyboardHorizontalApproachCurve.Evaluate(rate));

                if (rate < 0.5f)
                {
                    //アプローチ中: yのカーブに重みを付けつつ近づく

                    float verticalTarget = Mathf.Lerp(startPos.y, targetPos.y, keyboardVerticalApproachCurve.Evaluate(rate));
                    float vertical = Mathf.Lerp(hand.position.y, verticalTarget, keyboardVerticalWeightCurve.Evaluate(rate));
                    hand.position = new Vector3(horizontal.x, vertical, horizontal.z);
                }
                else
                {
                    //離れるとき: yを引き上げる。気持ち的には(targetPos.y + yOffset)がスタート位置側にあたるので、ウェイトを1から0に引き戻す感じ
                    float verticalTarget = Mathf.Lerp(targetPos.y + yOffsetAfterKeyDown, targetPos.y, keyboardVerticalApproachCurve.Evaluate(rate));
                    hand.position = new Vector3(horizontal.x, verticalTarget, horizontal.z);
                }

                //どちらの場合でも放射方向にターゲットを向かせる必要がある。
                //かつ、左手は方向が180度ずれてしまうので直す
                hand.rotation = Quaternion.Euler(
                    0,
                    -Mathf.Atan2(hand.position.z, hand.position.x) * Mathf.Rad2Deg +
                        (isLeftHand ? 180 : 0),
                    0);
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

        #endregion

        #region Mouse

        public void UpdateMouseBasedHeadTarget(int x, int y)
        {
            float xClamped = Mathf.Clamp(x - Screen.width * 0.5f, -1000, 1000) / 1000.0f;
            float yClamped = Mathf.Clamp(y - Screen.height * 0.5f, -1000, 1000) / 1000.0f;

            //画面中央 = カメラ位置なのでコレで空間的にだいたい正しいハズ
            //カメラ座標のz=0平面を見ようとするので、斜め前のどこかを見るような体裁。
            headLookTargetWhenTouchTyping.position =
                cam.TransformPoint(xClamped, yClamped, 0);

        }

        public void GrabMouseMotion(int x, int y)
        {
            if (EnablePresentationMotion)
            {
                float scaledX = presentationArmMotionScale * (x - Screen.width * 0.5f) / Screen.width;
                float scaledY = presentationArmMotionScale * (y - Screen.height * 0.5f) / Screen.height;


                var ray = new Ray(cam.TransformPoint(scaledX, scaledY, 0), cam.forward);
                float depthFromCamera = 1.0f;
                if (Physics.Raycast(ray, out RaycastHit hit))//, 1000, raycastMask.value))
                {
                    depthFromCamera = hit.distance;
                    //depthFromCamera = Vector3.Distance(ray.origin, hit.point);
                }

                //NOTE: Zの値はキャラのルート位置が零点であることを使って補正してる点に注意
                //float depthFromCamera = 
                //    new Vector2(cam.position.x, cam.position.z).magnitude +
                //    PresentationSlideAssumedZOffset;

                var targetPosition = cam.TransformPoint(scaledX, scaledY, depthFromCamera);
                //NOTE: 右腕を強引に左側に引っ張らないためのガード
                if (!(targetPosition.x > 0))
                {
                    targetPosition = new Vector3(0.01f, targetPosition.y, 0);
                }

                //NOTE: 手を肩よりも後ろに回すのはあまり自然じゃないのでガード
                if (targetPosition.z < 0)
                {
                    targetPosition = new Vector3(targetPosition.x, targetPosition.y, 0);
                }

                //手が体に近すぎるとめり込むのをガード
                var horizontalVec = new Vector2(targetPosition.x, targetPosition.z);
                if (horizontalVec.magnitude < presentationArmRadiusMin)
                {
                    if (horizontalVec.magnitude < Mathf.Epsilon)
                    {
                        horizontalVec = new Vector2(1, 0);
                    }
                    horizontalVec = presentationArmRadiusMin * horizontalVec.normalized;

                    targetPosition = new Vector3(
                        horizontalVec.x,
                        targetPosition.y,
                        horizontalVec.y
                        );
                }

                _presentationSlideTargetPosition = targetPosition;

                if (_rightHandMoveCoroutine != null)
                {
                    StopCoroutine(_rightHandMoveCoroutine);
                }

                //NOTE: 実態としてはマウスパッド持ってるのに近い状態なのでコレでOKとする
                _rightHandTargetType = HandTargetTypes.MousePad;

                fingerAnimator.FixRightHandToPresentationMode(true);
            }
            else
            {
                //NOTE: マウスパッド上で腕が置かれてて欲しい場所を指定し、IKモードによってはLookAtもそっちに向くようにする
                float xClamped = Mathf.Clamp(x - Screen.width * 0.5f, -1000, 1000) / 1000.0f;
                float yClamped = Mathf.Clamp(y - Screen.height * 0.5f, -1000, 1000) / 1000.0f;
                var targetPos = touchPad.GetHandTipPosFromScreenPoint(xClamped, yClamped) + yOffsetAlwaysVec;
                targetPos -= handToPalmLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

                if (_rightHandMoveCoroutine != null)
                {
                    StopCoroutine(_rightHandMoveCoroutine);
                }
                _touchPadTargetPosition = targetPos;
                _rightHandTargetType = HandTargetTypes.MousePad;

                _headTrackTargetWhenNotTouchTyping = rightHandTarget;
            }

        }

        public void ClickMotion(string info)
        {
            //指さしモード中は無視
            if (fingerAnimator.RightHandPresentationMode) { return; }

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

        #endregion

        #region Gamepad

        public void GamepadButtonDown(XinputKey key)
        {
            Vector3 targetPos = gamePad.GetButtonPosition(key) + yOffsetAlwaysVec;
            targetPos -= handToTipLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            if (gamePad.IsLeftHandPreffered(key))
            {
                _leftHandPressedGamepadKeyCount++;
                if (_leftHandPressedGamepadKeyCount == 1)
                {
                    _leftHandTargetType = HandTargetTypes.GamepadButton;
                    UpdateLeftHandMoveCoroutine(
                        GamepadButtonDownRoutine(leftHandTarget, targetPos, true)
                        );
                    _headTrackTargetWhenNotTouchTyping = leftHandTarget;
                }
            }
            else
            {
                _rightHandPressedGamepadKeyCount++;
                if (_rightHandPressedGamepadKeyCount == 1)
                {
                    _rightHandTargetType = HandTargetTypes.GamepadButton;
                    UpdateRightHandMoveCoroutine(
                        GamepadButtonDownRoutine(rightHandTarget, targetPos, false)
                        );
                    _headTrackTargetWhenNotTouchTyping = rightHandTarget;
                }
            }
        }

        public void GamepadButtonUp(XinputKey key)
        {
            Vector3 targetPos = gamePad.GetButtonPosition(key) + yOffsetAlwaysVec;
            targetPos -= handToTipLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            if (gamePad.IsLeftHandPreffered(key))
            {
                _leftHandPressedGamepadKeyCount--;
                if (_leftHandPressedGamepadKeyCount == 0)
                {
                    UpdateLeftHandMoveCoroutine(
                        GamepadButtonUpRoutine(leftHandTarget, targetPos, true)
                        );
                }
            }
            else
            {
                _rightHandPressedGamepadKeyCount--;
                if (_rightHandPressedGamepadKeyCount == 0)
                {
                    UpdateRightHandMoveCoroutine(
                        GamepadButtonUpRoutine(rightHandTarget, targetPos, false)
                        );
                }
            }
        }

        public void GamepadLeftStick(Vector2 stickPos)
        {
            var targetPos = gamePad.GetLeftStickPosition(stickPos.x, stickPos.y) + yOffsetAlwaysVec;
            targetPos -= handToPalmLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            _leftGamepadStickPosition = targetPos;

            StopLeftHandMoveCoroutine();
            _leftHandTargetType = HandTargetTypes.GamepadStick;
        }

        public void GamepadRightStick(Vector2 stickPos)
        {
            var targetPos = gamePad.GetRightStickPosition(stickPos.x, stickPos.y) + yOffsetAlwaysVec;
            targetPos -= handToPalmLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            _rightGamepadStickPosition = targetPos;

            StopRightHandMoveCoroutine();
            _rightHandTargetType = HandTargetTypes.GamepadStick;
        }

        public void GamepadLeanMotion(Vector2 stickPos)
        {
            _bodyLeanTargetEulerAngle = new Vector3(
                stickPos.y * bodyLeanMaxAngleDegree,
                0,
                -stickPos.x * bodyLeanMaxAngleDegree
                );

        }

        private IEnumerator GamepadButtonDownRoutine(Transform hand, Vector3 targetPos, bool isLeftHand)
        {
            float startTime = Time.time;
            Vector3 startPos = hand.position;

            //NOTE: KeyPressRoutineの前半に相当する押下のモーションまででストップ
            float duration = keyboardMotionDuration * 0.5f;
            while (Time.time - startTime < duration)
            {
                float rate = (Time.time - startTime) / keyboardMotionDuration;

                Vector3 horizontal = Vector3.Lerp(startPos, targetPos, keyboardHorizontalApproachCurve.Evaluate(rate));

                //yのカーブに重みを付けつつ近づく
                float verticalTarget = Mathf.Lerp(startPos.y, targetPos.y, keyboardVerticalApproachCurve.Evaluate(rate));
                float vertical = Mathf.Lerp(hand.position.y, verticalTarget, keyboardVerticalWeightCurve.Evaluate(rate));
                hand.position = new Vector3(horizontal.x, vertical, horizontal.z);

                //放射方向にターゲットを向かせる。左手は方向が180度ずれてしまうので直す
                hand.rotation = Quaternion.Euler(
                    0,
                    -Mathf.Atan2(hand.position.z, hand.position.x) * Mathf.Rad2Deg +
                        (isLeftHand ? 180 : 0),
                    0);
                yield return null;
            }

        }

        private IEnumerator GamepadButtonUpRoutine(Transform hand, Vector3 targetPos, bool isLeftHand)
        {
            float startTime = Time.time;
            Vector3 startPos = hand.position;

            //NOTE: KeyPressRoutineの後半に相当するモーション
            float duration = keyboardMotionDuration * 0.5f;
            while (Time.time - startTime < duration)
            {
                float rate = 0.5f + (Time.time - startTime) / keyboardMotionDuration;

                Vector3 horizontal = Vector3.Lerp(startPos, targetPos, keyboardHorizontalApproachCurve.Evaluate(rate));

                //rate == 0.5の時点でLerpのWeightが1になっているので、それを0側に戻すことで手が上がる
                float verticalTarget = Mathf.Lerp(targetPos.y + yOffsetAfterKeyDown, targetPos.y, keyboardVerticalApproachCurve.Evaluate(rate));
                hand.position = new Vector3(horizontal.x, verticalTarget, horizontal.z);

                //どちらの場合でも放射方向にターゲットを向かせる必要がある。
                //かつ、左手は方向が180度ずれてしまうので直す
                hand.rotation = Quaternion.Euler(
                    0,
                    -Mathf.Atan2(hand.position.z, hand.position.x) * Mathf.Rad2Deg +
                        (isLeftHand ? 180 : 0),
                    0);
                yield return null;
            }
        }



        #endregion


        private void UpdateLeftHandMoveCoroutine(IEnumerator routine)
        {
            StopLeftHandMoveCoroutine();
            _leftHandMoveCoroutine = StartCoroutine(routine);
        }

        private void UpdateRightHandMoveCoroutine(IEnumerator routine)
        {
            StopRightHandMoveCoroutine();
            _rightHandMoveCoroutine = StartCoroutine(routine);
        }

        private void StopLeftHandMoveCoroutine()
        {
            if (_leftHandMoveCoroutine != null)
            {
                StopCoroutine(_leftHandMoveCoroutine);
            }
        }

        private void StopRightHandMoveCoroutine()
        {
            if (_rightHandMoveCoroutine != null)
            {
                StopCoroutine(_rightHandMoveCoroutine);
            }
        }

        private enum HandTargetTypes
        {
            Keyboard,
            MousePad,
            GamepadButton,
            GamepadStick,
        }
    }

}
