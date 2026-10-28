using System.Linq;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> VRMのロード時に標準以外のブレンドシェイプ名をチェックしてWPFに通知するやつ </summary>
    public class ExtraBlendShapeClipNamesSender : MonoBehaviour
    {
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageSender sender, PerfectSyncExpressionRepository expressions)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                var names = string.Join(",",
                    expressions.OriginalMap
                        .Keys
                        .Where(k => k.Preset == ExpressionPreset.custom)
                        .Select(k => k.Name)              
                    );
                sender.SendCommand(
                    MessageFactory.ExtraBlendShapeClipNames(names)
                );
            };
        }
    }
}
