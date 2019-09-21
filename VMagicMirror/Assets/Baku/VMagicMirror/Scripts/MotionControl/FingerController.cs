﻿using System.Collections.Generic;
using UnityEngine;

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

        [SerializeField]
        private KeyboardProvider keyboard = null;

        private Animator _animator = null;

        //左手親指 = 0,
        //左手人差し指 = 1,
        //...
        //右手親指 = 5,
        //...
        //右手小指 = 9
        private Transform[][] _fingers = null;

        private readonly bool[] _isAnimating = new bool[10];
        private readonly float[] _animationStartedTime = new float[10];

        #region API

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
        }

        public void Dispose()
        {
            if (_animator == null)
            {
                return;
            }
            _animator = null;
            _fingers = null;
        }

        public void StartPressKeyMotion(string key)
        {
            StartMoveFinger(keyboard.GetKeyTargetData(key).fingerNumber);
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

        #endregion

        private void LateUpdate()
        {
            if (_animator == null || _fingers == null)
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

                //左右の手で回転方向が逆
                if (i > 4)
                {
                    angle = -angle;
                }

                foreach (var t in _fingers[i])
                {
                    if (t != null)
                    {
                        t.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
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
    }
}
