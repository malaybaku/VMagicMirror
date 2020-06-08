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
            _vrmLoadable = vrmLoadable;
            _sender = sender;
        }
        
        private IVRMLoadable _vrmLoadable;
        private IMessageSender _sender;

        private void Start()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            string names = string.Join(",",
                info.blendShape
                    .BlendShapeAvatar
                    .Clips
                    .Select(c => c.BlendShapeName)
                    .Where(n => !BasicNames.Contains(n))
            );

            _sender.SendCommand(
                MessageFactory.Instance.ExtraBlendShapeClipNames(names)
                );
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
