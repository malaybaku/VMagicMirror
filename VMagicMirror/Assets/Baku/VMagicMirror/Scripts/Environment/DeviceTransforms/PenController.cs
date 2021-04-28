using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> ペンタブモードのときペンをいい感じに動かすやつ </summary>
    public class PenController : MonoBehaviour
    {
        private const float AnimationDuration = 0.3f;
        private const Ease AnimationEase = Ease.OutCubic;
        
        [SerializeField] private Transform penRoot = null;
        [SerializeField] private MeshRenderer penMesh = null;

        private bool _hasModel = false;
        private bool _hasValidFinger = false;
        private Transform _rightWrist = null;
        private Transform _rightIndexProximal = null;
        private Transform _rightThumbIntermediate = null;


        private bool _isVisible = false;
        
        private bool _isRightHandOnPenTablet = false;
        private bool _isPenTabletVisible = false;
        private bool ShouldVisible => _isRightHandOnPenTablet && _isPenTabletVisible;
        
        private TweenerCore<Vector3, Vector3, VectorOptions> _tweener;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, PenTabletProvider penTabletProvider)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _rightWrist = info.animator.GetBoneTransform(HumanBodyBones.RightHand);
                _rightIndexProximal = info.animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
                _rightThumbIntermediate = info.animator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
                _hasValidFinger = (_rightIndexProximal != null && _rightThumbIntermediate != null);
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hasValidFinger = false;
                _rightWrist = null;
                _rightIndexProximal = null;
                _rightThumbIntermediate = null;
            };

            penMesh.enabled = false;
        }
        
        
        /// <summary>
        /// そもそもペンタブが表示されてるかどうか、という点から表示状態を更新します。
        /// </summary>
        /// <param name="visible"></param>
        public void SetDeviceVisibility(bool visible)
        {
            _isPenTabletVisible = visible;
            UpdateVisibility();
        }

        /// <summary>
        /// 右手がペンタブの上にあるかどうか、という点から表示状態を更新します。
        /// </summary>
        /// <param name="isOn"></param>
        public void SetHandIsOnPenTablet(bool isOn)
        {
            _isRightHandOnPenTablet = isOn;
            UpdateVisibility();
        }
        
        private void Update()
        {
            //NOTE: 指がないとペンの位置が決まらんから勘弁してくれ～！という理論です。はい。
            if (!_hasModel || !_hasValidFinger)
            {
                penMesh.enabled = false;
                return;
            }

            penMesh.enabled = true;

            //手に対して位置を合わせる。この結果としてパーティクルとペン先がちょっとズレる事があるが、それはOKという事にする
            penRoot.position = (_rightIndexProximal.position + _rightThumbIntermediate.position) * 0.5f;
            penRoot.localRotation = _rightWrist.rotation * Quaternion.AngleAxis(20f, Vector3.right);
        }

        private void UpdateVisibility()
        {
            if (_isVisible == ShouldVisible)
            {
                return;
            }

            //Deformer使うほどでもないのでscaleで適当にやってます
            _tweener?.Kill();
            _isVisible = ShouldVisible;
            if (_isVisible)
            {
                penRoot.localScale = new Vector3(1, 0, 1);
                penMesh.enabled = true;
                _tweener = penRoot
                    .DOScaleY(1f, AnimationDuration)
                    .SetEase(AnimationEase);
            }
            else
            {
                _tweener = penRoot
                    .DOScaleY(0f, AnimationDuration)
                    .SetEase(AnimationEase)
                    .OnComplete(() => penMesh.enabled = false);
            }
        }
    }
}
