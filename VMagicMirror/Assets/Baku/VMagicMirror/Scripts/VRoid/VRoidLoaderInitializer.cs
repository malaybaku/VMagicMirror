using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VRoidのロード処理を行うオブジェクトをInstantiateするだけのクラス。
    /// </summary>
    /// <remarks>
    /// ここのtargetPrefabにVRoidのクライアント実装が入りますが、レポジトリ上には本実装を含むprefabはありません。
    /// (SDKの利用ルールに沿う一環としてSDK周辺のコードをレポジトリから外してます)
    /// </remarks>
    public class VRoidLoaderInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject targetPrefab = null;
        
        private void Start()
        {
            if (targetPrefab != null)
            {
                Instantiate(targetPrefab);
            }
        }
    }
}
