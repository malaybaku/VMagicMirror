using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> ペンタブモードのときペンをいい感じに動かすやつ </summary>
    public class PenController : MonoBehaviour
    {
        [SerializeField] private Transform penRoot = null;
        [SerializeField] private MeshRenderer penMesh = null;

        private bool _hasModel = false;
        private bool _hasValidFinger = false;
        private Transform _rightWrist = null;
        private Transform _rightIndexProximal = null;
        private Transform _rightThumbIntermediate = null;
        
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
    }
}
