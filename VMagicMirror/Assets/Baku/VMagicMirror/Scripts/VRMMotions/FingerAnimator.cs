﻿using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class FingerAnimator : MonoBehaviour
    {
        //NOTE: 曲げ角度の符号に注意。左右で意味変わるのと、親指とそれ以外の差にも注意
        private static Dictionary<int, float[]> _fingerIdToPointingAngle = new Dictionary<int, float[]>()
        {
            [FingerConsts.RightThumb] = new float[] { 20, 20, 20 },
            [FingerConsts.RightIndex] = new float[] { -10, -10, -10 },
            [FingerConsts.RightMiddle] = new float[] { -80, -80, -80 },
            [FingerConsts.RightRing] = new float[] { -80, -80, -80 },
            [FingerConsts.RightLittle] = new float[] { -80, -80, -80 },
        };

        public float defaultBendingAngle = 10.0f;

        public float duration = 0.25f;

        public AnimationCurve angleCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0, 10f, 1, 1),
            new Keyframe(0.125f, 25f, 1, -1),
            new Keyframe(0.25f, 10f, -1, -1),
        });

        private Animator _animator;

        //左手親指 = 0,
        //左手人差し指 = 1,
        //...
        //右手親指 = 5,
        //...
        //右手小指 = 9
        private Transform[][] _fingers = null;

        public bool RightHandPresentationMode { get; private set; } = false;

        private bool[] _isAnimating = null;
        private float[] _animationStartedTime = null;

        public void Initialize(Animator animator)
        {
            _isAnimating = new bool[10];
            _animationStartedTime = new float[10];

            _animator = animator;
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

            for (int i = 0; i < _fingers.Length; i++)
            {
                float angle = (i < 5) ? defaultBendingAngle : -defaultBendingAngle;
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
        /// 
        /// </summary>
        /// <param name="fingerIndex">Finger index to move, from 0 (Left Thumb) to 9 (Right Little).</param>
        public void StartMoveFinger(int fingerIndex)
        {
            if (fingerIndex >= 0 && fingerIndex < _isAnimating.Length)
            {
                _isAnimating[fingerIndex] = true;
                _animationStartedTime[fingerIndex] = Time.time;
            }
        }


        public void FixRightHandToPresentationMode(bool fix)
        {
            RightHandPresentationMode = fix;
        }

        private void Start()
        {
            _isAnimating = new bool[10];
            _animationStartedTime = new float[10];
        }

        void LateUpdate()
        {
            if (_animator == null || _fingers == null)
            {
                return;
            }

            for (int i = 0; i < _isAnimating.Length; i++)
            {
                //右手人差し指はプレゼン中はプレゼンモードの指IKに任せたいので下手にいじらない
                if (i == FingerConsts.RightIndex && RightHandPresentationMode)
                {
                    continue;
                }

                //プレゼンモード中、右手の指はギュッと握った状態になっていてほしい
                if (i > 4 && RightHandPresentationMode)
                {
                    FixPointingHand(i);
                    continue;
                }



                float angle = defaultBendingAngle;

                if (_isAnimating[i])
                {
                    float time = Time.time - _animationStartedTime[i];
                    if (time > duration)
                    {
                        _isAnimating[i] = false;
                        time = duration;
                    }
                    angle = angleCurve.Evaluate(time);
                }

                //左右の手で回転方向が逆
                if (i > 4)
                {
                    angle = -angle;
                }

                foreach(var t in _fingers[i])
                {
                    if (t != null)
                    {
                        t.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    }
                }
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

    public static class FingerConsts
    {
        public const int LeftThumb = 0;
        public const int LeftIndex = 1;
        public const int LeftMiddle = 2;
        public const int LeftRing = 3;
        public const int LeftLittle = 4;
        public const int RightThumb = 5;
        public const int RightIndex = 6;
        public const int RightMiddle = 7;
        public const int RightRing = 8;
        public const int RightLittle = 9;
    }


}
