using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>タイピング入力に基づいて、腕IKの出力値を計算します。</summary>
    public class TypingHandIKGenerator : MonoBehaviour
    {
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        //手を正面方向に向くよう補正するファクター。1に近いほど手が正面向きになる
        private const float WristForwardFactor = 0.5f;

        #region settings (WPFから飛んでくる想定のもの)

        /// <summary>手首から指先までの距離[m]。キーボードを打ってる位置をそれらしく補正するために使う。</summary>
        public float HandToTipLength { get; set; } = 0.12f;

        /// <summary>キーボードに対する手のY方向オフセット[m]。大きくするとタイピング動作が大げさになる。</summary>
        public float YOffsetAlways { get; set; } = 0.03f;

        /// <summary>打鍵直後のキーボードに対する手のY方向オフセット[m]。</summary>
        public float YOffsetAfterKeyDown { get; set; } = 0.02f;

        #endregion

        #region 静的に決め打ちするもの

        public KeyboardProvider keyboard = null;

       [SerializeField]
        private AnimationCurve horizontalApproachCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0.0f, 1, -1, -1),
            new Keyframe(0.5f, 0, -1, 0),
            new Keyframe(1.0f, 0, 0, 0),
        });

        [SerializeField]
        private AnimationCurve verticalApproachCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0.0f, 1, 0, 0),
            new Keyframe(0.5f, 0, -1, 1),
            new Keyframe(1.0f, 1, 0, 0),
        });

        //高さ方向のブレンディング用のウェイト: このウェイトにより、高速で打鍵するときもある程度手が上下する
        [SerializeField]
        private AnimationCurve keyboardVerticalWeightCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0, 0.2f),
            new Keyframe(0.5f, 1.0f),
        });

        [SerializeField]
        private float keyboardMotionDuration = 0.25f;

        private Vector3 YOffsetAlwaysVec => YOffsetAlways * Vector3.up;

        #endregion

        //要るかなコレ。なくてもいいのでは？
        private Coroutine _leftHandMoveCoroutine = null;
        private Coroutine _rightHandMoveCoroutine = null;

        private void Start()
        {
            //初期位置がゼロだとヘンな動きになるので、それを防ぐ
            _leftHand.Position = keyboard.GetKeyTargetData("F").positionWithOffset;
            _rightHand.Position = keyboard.GetKeyTargetData("J").positionWithOffset;
        }
        
        
        public (ReactedHand, Vector3) PressKey(string key, bool isLeftHandOnlyMode)
        {
            var keyData = keyboard.GetKeyTargetData(key, isLeftHandOnlyMode);
            
            Vector3 targetPos = keyData.positionWithOffset + YOffsetAlwaysVec;

            var handOrientation = Vector3.Lerp(
                new Vector3(targetPos.x, 0, targetPos.z).normalized,
                Vector3.forward,
                WristForwardFactor
                ).normalized;
            
            targetPos -= HandToTipLength * handOrientation;

            if (keyData.IsLeftHandPreffered)
            {
                UpdateLeftHandCoroutine(KeyPressRoutine(IKTargets.LHand, targetPos));
                return (ReactedHand.Left, keyData.position);
            }
            else
            {
                UpdateRightHandCoroutine(KeyPressRoutine(IKTargets.RHand, targetPos));
                return (ReactedHand.Right, keyData.position);
            }
        }

        private IEnumerator KeyPressRoutine(IKTargets target, Vector3 targetPos)
        {
            bool isLeftHand = (target == IKTargets.LHand);
            IKDataRecord ikTarget = isLeftHand ? _leftHand : _rightHand;

            float startTime = Time.time;
            Vector3 startPos = ikTarget.Position;
            float forwardDeg = isLeftHand ? 90 : -90;
            
            while (Time.time - startTime < keyboardMotionDuration)
            {
                float rate = (Time.time - startTime) / keyboardMotionDuration;

                Vector3 horizontal = Vector3.Lerp(startPos, targetPos, horizontalApproachCurve.Evaluate(rate));

                if (rate < 0.5f)
                {
                    //アプローチ中: yのカーブに重みを付けつつ近づく
                    float verticalTarget = Mathf.Lerp(startPos.y, targetPos.y, verticalApproachCurve.Evaluate(rate));
                    float vertical = Mathf.Lerp(ikTarget.Position.y, verticalTarget, keyboardVerticalWeightCurve.Evaluate(rate));
                    ikTarget.Position = new Vector3(horizontal.x, vertical, horizontal.z);
                }
                else
                {
                    //離れるとき: yを引き上げる。気持ち的には(targetPos.y + yOffset)がスタート位置側にあたるので、ウェイトを1から0に引き戻す感じ
                    float verticalTarget = Mathf.Lerp(targetPos.y + YOffsetAfterKeyDown, targetPos.y, verticalApproachCurve.Evaluate(rate));
                    ikTarget.Position = new Vector3(horizontal.x, verticalTarget, horizontal.z);
                }

                float angleDeg =
                    -Mathf.Atan2(ikTarget.Position.z, ikTarget.Position.x) * Mathf.Rad2Deg + 
                    (isLeftHand ? 180 : 0);
                angleDeg = Mathf.Repeat(angleDeg + 180f, 360f) - 180f;
                
                //どちらの場合でも放射方向にターゲットを向かせる必要がある。
                //かつ、左手は方向が180度ずれてしまうので直す
                ikTarget.Rotation = Quaternion.Euler(
                    0,
                    Mathf.Lerp(angleDeg, forwardDeg, WristForwardFactor),
                    0);
                yield return null;
            }

            //最後: ピッタリ合わせておしまい
            ikTarget.Position = new Vector3(targetPos.x, targetPos.y + YOffsetAfterKeyDown, targetPos.z);
            
            float finishAngleDeg = 
                -Mathf.Atan2(ikTarget.Position.z, ikTarget.Position.x) * Mathf.Rad2Deg + 
                (isLeftHand ? 180 : 0);
            finishAngleDeg = Mathf.Repeat(finishAngleDeg + 180f, 360f) - 180f;
            
            ikTarget.Rotation = Quaternion.Euler(
                0,
                Mathf.Lerp(finishAngleDeg, forwardDeg, WristForwardFactor),
                0);
        }

        private void UpdateLeftHandCoroutine(IEnumerator routine)
        {
            if (_leftHandMoveCoroutine != null)
            {
                StopCoroutine(_leftHandMoveCoroutine);
            }
            _leftHandMoveCoroutine = StartCoroutine(routine);
        }

        private void UpdateRightHandCoroutine(IEnumerator routine)
        {
            if (_rightHandMoveCoroutine != null)
            {
                StopCoroutine(_rightHandMoveCoroutine);
            }
            _rightHandMoveCoroutine = StartCoroutine(routine);
        }
    }
}


