using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPHumanoidVisualizer : MonoBehaviour
    {
        [SerializeField] private GameObject boneItemPrefab;
        [SerializeField] private Transform target;
        
        private Transform _target;

        private void Update()
        {
            if (_target == target)
            {
                return;
            }

            _target = target;
            if (_target != null && boneItemPrefab != null)
            {
                SetupVisualization(_target);
            }
        }

        private void SetupVisualization(Transform t)
        {
            var count = t.childCount;
            for (var i = 0; i < count; i++)
            {
                SetupVisualization(t.GetChild(i));
            }

            //順番が大事: 先にアイテムをアタッチしちゃうと、そのアイテムもforで考慮されてしまうので…
            var item = Instantiate(boneItemPrefab);
            var itemTransform = item.transform;
            itemTransform.SetParent(t);
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localRotation = Quaternion.identity;
        }
    }
}
