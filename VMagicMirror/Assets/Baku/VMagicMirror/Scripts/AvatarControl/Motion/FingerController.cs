using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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
        private const string RUp = nameof(RUp);
        private const string MUp = nameof(MUp);
        private const string LUp = nameof(LUp);

        private const float DefaultBendingAngle = 10.0f;
        private const float Duration = 0.25f;
        private const float ThumbProximalMaxBendAngle = 30f;
        private const float HoldOperationSpeedFactor = 18.0f;
        private const float TypingBendingAngle = 25f;

        //タイピング用に指を折ったり戻したりする速度[deg/s]
        private const float TypingBeidSpeedPerSeconds = 15f / 0.125f;

        //NOTE: 曲げ角度の符号に注意。左右で意味変わるのと、親指とそれ以外の差にも注意
        private static Dictionary<int, float[]> _fingerIdToPointingAngle = new Dictionary<int, float[]>()
        {
            [FingerConsts.RightThumb] = new float[] { 20, 20, 20 },
            [FingerConsts.RightIndex] = new float[] { -10, -10, -10 },
            [FingerConsts.RightMiddle] = new float[] { -80, -80, -80 },
            [FingerConsts.RightRing] = new float[] { -80, -80, -80 },
            [FingerConsts.RightLittle] = new float[] { -80, -80, -80 },
        };


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
        //NOTE: _fingersに対して毎フレームnullチェックしないためにフラグを分ける
        private bool[][] _hasFinger = null;

        //右手首の位置。フォールバック系の処理で使うのでとっておく
        private Transform _rightWrist = null;

        private readonly bool[] _isAnimating = new bool[10];
        private readonly float[] _animationStartedTime = new float[10];
        private readonly bool[] _shouldHoldPressedMode = new bool[10];
        private readonly float[] _holdAngles = new float[10];
        //「指を曲げっぱなしにする/離す」というオペレーションによって決まる値
        private readonly float[] _holdOperationBendingAngle = new float[10];

        private readonly bool[] _isTypingBending = new bool[10];
        private readonly bool[] _isTypingReleasing = new bool[10];

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
            _fingers = new[]
            {
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftRingProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightThumbProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightIndexProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightRingDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightRingProximal),
                },
                new[]
                {
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleDistal),
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate),
                    _animator.GetBoneTransform(HumanBodyBones.RightLittleProximal),
                },
            };
            _hasFinger = new bool[10][];
            for (int i = 0; i < _hasFinger.Length; i++)
            {
                _hasFinger[i] = new bool[3];
                for (int j = 0; j < 3; j++)
                {
                    _hasFinger[i][j] = _fingers[i][j] != null;
                }
            }

            ResetAllAngles();
            _hasModel = true;
        }

        public void Dispose()
        {
            _hasModel = false;
            _animator = null;
            _fingers = null;
            _hasFinger = null;
            _rightWrist = null;
        }

        public void StartPressKeyMotion(string key, bool isLeftHandOnly)
        {
            StartMoveFinger(_keyboard.GetKeyTargetData(key, isLeftHandOnly).fingerNumber);
        }

        /// <summary>
        /// タイピング用に指を動かします。これは実際には、対応する指を探してHold()を呼び出す処理です。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isLeftHandOnly"></param>
        public void HoldTypingKey(string key, bool isLeftHandOnly)
        {
            var fingerNumber = _keyboard.GetKeyTargetData(key, isLeftHandOnly).fingerNumber;
            _isTypingBending[fingerNumber] = true;
            //普通こっちのフラグは立ってないハズだけど、念のため。
            _isTypingReleasing[fingerNumber] = false;

            //今から曲げようとしてる指以外が打鍵状態だったら離す
            int startIndex = (fingerNumber < 5) ? 0 : 5;
            for (int i = startIndex; i < startIndex + 5; i++)
            {
                if (i != fingerNumber && _isTypingBending[i])
                {
                    _isTypingBending[i] = false;
                    _isTypingReleasing[i] = true;
                }
            }
        }

        /// <summary>
        /// タイピング用に指定していた指をもとに戻します。これは実際には、対応する指を探してRelease()を呼び出す処理です。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isLeftHandOnly"></param>
        public void ReleaseTypingKey(string key, bool isLeftHandOnly)
        {
            var fingerNumber = _keyboard.GetKeyTargetData(key, isLeftHandOnly).fingerNumber;            
            _isTypingBending[fingerNumber] = false;
            _isTypingReleasing[fingerNumber] = true;
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

        public void OnMouseButton(string info)
        {
            if (info == RDown || info == MDown || info == LDown)
            {
                int downFinger = (info == RDown) ? FingerConsts.RightMiddle : FingerConsts.RightIndex;
                _isTypingBending[downFinger] = true;
                _isTypingReleasing[downFinger] = false;
                for (int i = 5; i < 10; i++)
                {
                    //NOTE: 親指とかの関係ない指にまでこの処理をかけるのはキーボードとの行き来のときに破綻を防ぐため
                    if (i != downFinger && _isTypingBending[i])
                    {
                        _isTypingBending[i] = false;
                        _isTypingReleasing[i] = true;
                    }
                }
            }
            else if (info == RUp)
            {
                _isTypingBending[FingerConsts.RightMiddle] = false;
                _isTypingReleasing[FingerConsts.RightMiddle] = true;
            }
            else if (info == MUp || info == LUp) 
            {
                _isTypingBending[FingerConsts.RightIndex] = false;
                _isTypingReleasing[FingerConsts.RightIndex] = true;
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

        /// <summary> 左手のすべての指に対し、タイピング動作のフラグを解除します。　</summary>
        public void ReleaseLeftHandTyping()
        {
            for (int i = 0; i < 5; i++)
            {
                _isTypingBending[i] = false;
                _isTypingReleasing[i] = false;
            }
        }

        /// <summary> 右手のすべての指に対し、タイピング動作のフラグを解除します。 </summary>
        public void ReleaseRightHandTyping()
        {
            for (int i = 5; i < 10; i++)
            {
                _isTypingBending[i] = false;
                _isTypingReleasing[i] = false;
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
                var currentAngle = _holdOperationBendingAngle[i];

                _holdOperationBendingAngle[i] = math.lerp(
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
                    angle = EvalAngleCurve(time);
                }
                else if (_isTypingBending[i])
                {
                    //Lerpではなく時間に対する線形変化によって指を折り曲げる
                    angle = math.min(currentAngle + TypingBeidSpeedPerSeconds * Time.deltaTime, TypingBendingAngle);
                }
                else if (_isTypingReleasing[i])
                {
                    //Bendingと同じ考え方でデフォルト角度に戻す
                    angle = math.max(currentAngle - TypingBeidSpeedPerSeconds * Time.deltaTime, DefaultBendingAngle);
                    if (!(angle > DefaultBendingAngle))
                    {
                        //リリース動作が終わったはずなのでフラグを折っておく: 余計な動作が起きにくいように。
                        _isTypingReleasing[i] = false;
                    }
                }
                else
                {
                    angle = _holdOperationBendingAngle[i];
                }

                //Holdのほうの値は正負考えずに入れるようになってるため、常にプラスで保存
                _holdOperationBendingAngle[i] = angle;
                
                //左右の手で回転方向が逆
                if (i > 4)
                {
                    angle = -angle;
                }

                
                for (int j = 0; j < _fingers[i].Length; j++)
                {
                    var t = _fingers[i][j];
                    angle = LimitThumbBendAngle(angle, i, j);
                    if (_hasFinger[i][j] && ApplyRate > 0)
                    {
                        if (ApplyRate >= 1.0f)
                        {
                            t.localRotation = GetFingerBendRotation(angle, i, j);
                        }
                        else if (ApplyRate > 0f)
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
                if (!_hasFinger[index][i])
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

        private static readonly Quaternion MajorFingerBendRotation =
            Quaternion.AngleAxis(DefaultBendingAngle, Vector3.forward);
        private static readonly Quaternion ThumbSpecialBendRotation =
            Quaternion.AngleAxis(DefaultBendingAngle, Vector3.down);

        //この関数で何をやるかというと、大多数の呼び出しでは同じ回転で用が足りるのを活用して
        //AngleAxisの呼び出し回数を削ります
        private static Quaternion GetFingerBendRotation(float angleDeg, int fingerNumber, int jointIndex)
        {
            var diff = (angleDeg - DefaultBendingAngle) * (angleDeg - DefaultBendingAngle);
            if (diff > 0.01)
            {
                //キャッシュ値が使えない: 普通に求める
                return Quaternion.AngleAxis(angleDeg, GetRotationAxis(fingerNumber, jointIndex));
            }
            
            //キャッシュ値が使える: あらかじめ用意してた値で返す
            if ((fingerNumber == FingerConsts.LeftThumb || fingerNumber == FingerConsts.RightThumb) && 
                jointIndex < 2
                )
            {
                return ThumbSpecialBendRotation;
            }
            else
            {
                return MajorFingerBendRotation;
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

            return math.clamp(angle, -ThumbProximalMaxBendAngle, ThumbProximalMaxBendAngle);
        }
        
        private static float EvalAngleCurve(float t)
        {
            //10 > 25 > 10と直線的に動かしたい
            if (t < 0.125f)
            {
                // return 10f + 15f * t / 0.125f;
                return 10f + 120f * t;
            }
            else
            {
                return 25f - (t - .125f) * 120f;
                // return 25f - 15f * (t - 0.125f) / 0.125f;
            }
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
                ApplyRate = math.lerp(startRate, goal, timeRate);
                yield return null;
            }
            ApplyRate = goal;
        }
    }
}
