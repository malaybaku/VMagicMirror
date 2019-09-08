using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 読み込んだVRMからブレンドシェイプ情報を引き抜いて保持するやつ
    /// </summary>
    public class VRMBlendShapeStore
    {
        private BlendShapeStoreItem[] _blendShapeItems = new BlendShapeStoreItem[0];
        private string[] _blendShapeNames = new string[0];

        public bool IsInitialized { get; private set; } = false;

        public void OnVrmLoaded(VrmLoadedInfo info)
        {
            InitializeBlendShapeItems(info.vrmRoot);
            IsInitialized = true;
        }

        public void OnVrmDisposing()
        {
            IsInitialized = false;
            _blendShapeItems = new BlendShapeStoreItem[0];
            _blendShapeNames = new string[0];
        }

        //NOTE: 配列そのまま返してるのはパフォーマンス配慮
        public BlendShapeStoreItem[] GetBlendShapeStoreItems() => _blendShapeItems;

        //NOTE: 配列そのまま返してるのはパフォーマンス配慮
        public string[] GetBlendShapeNames()
        {
            //初期化時点では名前一覧は作ってないので遅れて初期化するイメージ
            if (_blendShapeNames.Length != _blendShapeItems.Length)
            {
                //頻繁に呼ばれない想定なのでLinqで書いてます
                _blendShapeNames = _blendShapeItems
                    .Select(i => i.name)
                    .OrderBy(n => n)
                    .ToArray();
            }
            return _blendShapeNames;
        }

        private void InitializeBlendShapeItems(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var renderers = HierarchyUtils
                .GetAllChildrenRecurse(target)
                .Select(t =>
                {
                    try
                    {
                        return t.GetComponent<SkinnedMeshRenderer>();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                })
                .Where(r => r != null)
                .ToArray();

            var blendShapes = new List<BlendShapeStoreItem>();

            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var mesh = renderer.sharedMesh;
                for (int j = 0; j < mesh.blendShapeCount; j++)
                {
                    string name = mesh.GetBlendShapeName(j);
                    blendShapes.Add(new BlendShapeStoreItem()
                    {
                        renderer = renderer,
                        name = name,
                        index = mesh.GetBlendShapeIndex(name),
                    });
                }
            }

            _blendShapeItems = blendShapes.ToArray();
        }
    }
}
