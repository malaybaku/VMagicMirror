using System.Linq;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VRMのロード時に規格外のブレンドシェイプ名をチェックしてWPFに通知するやつ
    /// </summary>
    public class ExtraBlendShapeClipNamesSender : MonoBehaviour
    {
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageSender sender)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                string names = string.Join(",",
                    info.blendShape
                        .BlendShapeAvatar
                        .Clips
                        .Select(c => c.BlendShapeName)
                        .Where(n => !BasicNames.Contains(n))
                );
                
                sender.SendCommand(
                    MessageFactory.Instance.ExtraBlendShapeClipNames(names)
                );
            };
        }

        private static readonly string[] BasicNames = new[]
        {
            "Joy",
            "Angry",
            "Sorrow",
            "Fun",

            "A",
            "I",
            "U",
            "E",
            "O",

            "Neutral",
            "Blink",
            "Blink_L",
            "Blink_R",

            "LookUp",
            "LookDown",
            "LookLeft",
            "LookRight",
        };
    }
}
