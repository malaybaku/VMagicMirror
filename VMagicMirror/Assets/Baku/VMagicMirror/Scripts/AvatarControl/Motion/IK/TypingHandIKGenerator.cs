using System;
using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>タイピング入力に基づいて、腕IKの出力値を計算します。</summary>
    public class TypingHandIKGenerator : MonoBehaviour
    {
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        private readonly IKDataRecord _blendedLeftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _blendedLeftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        private readonly IKDataRecord _blendedRightHand = new IKDataRecord();
        public IIKGenerator RightHand => _blendedRightHand;

        //手を正面方向に向くよう補正するファクター。1に近いほど手が正面向きになる
        private const float WristForwardFactor = 0.5f;

        //NOTE: HandIkIntegratorから初期化で入れてもらう
        public AlwaysDownHandIkGenerator DownHand { get; set; } 

        #region settings (WPFから飛んでくる想定のもの)

        /// <summary>手首から指先までの距離[m]。キーボードを打ってる位置をそれらしく補正するために使う。</summary>
        public float HandToTipLength { get; set; } = 0.12f;

        /// <summary>キーボードに対する手のY方向オフセット[m]。大きくするとタイピング動作が大げさになる。</summary>
        public float YOffsetAlways { get; set; } = 0.03f;

        /// <summary>打鍵直後のキーボードに対する手のY方向オフセット[m]。</summary>
        public float YOffsetAfterKeyDown { get; set; } = 0.02f;

        /// <summary> 一定時間タイピングしなかった場合に手降ろし姿勢に遷移すべきかどうか </summary>
        public bool EnableHandDownTimeout { get; set; } = true;

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

        //左右それぞれの手について、現在押し下げている直近のキー
        private string _leftCurrentKey = "";
        private string _rightCurrentKey = "";

        //入力(キーの上げ下げ)がない時間のカウント。
        private float _leftHandNoInputCount = 0f;
        private float _rightHandNoInputCount = 0f;

        //この値で手下げ姿勢と手上げ姿勢をブレンドする。
        private float _leftHandBlendRate = 1f;
        private float _rightHandBlendRate = 1f;

        //NOTE: こう書いた方がDRY原則っぽくなるので一応。
        private const float HandDownBlendSpeed = HandIKIntegrator.HandDownBlendSpeed;
        private const float HandUpBlendSpeed = HandIKIntegrator.HandUpBlendSpeed;

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
        
        //TODO: 上げ下げをちゃんと配慮する。ただし、素早すぎる打鍵に対して見栄えを担保しなきゃいけないことに注意
        //コルーチンが以下3種類あるように考え、2キー以上を片手で同時押しするのは再現しない。
        // - リリース状態からキー押しに移行 <- 従来のKeyPressの前半の動き、でもよい。ちょっとオーバースペックだけど
        // - すでにキーを押してる状態から他へ移行 <- 従来のKeyPressの前半の動き
        // - いま押してるキーを離す <- 従来のKeyPress後半の動き

        public (ReactedHand, Vector3) KeyDown(string key, bool isLeftHandOnlyMode)
        {
            var keyData = _keyboard.GetKeyTargetData(key, isLeftHandOnlyMode);
            
            Vector3 targetPos =
                keyData.positionWithOffset + 
                YOffsetAlways * _keyboard.KeyboardUp -
                HandToTipLength * _keyboard.KeyboardForward;            
            
            if (keyData.IsLeftHandPreffered)
            {
                _leftCurrentKey = key;
                UpdateLeftHandCoroutine(KeyDownRoutine(IKTargets.LHand, targetPos));
                return (ReactedHand.Left, keyData.position);
            }
            else
            {
                _rightCurrentKey = key;
                UpdateRightHandCoroutine(KeyDownRoutine(IKTargets.RHand, targetPos));
                return (ReactedHand.Right, keyData.position);
            }
        }
        
        public (ReactedHand, Vector3) KeyUp(string key, bool isLeftHandOnlyMode)
        {
            if (key != _leftCurrentKey && key != _rightCurrentKey)
            {
                //今押してるのと違うキーを上げた場合、上げ動作をすると変になっちゃうので無視。
                //「AをKeyDown > BをKeyDown > AをKeyUp」などとするとココに到達する
                return (ReactedHand.None, Vector3.zero);
            }
         
            var keyData = _keyboard.GetKeyTargetData(key, isLeftHandOnlyMode);
            Vector3 targetPos =
                keyData.positionWithOffset + 
                YOffsetAlways * _keyboard.KeyboardUp -
                HandToTipLength * _keyboard.KeyboardForward;

            if (keyData.IsLeftHandPreffered)
            {
                _leftCurrentKey = "";
                UpdateLeftHandCoroutine(KeyUpRoutine(IKTargets.LHand, targetPos));
                return (ReactedHand.Left, keyData.position);
            }
            else
            {
                _rightCurrentKey = "";
                UpdateRightHandCoroutine(KeyUpRoutine(IKTargets.RHand, targetPos));
                return (ReactedHand.Right, keyData.position);
            }
        }


        public void ResetLeftHandDownTimeout(bool refreshIkImmediate)
        {
            _leftHandNoInputCount = 0f;
            if (refreshIkImmediate)
            {
                _blendedLeftHand.Position = _leftHand.Position;
                _blendedLeftHand.Rotation = _leftHand.Rotation;
            }
        }

        public void ResetRightHandDownTimeout(bool refreshIkImmediate)
        {
            _rightHandNoInputCount = 0f;
            if (refreshIkImmediate)
            {
                _blendedRightHand.Position = _rightHand.Position;
                _blendedRightHand.Rotation = _rightHand.Rotation;
            }
        }

        private void Update()
        {
            UpdateLeft();
            UpdateRight();
            
            void UpdateLeft()
            {
                _leftHandNoInputCount += Time.deltaTime;
                if (_leftHandNoInputCount > HandIKIntegrator.AutoHandDownDuration)
                {
                    _leftHandBlendRate = Mathf.Max(0f, _leftHandBlendRate - HandDownBlendSpeed * Time.deltaTime);
                }
                else
                {
                    _leftHandBlendRate = Mathf.Min(1f, _leftHandBlendRate + HandUpBlendSpeed * Time.deltaTime);
                }

                //NOTE: 多くの場合ブレンド計算しないほうが計算が軽い
                if (_leftHandBlendRate > 0.999f)
                {
                    _blendedLeftHand.Position = _leftHand.Position;
                    _blendedLeftHand.Rotation = _leftHand.Rotation;
                }
                else if (_leftHandBlendRate < 0.001f)
                {
                    _blendedLeftHand.Position = DownHand.LeftHand.Position;
                    _blendedLeftHand.Rotation = DownHand.LeftHand.Rotation;
                }
                else
                {
                    var rate = Mathf.SmoothStep(0, 1, _leftHandBlendRate);
                    _blendedLeftHand.Position = Vector3.Lerp(
                        DownHand.LeftHand.Position, _leftHand.Position, rate
                        );
                    _blendedLeftHand.Rotation = Quaternion.Slerp(
                        DownHand.LeftHand.Rotation, _leftHand.Rotation, rate
                        );
                }
            }
            
            void UpdateRight()
            {                
                _rightHandNoInputCount += Time.deltaTime;
                if (_rightHandNoInputCount > HandIKIntegrator.AutoHandDownDuration)
                {
                    _rightHandBlendRate = Mathf.Max(0f, _rightHandBlendRate - HandDownBlendSpeed * Time.deltaTime);
                }
                else
                {
                    _rightHandBlendRate = Mathf.Min(1f, _rightHandBlendRate + HandUpBlendSpeed * Time.deltaTime);
                }

                //NOTE: 多くの場合ブレンド計算しないほうが計算が軽い
                if (_rightHandBlendRate > 0.999f)
                {
                    _blendedRightHand.Position = _rightHand.Position;
                    _blendedRightHand.Rotation = _rightHand.Rotation;
                }
                else if (_rightHandBlendRate < 0.001f)
                {
                    _blendedRightHand.Position = DownHand.RightHand.Position;
                    _blendedRightHand.Rotation = DownHand.RightHand.Rotation;
                }
                else
                {
                    var rate = Mathf.SmoothStep(0, 1, _rightHandBlendRate);
                    _blendedRightHand.Position = Vector3.Lerp(
                        DownHand.RightHand.Position, _rightHand.Position, rate
                        );
                    _blendedRightHand.Rotation = Quaternion.Slerp(
                        DownHand.RightHand.Rotation, _rightHand.Rotation, rate
                        );
                }
            }
        }

        private IEnumerator KeyDownRoutine(IKTargets target, Vector3 targetPos)
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

                if (rate >= 0.5f)
                {
                    break;
                }

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
                
                //一応Lerpしてるけどあんまり必要ないかもね
                ikTarget.Rotation = Quaternion.Slerp(
                    ikTarget.Rotation,
                    keyboardRot,
                    0.2f
                );

                yield return null;
            }

            //最後: キーを押し下げてるときの位置にぴったりあわせて終わり
            ikTarget.Position = targetPos; 
            ikTarget.Rotation = keyboardRot;
            
        }

        private IEnumerator KeyUpRoutine(IKTargets target, Vector3 targetPos)
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
            float targetVertical = Vector3.Dot(targetPos - keyboardRootPos, keyboardUp);
            
            while (Time.time - startTime < keyboardMotionDuration)
            {
                //NOTE: 歴史的経緯により、+0.5することで後半の動作をするように仕向ける
                float rate = (Time.time - startTime) / keyboardMotionDuration + 0.5f;

                //NOTE: キー上げの時点で水平方向が合ってなかった場合、ぴったり合わせてしまう(あえてlerpしない)
                Vector3 lerpApproach = targetPos;
                //Y成分に相当するところをキャンセルしておく
                lerpApproach -= keyboardUp * Vector3.Dot(lerpApproach - keyboardRootPos, keyboardUp);

                //離れるとき: キーボードから垂直方向に手を引き上げる。Lerpの係数は1から0に戻っていくことに注意
                float vertical = Mathf.Lerp(
                    targetVertical + YOffsetAfterKeyDown, 
                    targetVertical,
                    verticalApproachCurve.Evaluate(rate));
                ikTarget.Position = lerpApproach + keyboardUp * vertical;
                
                //一応Lerpしてるけどあんまり必要ないかもね
                ikTarget.Rotation = Quaternion.Slerp(
                    ikTarget.Rotation,
                    keyboardRot,
                    0.2f
                );

                if (rate >= 1.0f)
                {
                    break;
                }

                yield return null;
            }

            //最後: ピッタリ合わせておしまい
            ikTarget.Position = targetPos + keyboardUp * YOffsetAfterKeyDown; 
            ikTarget.Rotation = keyboardRot;
            
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


