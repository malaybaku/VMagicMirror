using System;
using UnityEngine;
using RootMotion.FinalIK;

namespace Baku.VMagicMirror
{
    public class IkWeightCrossFade : MonoBehaviour
    {
        [SerializeField] private ElbowMotionModifier elbowMotionModifier = null;
        
        private FullBodyBipedIK _ik = null;
        private bool _hasModel = false;

        private float _originLeftShoulderPositionWeight = 1.0f;
        private float _originLeftHandPositionWeight = 1.0f;
        private float _originLeftHandRotationWeight = 1.0f;

        private float _originRightShoulderPositionWeight = 1.0f;
        private float _originRightHandPositionWeight = 1.0f;
        private float _originRightHandRotationWeight = 1.0f;

        private bool _isFadeOut = false;
        private float _fadeCount = 0f;
        private float _fadeDuration = 0f;

        public void OnVrmLoaded(VrmLoadedInfo info)
        {
            _ik = info.fbbIk;
            _originLeftShoulderPositionWeight = _ik.solver.leftShoulderEffector.positionWeight;
            _originLeftHandPositionWeight = _ik.solver.leftHandEffector.positionWeight;
            _originLeftHandRotationWeight = _ik.solver.leftHandEffector.rotationWeight;

            _originRightShoulderPositionWeight = _ik.solver.rightShoulderEffector.positionWeight;
            _originRightHandPositionWeight = _ik.solver.rightHandEffector.positionWeight;
            _originRightHandRotationWeight = _ik.solver.rightHandEffector.rotationWeight;

            _hasModel = true;
        }

        public void OnVrmDisposing()
        {
            _hasModel = false;
            _ik = null;
        }

        /// <summary>
        /// 指定した秒数をかけて腕IKの回転、並進のIKウェイトを0にします。
        /// </summary>
        /// <param name="duration"></param>
        public void FadeOutArmIkWeights(float duration)
        {
            _isFadeOut = true;
            _fadeDuration = duration;
            _fadeCount = 0f;
        }

        /// <summary>
        /// 指定した秒数をかけて腕IKの回転、並進のIKウェイトをもともとの値にします。
        /// </summary>
        /// <param name="duration"></param>
        public void FadeInArmIkWeights(float duration)
        {
            _isFadeOut = false;
            _fadeDuration = duration;
            _fadeCount = 0f;
        }

        /// <summary>直ちにIKのウェイトをもとの値に戻します。</summary>
        public void FadeInArmIkWeightsImmediately()
        {
            if (!_hasModel)
            {
                return;
            }
            _ik.solver.leftShoulderEffector.positionWeight = _originLeftShoulderPositionWeight;
            _ik.solver.leftHandEffector.positionWeight = _originLeftHandPositionWeight;
            _ik.solver.leftHandEffector.rotationWeight = _originLeftHandRotationWeight;
            
            _ik.solver.rightShoulderEffector.positionWeight = _originRightShoulderPositionWeight;
            _ik.solver.rightHandEffector.positionWeight = _originRightHandPositionWeight;
            _ik.solver.rightHandEffector.rotationWeight = _originRightHandRotationWeight;
            
            elbowMotionModifier.ElbowIkRate = 1.0f;
            _fadeCount = _fadeDuration;
            _isFadeOut = false;
        }

        /// <summary>
        /// 「全身アニメーションを適用したい」という理由でIK weightを変化させたい場合に呼び出す
        /// </summary>
        /// <param name="targetWeight"></param>
        public void SetBodyMotionBasedIkWeightRequest(float targetWeight)
        {
            //TODO: 本当はほかのweightに乗算した値として適用したい + 脚とかElbowも全部必要な気がするんだよな～
            if (targetWeight > 0.5f)
            {
                FadeInArmIkWeights(0.5f);
            }
            else
            {
                FadeOutArmIkWeights(0.5f);
            }
        }
        
        private void Update()
        {
            if (!_hasModel || _fadeCount > _fadeDuration)
            {
                return;
            }

            _fadeCount += Time.deltaTime;

            float rate =
                _isFadeOut ?
                1.0f - (_fadeCount / _fadeDuration) :
                _fadeCount / _fadeDuration;
            rate = Mathf.Clamp(rate, 0f, 1f);
            rate = Mathf.SmoothStep(0, 1, rate);

            _ik.solver.leftShoulderEffector.positionWeight = _originLeftShoulderPositionWeight * rate;
            _ik.solver.leftHandEffector.positionWeight = _originLeftHandPositionWeight * rate;
            _ik.solver.leftHandEffector.rotationWeight = _originLeftHandRotationWeight * rate;

            _ik.solver.rightShoulderEffector.positionWeight = _originRightShoulderPositionWeight * rate;
            _ik.solver.rightHandEffector.positionWeight = _originRightHandPositionWeight * rate;
            _ik.solver.rightHandEffector.rotationWeight = _originRightHandRotationWeight * rate;

            elbowMotionModifier.ElbowIkRate = rate;
        }

    }
}
