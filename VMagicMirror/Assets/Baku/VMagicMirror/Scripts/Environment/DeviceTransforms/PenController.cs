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
        
        //厳格なペンの半分の長さ、に1mmだけオフセットをつけたもの。パーティクルを出すことを考慮して1mmだけずらします
        private const float ExactPenHalfLength = 0.0851f;
        //レイキャストでは少し大きな値にすることで、ペンがタブレットの手前に浮くようにする
        private const float RaycastPenHalfLength = 0.095f;
        
        [SerializeField] private Transform penRoot = null;
        [SerializeField] private MeshRenderer penMesh = null;

        private bool _hasModel = false;
        private bool _hasValidFinger = false;
        private Transform _rightWrist = null;
        private Transform _rightIndexProximal = null;
        private Transform _rightThumbIntermediate = null;


        private bool _isVisible = false;
        //モデルに指がないから(他の条件はクリアしてるけど)ペンを非表示にしてる、というフラグ。
        //指なしモデル→指ありモデルに切り替わったときにペンを再表示するために使います
        private bool _penMeshDisabledBecauseOfInvalidFinger = false;
        
        private bool _isRightHandOnPenTablet = false;
        private bool _isPenVisible = true;
        private bool ShouldVisible => _isRightHandOnPenTablet && _isPenVisible;

        private Collider _collider = null;
        
        private TweenerCore<Vector3, Vector3, VectorOptions> _tweener;

        private IMessageSender _sender;

        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender, IVRMLoadable vrmLoadable)
        {
            _sender = sender;
            receiver.AssignCommandHandler(
                VmmCommands.SetPenVisibility,
                message => SetDeviceVisibility(message.ToBoolean())
                );
            
            vrmLoadable.VrmLoaded += info =>
            {
                _rightWrist = info.controlRig.GetBoneTransform(HumanBodyBones.RightHand);
                _rightIndexProximal = info.controlRig.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
                _rightThumbIntermediate = info.controlRig.GetBoneTransform(HumanBodyBones.RightThumbDistal);
                _hasValidFinger = (_rightIndexProximal != null && _rightThumbIntermediate != null);
                _hasModel = true;
                _sender.SendCommand(MessageFactory.SetModelDoesNotSupportPen(!_hasValidFinger));
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hasValidFinger = false;
                _rightWrist = null;
                _rightIndexProximal = null;
                _rightThumbIntermediate = null;
                //モデルがロードされてないならサポート外警告は不要、とする。分かりやすいので
                _sender.SendCommand(MessageFactory.SetModelDoesNotSupportPen(false));
            };

            penMesh.enabled = false;
        }
        
        /// <summary>
        /// ペンがめり込んではいけないペンタブのコライダーを割り当てます。
        /// </summary>
        /// <param name="penTabletCollider"></param>
        public void AssignPenTabletCollider(Collider penTabletCollider)
        {
            _collider = penTabletCollider;
        }
        
        // ペン単体について表示したいかどうか、というのを更新する。
        // NOTE: これにtrueを渡しても実際にペンタブモードになってなければペンは出ない。
        private void SetDeviceVisibility(bool visible)
        {
            _isPenVisible = visible;
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

        public Vector3 GetTipPosition() => penRoot.position + penRoot.up * ExactPenHalfLength;
        
        private void Update()
        {
            if (!_hasModel)
            {
                return;
            }

            //NOTE: 指がないとペンの位置が決まらんから勘弁してくれ～！というガード
            if (!_hasValidFinger)
            {
                _penMeshDisabledBecauseOfInvalidFinger = true;
                penMesh.enabled = false;
                return;
            }

            //NOTE: このif文を通るのは「指ボーンがないモデルの後に指ボーンがあるモデルを読み込んだ」みたいなケース。
            //珍しいパターンのはずなためアニメーションせず、素朴に再表示だけやる
            if (_isVisible && _penMeshDisabledBecauseOfInvalidFinger)
            {
                penMesh.enabled = true;
                _penMeshDisabledBecauseOfInvalidFinger = false;
            }

            //上記以外の場合、UpdateVisibilityによってメッシュのvisibilityは制御される

            //手に対して位置を合わせに行く。分かりやすいので
            var pos = (_rightIndexProximal.position + _rightThumbIntermediate.position) * 0.5f;
            //NOTE: 200は書き間違いじゃなくて、ペンの上下をほぼひっくり返すためにやってます
            penRoot.localRotation =
                _rightWrist.rotation *
                Quaternion.AngleAxis(200f, Vector3.right);

            //NOTE: ペン先が明らかに突き抜けていれば手前に戻す。レイキャストの方向にだけ注意
            var up = penRoot.up;
            if (_collider.Raycast(
                new Ray(pos + up * RaycastPenHalfLength, -up), out var hit, 10f
                ))
            {
                penRoot.position = pos - up * hit.distance;
            }
            else
            {
                penRoot.position = pos;
            }
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
