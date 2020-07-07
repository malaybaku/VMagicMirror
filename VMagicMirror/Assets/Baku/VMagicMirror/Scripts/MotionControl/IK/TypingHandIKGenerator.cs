using System.Collections;
using UnityEngine;
using Zenject;

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

        #endregion

        private KeyboardProvider _keyboard = null;
        
        //要るかなコレ。なくてもいいのでは？
        private Coroutine _leftHandMoveCoroutine = null;
        private Coroutine _rightHandMoveCoroutine = null;

        [Inject]
        public void Initialize(KeyboardProvider provider)
        {
            _keyboard = provider;
        }
        
        private void Start()
        {
            //これらのIKは初期値から動かない事があるので、その場合にあまりに変になるのを防ぐのが狙い。
            _leftHand.Position = _keyboard.GetKeyTargetData("F").positionWithOffset;
            _leftHand.Rotation = Quaternion.Euler(0, 90, 0);
            
            _rightHand.Position = _keyboard.GetKeyTargetData("J").positionWithOffset;
            _rightHand.Rotation = Quaternion.Euler(0, -90, 0);
        }

        public (ReactedHand, Vector3) PressKey(string key, bool isLeftHandOnlyMode)
        {
            var keyData = _keyboard.GetKeyTargetData(key, isLeftHandOnlyMode);
            
            Vector3 targetPos =
                keyData.positionWithOffset + 
                YOffsetAlways * _keyboard.KeyboardUp -
                HandToTipLength * _keyboard.KeyboardForward;

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
            //NOTE: 第2項は手首を正面に向けるための前処理みたいなファクターです
            var keyboardRot = 
                _keyboard.GetKeyboardRotation() * 
                Quaternion.AngleAxis(isLeftHand ? 90 : -90, Vector3.up);
            var keyboardRootPos = _keyboard.transform.position;
            var keyboardUp = _keyboard.KeyboardUp;

            float startTime = Time.time;
            Vector3 startPos = ikTarget.Position;
            float startVertical = Vector3.Dot(startPos - keyboardRootPos, keyboardUp);
            float targetVertical = Vector3.Dot(targetPos - keyboardRootPos, keyboardUp);
            
            while (Time.time - startTime < keyboardMotionDuration)
            {
                float rate = (Time.time - startTime) / keyboardMotionDuration;

                Vector3 lerpApproach = Vector3.Lerp(startPos, targetPos, horizontalApproachCurve.Evaluate(rate));
                //Y成分に相当するところをキャンセルしておく
                lerpApproach -= keyboardUp * Vector3.Dot(lerpApproach - keyboardRootPos, keyboardUp);

                if (rate < 0.5f)
                {
                    //アプローチ中: 垂直方向のカーブのつけかたをいい感じにする。
                    float verticalTarget = Mathf.Lerp(
                        startVertical, targetVertical, verticalApproachCurve.Evaluate(rate)
                        );
                    float vertical = Mathf.Lerp(
                        Vector3.Dot(ikTarget.Position - keyboardRootPos, keyboardUp),
                        verticalTarget,
                        keyboardVerticalWeightCurve.Evaluate(rate)
                        );
                    ikTarget.Position = lerpApproach + keyboardUp * vertical;
                }
                else
                {
                    //離れるとき: キーボードから垂直方向に手を引き上げる。Lerpの係数は1から0に戻っていくことに注意
                    float vertical = Mathf.Lerp(
                        targetVertical + YOffsetAfterKeyDown, 
                        targetVertical,
                        verticalApproachCurve.Evaluate(rate));
                    ikTarget.Position = lerpApproach + keyboardUp * vertical;
                }
                
                //一応Lerpしてるけどあんまり必要ないかもね
                ikTarget.Rotation = Quaternion.Slerp(
                    ikTarget.Rotation,
                    keyboardRot,
                    0.2f
                );

                yield return null;
            }

            //最後: ピッタリ合わせておしまい
            ikTarget.Position = targetPos + keyboardUp * YOffsetAfterKeyDown; 
            ikTarget.Rotation = keyboardRot;
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


