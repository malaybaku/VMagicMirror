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

        private bool _forceStopHandIk = false;

        /// <summary>
        /// フェード等と無関係に手のIKを無効化するかどうかを取得、設定します。
        /// ユーザー設定でタイピング(+クリック)動作を切りたいときに使う想定。
        /// </summary>
        public bool ForceStopHandIk
        {
            get => _forceStopHandIk;
            set
            {
                if (_forceStopHandIk != value)
                {
                    _forceStopHandIk = value;
                    //FadeOutのほうでもわざわざAPIを叩いているのはfadeのカウントを上書きするため
                    if (value)
                    {
                        FadeOutArmIkWeights(0.5f);
                    }
                    else
                    {
                        FadeInArmIkWeights(0.5f);
                    }
                }
            }
        }
        
        public void OnVrmLoaded(VrmLoadedInfo info)
        {
            var ik = info.vrmRoot.GetComponent<FullBodyBipedIK>();
            _ik = ik;

            _originLeftShoulderPositionWeight = ik.solver.leftShoulderEffector.positionWeight;
            _originLeftHandPositionWeight = ik.solver.leftHandEffector.positionWeight;
            _originLeftHandRotationWeight = ik.solver.leftHandEffector.rotationWeight;

            _originRightShoulderPositionWeight = ik.solver.rightShoulderEffector.positionWeight;
            _originRightHandPositionWeight = ik.solver.rightHandEffector.positionWeight;
            _originRightHandRotationWeight = ik.solver.rightHandEffector.rotationWeight;

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

        /// <summary>直ちにIKのウェイトを0にします。</summary>
        public void FadeOutArmIkWeightsImmediately()
        {
            if (!_hasModel)
            {
                return;
            }
            _ik.solver.leftShoulderEffector.positionWeight = 0;
            _ik.solver.leftHandEffector.positionWeight = 0;
            _ik.solver.leftHandEffector.rotationWeight = 0;
            
            _ik.solver.rightShoulderEffector.positionWeight = 0;
            _ik.solver.rightHandEffector.positionWeight = 0;
            _ik.solver.rightHandEffector.rotationWeight = 0;

            elbowMotionModifier.ElbowIkRate = 0;
            _fadeCount = _fadeDuration;
            _isFadeOut = true;
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

            //強制で止めている場合、ここで一時的に0に上書きしてしまう(この方法だとリセットが簡単なため)
            if (ForceStopHandIk)
            {
                rate = 0;
            }

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
