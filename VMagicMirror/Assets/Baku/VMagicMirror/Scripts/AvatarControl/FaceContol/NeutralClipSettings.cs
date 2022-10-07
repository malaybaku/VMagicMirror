using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// デフォルト表情関連の設定を保持して適用するクラスです。
    /// とくにPresetのNeutralクリップについて、実態(VMCとかがやってる)に即して任意で有効化できるようにするのが狙いです。
    /// </summary>
    public class NeutralClipSettings : MonoBehaviour
    {
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.FaceNeutralClip,
                c =>
                {
                    HasValidNeutralClipKey = !string.IsNullOrWhiteSpace(c.Content);
                    if (HasValidNeutralClipKey)
                    {
                        NeutralClipKey = ExpressionKeyUtils.CreateKeyByName(c.Content);
                    }
                });

            receiver.AssignCommandHandler(
                VmmCommands.FaceOffsetClip,
                c =>
                {
                    HasValidOffsetClipKey = !string.IsNullOrWhiteSpace(c.Content);
                    if (HasValidOffsetClipKey)
                    {
                        OffsetClipKey = ExpressionKeyUtils.CreateKeyByName(c.Content);
                    }
                });
        }
        
        public void AccumulateNeutralClip(ExpressionAccumulator accumulator, float weight = 1f) 
        {
            if (HasValidNeutralClipKey)
            {
                accumulator.Accumulate(NeutralClipKey, weight);
            }
        }

        public void AccumulateOffsetClip(ExpressionAccumulator accumulator)
        {
            if (HasValidOffsetClipKey)
            {
                accumulator.Accumulate(OffsetClipKey, 1f);
            }
        }

        private bool HasValidNeutralClipKey = false;
        private ExpressionKey NeutralClipKey;

        private bool HasValidOffsetClipKey = false;
        private ExpressionKey OffsetClipKey;
        
    }
}
