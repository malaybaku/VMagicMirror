using VRM;

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
    public class EyeBoneAngleMapApplier
    {
        public EyeBoneAngleMapApplier(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposed;
        }

        private bool _hasBoneApplier;
        private VRMLookAtBoneApplyer _applier;

        public bool HasLookAtBoneApplier => _hasBoneApplier;

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

        private void OnVrmDisposed()
        {
            _applier = null;
            _hasBoneApplier = false;
        }

        //VRMLookAtBoneApplyer.ApplyRotationsを参考にしているが、pitchの正負向きが逆扱いなことに注意
        //TODO: 符号にめちゃくちゃ注意すること！左右でinner / outerが変わる事にも要注意

        public (float resultYaw, float resultPitch) GetLeftMappedValues(float yaw, float pitch)
        {
            if (!HasLookAtBoneApplier)
            {
                return (yaw, pitch);
            }

            var mappedYaw = yaw < 0
                ? -_applier.HorizontalOuter.Map(-yaw)
                : _applier.HorizontalInner.Map(yaw);

            var mappedPitch = pitch < 0
                ? -_applier.VerticalUp.Map(-pitch)
                : _applier.VerticalDown.Map(pitch);

            return (mappedYaw, mappedPitch);
        }

        public (float resultYaw, float resultPitch) GetRightMappedValues(float yaw, float pitch)
        {
            if (!HasLookAtBoneApplier)
            {
                return (yaw, pitch);
            }

            var mappedYaw = yaw < 0
                ? -_applier.HorizontalInner.Map(-yaw)
                : _applier.HorizontalOuter.Map(yaw);

            var mappedPitch = pitch < 0
                ? -_applier.VerticalUp.Map(-pitch)
                : _applier.VerticalDown.Map(pitch);

            return (mappedYaw, mappedPitch);
        }
    }
}
