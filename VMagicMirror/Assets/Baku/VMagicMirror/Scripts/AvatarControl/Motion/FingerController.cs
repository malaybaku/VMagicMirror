using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>指の制御をFKベースでどうにかするやつ</summary>
    /// <remarks>
    /// 指の動きは現状けっこう単純なのでいい加減に作ってます(その方が処理も軽いので)
    /// 指の角度を既定値に戻したいときは
    /// <see cref="ResetAllAngles"/>を呼んでから<see cref="Behaviour.enabled"/>をfalseにするとかでOK。
    /// </remarks>
    public class FingerController : MonoBehaviour
    {
        #region consts / readonly 

        private const string RDown = nameof(RDown);
        private const string MDown = nameof(MDown);
        private const string LDown = nameof(LDown);

        private const float DefaultBendingAngle = 10.0f;
        private const float Duration = 0.25f;
        private const float ThumbProximalMaxBendAngle = 30f;
        private const float HoldOperationSpeedFactor = 18.0f;

        //NOTE: 曲げ角度の符号に注意。左右で意味変わるのと、親指とそれ以外の差にも注意
        private static Dictionary<int, float[]> _fingerIdToPointingAngle = new Dictionary<int, float[]>()
        {
            [FingerConsts.RightThumb] = new float[] { 20, 20, 20 },
            [FingerConsts.RightIndex] = new float[] { -10, -10, -10 },
            [FingerConsts.RightMiddle] = new float[] { -80, -80, -80 },
            [FingerConsts.RightRing] = new float[] { -80, -80, -80 },
            [FingerConsts.RightLittle] = new float[] { -80, -80, -80 },
        };

        private static AnimationCurve _angleCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0, 10f, 1, 1),
            new Keyframe(0.125f, 25f, 1, -1),
            new Keyframe(0.25f, 10f, -1, -1),
        });

        #endregion

        private KeyboardProvider _keyboard = null;

        private bool _hasModel = false;
        private Animator _animator = null;

        //左手親指 = 0,
        //左手人差し指 = 1,
        //...
        //右手親指 = 5,
        //...
        //右手小指 = 9
        private Transform[][] _fingers = null;

        //右手首の位置。フォールバック系の処理で使うのでとっておく
        private Transform _rightWrist = null;

        private readonly bool[] _isAnimating = new bool[10];
        private readonly float[] _animationStartedTime = new float[10];
        private readonly bool[] _shouldHoldPressedMode = new bool[10];
        private readonly float[] _holdAngles = new float[10];
        //「指を曲げっぱなしにする/離す」というオペレーションによって決まる値
        private readonly float[] _holdOperationBendingAngle = new float[10];

        private Coroutine _coroutine = null;

        [Inject]
        public void Initialize(KeyboardProvider keyboard)
        {
            _keyboard = keyboard;
        }
        
        #region API

        /// <summary>
        /// モーション再生中などに、一時的に曲げ角度の適用をストップするとき立てるフラグ。
        /// </summary>
        public float ApplyRate { get; private set; } = 1.0f;

        public void FadeInWeight(float duration) => SetCoroutine(FadeFingerRate(1.0f, duration));
        public void FadeOutWeight(float duration) => SetCoroutine(FadeFingerRate(0.0f, duration));

        public bool RightHandPresentationMode { get; set; } = false;

        public void Initialize(Animator animator)
        {
            if(animator == null) { return; }

            _animator = animator;

            for (int i = 0; i < _isAnimating.Length; i++)
            {
                _isAnimating[i] = false;
                _animationStartedTime[i] = 0;
            }

            _rightWrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
            _fingers = new Transform[][]
            {
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightRingDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightRingProximal),
                },
                new Transform[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleProximal),
                },
            };

            ResetAllAngles();
            _hasModel = true;
        }

        public void Dispose()
        {
            _hasModel = false;
            _animator = null;
            _fingers = null;
            _rightWrist = null;
        }

        public void StartPressKeyMotion(string key, bool isLeftHandOnly)
        {
            StartMoveFinger(_keyboard.GetKeyTargetData(key, isLeftHandOnly).fingerNumber);
        }

        public void StartClickMotion(string info)
        {
            if (info == RDown)
            {
                StartMoveFinger(FingerConsts.RightMiddle);
            }
            else if (info == MDown || info == LDown)
            {
                StartMoveFinger(FingerConsts.RightIndex);
            }
        }

        public void ResetAllAngles()
        {
            for (int i = 0; i < _fingers.Length; i++)
            {
                float angle = (i < 5) ? DefaultBendingAngle : -DefaultBendingAngle;
                for (int j = 0; j < _fingers[i].Length; j++)
                {
                    if (_fingers[i][j] != null)
                    {
                        _fingers[i][j].localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    }
                }
            }
        }

        /// <summary>
        /// 特定の指の曲げ角度を固定します。ゲームパッドの入力を表現するために使うのを想定しています。
        /// </summary>
        /// <param name="fingerNumber"></param>
        /// <param name="angle"></param>
        public void Hold(int fingerNumber, float angle)
        {
            if (fingerNumber >= 0 && fingerNumber < _shouldHoldPressedMode.Length)
            {
                _shouldHoldPressedMode[fingerNumber] = true;
                _holdAngles[fingerNumber] = angle;
            }
        }

        /// <summary>
        /// <see cref="Hold"/>で押していた指を解放します。
        /// </summary>
        /// <param name="fingerNumber"></param>
        public void Release(int fingerNumber)
        {
            if (fingerNumber >= 0 && fingerNumber < _shouldHoldPressedMode.Length)
            {
                _shouldHoldPressedMode[fingerNumber] = false;
                _holdAngles[fingerNumber] = 0;
            }
        }

        /// <summary>
        /// 右手指先の位置を取得しようとします。
        /// モデルが未初期化だったり、VRM自体に指ボーンが入っていなかったりすると無効な値を返します。
        /// </summary>
        public void TryGetRightIndexTipPosition(out Vector3 result, out bool isValid)
        {
            Transform t = _fingers?[6]?[0];
            if (t != null)
            {
                result = t.position;
                isValid = true;
            }
            else
            {
                result = Vector3.zero;
                isValid = false;
            }
        }

        /// <summary>
        /// 右手首の位置を取得しようとします。
        /// モデルが未初期化の場合は無効な値を返します。
        /// </summary>
        public void TryGetRightWristPosition(out Vector3 result, out bool isValid)
        {
            if (_rightWrist != null)
            {
                result = _rightWrist.position;
                isValid = true;
            }
            else
            {
                result = Vector3.zero;
                isValid = false;
            }
        }
        
        #endregion

        private void LateUpdate()
        {
            if (!_hasModel)
            {
                return;
            }

            for (int i = 0; i < _isAnimating.Length; i++)
            {
                //プレゼンモード中、右手の形はギュッと握った状態
                if (i > 4 && RightHandPresentationMode)
                {
                    FixPointingHand(i);
                    continue;
                }

                float angle = DefaultBendingAngle;

                _holdOperationBendingAngle[i] = Mathf.Lerp(
                    _holdOperationBendingAngle[i],
                    _shouldHoldPressedMode[i] ? _holdAngles[i] : DefaultBendingAngle,
                    HoldOperationSpeedFactor * Time.deltaTime
                    );
                
                if (_isAnimating[i])
                {
                    float time = Time.time - _animationStartedTime[i];
                    if (time > Duration)
                    {
                        _isAnimating[i] = false;
                        time = Duration;
                    }
                    angle = _angleCurve.Evaluate(time);
                }
                else
                {
                    angle = _holdOperationBendingAngle[i];
                }

                //左右の手で回転方向が逆
                if (i > 4)
                {
                    angle = -angle;
                }

                
                for (int j = 0; j < _fingers[i].Length; j++)
                {
                    var t = _fingers[i][j];
                    //Holdのほうの値は正負考えずに入れるようになってるため、常にプラスで保存
                    _holdOperationBendingAngle[i] = Mathf.Abs(angle);
                    angle = LimitThumbBendAngle(angle, i, j);
                    if (t != null && ApplyRate > 0)
                    {
                        if (ApplyRate >= 1.0f)
                        {
                            t.localRotation = Quaternion.AngleAxis(angle, GetRotationAxis(i, j));
                        }
                        else
                        {
                            t.localRotation = Quaternion.Slerp(
                                t.localRotation, 
                                Quaternion.AngleAxis(angle, GetRotationAxis(i, j)),
                                ApplyRate
                                );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// インデックスで指定した指を動かす(0: Left Thumb, ..., 9: Right Little).
        /// </summary>
        /// <param name="fingerIndex"></param>
        private void StartMoveFinger(int fingerIndex)
        {
            if (fingerIndex >= 0 && fingerIndex < _isAnimating.Length)
            {
                _isAnimating[fingerIndex] = true;
                _animationStartedTime[fingerIndex] = Time.time;
            }
        }

        private void FixPointingHand(int index)
        {
            float[] angles = _fingerIdToPointingAngle[index];
            Transform[] targets = _fingers[index];

            for (int i = 0; i < angles.Length; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                //親指だけはy軸で回さないと指がうまく閉じない(※そもそもあまり触らない方がいいという説もある)
                Vector3 axis = (index == FingerConsts.RightThumb) ?
                    Vector3.up :
                    Vector3.forward;

                targets[i].localRotation = Quaternion.AngleAxis(angles[i], axis);
            }
        }

        private static Vector3 GetRotationAxis(int fingerNumber, int jointIndex)
        {
            if ((fingerNumber == FingerConsts.LeftThumb || fingerNumber == FingerConsts.RightThumb) && 
                jointIndex < 2
                )
            {
                return Vector3.down;
            }
            else
            {
                return Vector3.forward;
            }
        }

        private static float LimitThumbBendAngle(float angle, int fingerNumber, int jointIndex)
        {
            if (fingerNumber != FingerConsts.LeftThumb &&
                fingerNumber != FingerConsts.RightThumb)
            {
                return angle;
            }

            if (jointIndex != 2)
            {
                return angle;
            }

            return Mathf.Clamp(angle, -ThumbProximalMaxBendAngle, ThumbProximalMaxBendAngle);
        }

        private void SetCoroutine(IEnumerator coroutine)
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
            _coroutine = StartCoroutine(coroutine);
        }
        
        private IEnumerator FadeFingerRate(float goal, float duration)
        {
            if (duration <= 0)
            {
                ApplyRate = goal;
                yield break;
            }
            
            float startRate = ApplyRate;
            float start = Time.time;
            while (Time.time - start < duration)
            {
                float timeRate = (Time.time - start) / duration;
                ApplyRate = Mathf.Lerp(startRate, goal, timeRate);
                yield return null;
            }
            ApplyRate = goal;
        }
    }
}
