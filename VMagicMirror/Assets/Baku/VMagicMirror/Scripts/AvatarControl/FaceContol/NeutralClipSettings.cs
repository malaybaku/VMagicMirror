using System;
using System.Linq;
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
        private bool _hasValidNeutralClipKey;
        private ExpressionKey _neutralClipKey;

        private bool _hasValidOffsetClipKey;
        private ExpressionKey[] _offsetClipKeys = Array.Empty<ExpressionKey>();

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.FaceNeutralClip,
                c =>
                {
                    _hasValidNeutralClipKey = !string.IsNullOrWhiteSpace(c.GetStringValue());
                    if (_hasValidNeutralClipKey)
                    {
                        _neutralClipKey = ExpressionKeyUtils.CreateKeyByName(
                            BlendShapeCompatUtil.GetVrm10ClipName(c.GetStringValue())
                            );
                    }
                });

            receiver.AssignCommandHandler(
                VmmCommands.FaceOffsetClip,
                c =>
                {
                    var stringValue = c.GetStringValue();
                    _hasValidOffsetClipKey = !string.IsNullOrWhiteSpace(stringValue);
                    if (_hasValidOffsetClipKey)
                    {
                        _offsetClipKeys = stringValue.Split('\t')
                            .Select(v => 
                                ExpressionKeyUtils.CreateKeyByName(BlendShapeCompatUtil.GetVrm10ClipName(v)))
                            .ToArray();
                    }
                });
        }
        
        public void AccumulateNeutralClip(ExpressionAccumulator accumulator, float weight = 1f) 
        {
            if (_hasValidNeutralClipKey)
            {
                accumulator.Accumulate(_neutralClipKey, weight);
            }
        }

        public void AccumulateOffsetClip(ExpressionAccumulator accumulator)
        {
            if (_hasValidOffsetClipKey)
            {
                foreach (var key in _offsetClipKeys)
                {
                    accumulator.Accumulate(key, 1f);
                }
            }
        }
    }
}
