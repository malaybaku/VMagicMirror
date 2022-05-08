using UnityEngine;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// UniVRMの実装ではないLookAtや「目を閉じるとき眼球が下向きになる」などの処理で適用された眼球回転について、
    /// VRMに定義された目ボーンのMappingに即した値に上書きするための値を提供するクラス。
    /// </summary>
    /// <remarks>
    /// この処理を使う場合は次の2点に注意する。
    /// - must: 呼び出しタイミングは極めて遅めにする
    /// - should: VRMのLookAtは動いていないべき
    /// </remarks>
    public class EyeBoneAngleMapApplier : MonoBehaviour
    {
        //NOTE: この値はGUIの設定でオンオフしたい
        [SerializeField] private bool applyMapping;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposed;
        }

        private bool _hasBoneApplier;
        private VRMLookAtBoneApplyer _applier;

        public bool NeedOverwrite => applyMapping;

        private void OnVrmDisposed()
        {
            _applier = null;
            _hasBoneApplier = false;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            var boneApplier = info.vrmRoot.GetComponent<VRMLookAtBoneApplyer>();
            if (boneApplier == null)
            {
                _hasBoneApplier = false;
                return;
            }

            _hasBoneApplier = true;
            _applier = boneApplier;
        }

        public Quaternion GetLeftEyeRotation(Quaternion localRot)
        {
            if (!_hasBoneApplier)
            {
                return localRot;
            }
            
            var (yaw, pitch) = SeparateRotation(localRot);
            Debug.Log($"Separated rot = {yaw:0.0}, {pitch:0.0}");
            return GetLeftEyeRotation(yaw, pitch);
        }
        
        public Quaternion GetRightEyeRotation(Quaternion localRot)
        {
            if (!_hasBoneApplier)
            {
                return localRot;
            }
            
            var (yaw, pitch) = SeparateRotation(localRot);
            return GetRightEyeRotation(yaw, pitch);
        }

        //NOTE: 下記でyaw, pitchはdeg単位, かつ_applierが非nullなことが保証されている
        //VRMLookAtBoneApplyer.ApplyRotationsを参考にしているが、pitchの正負向きが逆扱いなことに注意
        
        //TODO: 符号にめちゃくちゃ注意すること！左右でinner / outerが変わる事にも要注意
        private Quaternion GetLeftEyeRotation(float yaw, float pitch)
        {
            var mappedYaw = yaw < 0
                ? -_applier.HorizontalOuter.Map(-yaw)
                : _applier.HorizontalInner.Map(yaw);

            var mappedPitch = pitch < 0
                ? _applier.VerticalUp.Map(-pitch)
                : _applier.VerticalDown.Map(pitch);

            Debug.Log($"Mapped rot = {mappedYaw:0.0}, {mappedPitch:0.0}");
            return Quaternion.Euler(mappedPitch, mappedYaw, 0f);
        }

        private Quaternion GetRightEyeRotation(float yaw, float pitch)
        {
            var mappedYaw = yaw < 0
                ? -_applier.HorizontalInner.Map(-yaw)
                : _applier.HorizontalOuter.Map(yaw);

            var mappedPitch = pitch < 0
                ? _applier.VerticalUp.Map(-pitch)
                : _applier.VerticalDown.Map(pitch);

            return Quaternion.Euler(mappedPitch, mappedYaw, 0f);
        }

        //目ボーンの回転をヨーとピッチ(degree)に変換する。
        private static (float yaw, float pitch) SeparateRotation(Quaternion rot)
        {
            //NOTE: 常識的に目ボーンの回転値にはロール運動が効いてないはずだが、それを前提にせず、素朴に計算する。
            // eulerAnglesは計算ミスが怖いので避ける。
            var direction = rot * Vector3.forward;
            return (
                MathUtil.ClampedAtan2Deg(direction.x, direction.z),
                -Mathf.Rad2Deg * Mathf.Asin(direction.y)
            );
        }
    }
}
