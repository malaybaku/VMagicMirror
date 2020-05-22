using System;
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
        [SerializeField] private VRoidLoaderDummy fallbackPrefab = null;
        
        private void Start()
        {
            if (fallbackPrefab == null)
            {
                Debug.LogWarning(
                    "VRoidConnectorのフォールバックprefabが設定されていません。" +
                    "UI上でVRoidHubへの接続処理を行うと終了でしか復帰できなくなることがあります。"
                    );
                return;
            }

            try
            {
                if (targetPrefab != null)
                {
                    Instantiate(targetPrefab);
                }
                else
                {
                    Instantiate(fallbackPrefab);
                }
            }
            catch (Exception ex)
            {
                //NOTE: レポジトリクローン時にはmissing component等が発生することがあります。
                LogOutput.Instance.Write(ex);
                //NOTE: 2回やってもダメな場合は諦める
                Instantiate(fallbackPrefab);
            }
        }
    }
}
