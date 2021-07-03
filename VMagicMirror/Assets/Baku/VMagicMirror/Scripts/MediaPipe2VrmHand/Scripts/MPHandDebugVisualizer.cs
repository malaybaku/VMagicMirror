using Baku.VMagicMirror.IK;
using MediaPipe.HandPose;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// デバッグでBarracudaが吐いてる手の座標情報が見たいときに使うやつ
    /// </summary>
    public class MPHandDebugVisualizer : MonoBehaviour
    {
        [SerializeField] private MPHand source = null;

        [SerializeField] private Transform rootPrefab = null;
        [SerializeField] private Transform proximalPrefab = null;
        [SerializeField] private Transform interPrefab = null;
        [SerializeField] private Transform distalPrefab = null;
        [SerializeField] private Transform tipPrefab = null;

        //NOTE: 各prefabをrootに直置きすると捌きにくいため、ここに入れる
        [SerializeField] private Transform placeHolder = null;
        [SerializeField] private float scale = 1f;
        
        //NOTE: とりあえず左手だけにする。両手いきなりやるとわけわかんないので

        private Transform[] _leftHandObjects = new Transform[HandPipeline.KeyPointCount];
        
        private void Start()
        {
            _leftHandObjects[0] = Instantiate(rootPrefab, placeHolder);
            int index = 1;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var prefab = j == 0 ? proximalPrefab : j == 1 ? interPrefab : j == 2 ? distalPrefab : tipPrefab;
                    _leftHandObjects[index] = Instantiate(prefab, placeHolder);
                    index++;
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < _leftHandObjects.Length; i++)
            {
                _leftHandObjects[i].localPosition = source.LeftHandPoints[i] * scale;
            }
        }
    }
}
