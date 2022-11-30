using System.Threading;
using Cysharp.Threading.Tasks;
using UniGLTF.Extensions.VRMC_vrm;
using UnityEngine;
using UniVRM10;
using UniVRM10.Migration;

namespace Baku.VMagicMirror
{
    public static class Vrm10Validator
    {
        private static bool _isVrm10;
        
        public static async UniTask<bool> CheckModelIsVrm10(byte[] binary, CancellationToken cancellationToken)
        {
            _isVrm10 = false;
            Vrm10Instance instance = null;
            try
            {
                instance = await Vrm10.LoadBytesAsync(binary,
                    false,
                    ControlRigGenerationOption.None,
                    false,
                    vrmMetaInformationCallback: OnVrmMetaLoaded,
                    ct: cancellationToken
                );
            }
            finally
            {
                if (instance != null)
                {
                    Object.Destroy(instance.gameObject);
                }
            }

            return _isVrm10;
        }

        private static void OnVrmMetaLoaded(Texture2D thumbnail, Meta vrm10meta, Vrm0Meta vrm0meta)
        {
            _isVrm10 = vrm0meta == null && vrm10meta != null;
        }
    }
}
