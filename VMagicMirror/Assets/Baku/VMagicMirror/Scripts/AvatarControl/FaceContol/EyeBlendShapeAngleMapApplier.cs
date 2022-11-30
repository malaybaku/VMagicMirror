using UnityEngine;
using UniVRM10;
using LookAtType = UniGLTF.Extensions.VRMC_vrm.LookAtType;

namespace Baku.VMagicMirror
{
    public readonly struct EyeBlendShapeResult
    {
        public EyeBlendShapeResult(float left, float right, float up, float down)
        {
            Left = left;
            Right = right;
            Up = up;
            Down = down;
        }
        public float Left { get; }
        public float Right { get; }
        public float Up { get; }
        public float Down { get; }
    }

    /// <summary>
    /// UniVRMの実装ではないLookAtや「目を閉じるとき眼球が下向きになる」などの処理で適用された眼球回転について、
    /// VRMに定義された目ボーンのMappingに即した値に上書きするための値を提供するクラス。
    /// </summary>
    /// <remarks>
    /// この処理を使う場合は次の2点に注意する。
    /// - must: 呼び出しタイミングは極めて遅めにする
    /// - should: VRMのLookAtは動いていないべき
    /// </remarks>
    public class EyeBlendShapeMapApplier
    {
        public EyeBlendShapeMapApplier(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposed;
        }

        public bool _hasApplier { get; private set; }
        private VRM10ObjectLookAt _applier;

        public bool NeedOverwrite => _hasApplier;

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            if (info.instance.Vrm.LookAt.LookAtType != LookAtType.expression)
            {
                _hasApplier = false;
                return;
            }

            _hasApplier = true;
            _applier = info.instance.Vrm.LookAt;
        }

        private void OnVrmDisposed()
        {
            _applier = null;
            _hasApplier = false;
        }

        //VRMLookAtBlendShapeApplyer.ApplyRotationsを参考にしているが、pitchの正負向きが逆扱いなことに注意
        public EyeBlendShapeResult GetMappedValues(float yaw, float pitch)
        {
            if (!NeedOverwrite)
            {
                return new EyeBlendShapeResult(0, 0, 0, 0);
            }

            var left = 0f;
            var right = 0f;
            //NOTE: 挙動が怪しいかも…
            if (yaw < 0)
            {
                left = Mathf.Clamp01(_applier.HorizontalInner.Map(-yaw));
            }
            else
            {
                right = Mathf.Clamp01(_applier.HorizontalOuter.Map(yaw));
            }

            var up = 0f;
            var down = 0f;
            if (pitch < 0)
            {
                up = Mathf.Clamp01(-_applier.VerticalUp.Map(-pitch));
            }
            else
            {
                down = Mathf.Clamp01(_applier.VerticalDown.Map(pitch));
            }

            return new EyeBlendShapeResult(left, right, up, down);
        }
    }
}
