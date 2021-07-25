using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// パーフェクトシンク中にブレンドシェイプを自動強調するのに用いるパラメータ設定群
    /// </summary>
    [CreateAssetMenu(menuName = "VMagicMirror/ExternalTracker/PerfectSyncEmphasizeSetting")]
    public class PerfectSyncEmphasizeSetting : ScriptableObject
    {
        [Serializable]
        public class ShapeItem
        {
            /// <summary>
            /// ブレンドシェイプのキー名
            /// </summary>
            public string keyName;

            /// <summary>
            /// 自然にFaceTrackingで認識できるブレンドシェイプの最大値。
            /// この値が1である場合、そのキーのブレンドシェイプは全く強調しない。
            /// </summary>
            [Range(0.1f, 1f)] 
            public float maxValue;
        }
        
        [SerializeField] ShapeItem[] items = LoadDefaultItems();
        public ShapeItem[] Items => items;

        private static ShapeItem[] LoadDefaultItems()
        {
            //NOTE: 
            return ExternalTrackerPerfectSync.Keys.PerfectSyncKeys
                .Select(k => new ShapeItem()
                {
                    keyName = k.Name,
                    //アゴは絶対に強調対象じゃない(やると見栄えも悪い)ので明示的に外す
                    maxValue = k.Name.Contains("Jaw") ? 1.0f : 0.1f,
                })
                .ToArray();
        }

    }

    public class PerfectSyncEmphasizeSettingsHandler
    {
        public PerfectSyncEmphasizeSettingsHandler(PerfectSyncEmphasizeSetting source)
        {
            _source = source;
            Items = _source.Items.ToDictionary(
                i => BlendShapeKey.CreateUnknown(i.keyName),
                i => i
            );
        }

        private readonly PerfectSyncEmphasizeSetting _source;
        public Dictionary<BlendShapeKey, PerfectSyncEmphasizeSetting.ShapeItem> Items { get; }

        public void SaveIfEditor()
        {
#if UNITY_EDITOR
            Debug.Log("Save Perfect Sync Emphasize Setting...");
            UnityEditor.EditorUtility.SetDirty(_source);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Perfect Sync Emphasize Setting was saved.");
#endif
        }
    }
}
