using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.FK
{
    /// <summary>
    /// Humanoidボーンのフレームをナイーブにリダクションするやつ。
    /// </summary>
    /// <remarks>
    /// Experimentalというか、「動くけど見栄えが微妙」という実装ステータス。
    /// やってないけど検討すべき事: 
    /// - タイピングで指がいちばん降りたタイミングなど、リダクション後に優先的に選択して表示するフレームを拾う仕組みを足す
    /// - (このクラスの仕事じゃないけど)捨てるフレームの情報を残像的な何かとして使う
    /// </remarks>
    public class BoneFrameReducer : MonoBehaviour
    {
        [Range(0.01f, 0.2f)] 
        [SerializeField] private float updateInterval = 0.05f;

        //NOTE: 揺れものはフルFPSでええねん、という事です
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new Dictionary<HumanBodyBones, Transform>();
        private readonly Dictionary<HumanBodyBones, Quaternion> _reducedRotations =
            new Dictionary<HumanBodyBones, Quaternion>();   
        private readonly Dictionary<HumanBodyBones, Quaternion> _tempRotations =
            new Dictionary<HumanBodyBones, Quaternion>();

        private Vector3 _reducedHipsPosition;
        private Vector3 _tempHipsPosition;

        private bool _hasModel;
        private float _updateCount = 0f;

        private bool _frameReduceEnabled = false;

        [Inject]
        public void Initialize(IMessageReceiver receiver, IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += OnModelLoaded;
            vrmLoadable.VrmDisposing += OnModelUnloaded;
            receiver.AssignCommandHandler(
                VmmCommands.UseFrameReduction,
                c => _frameReduceEnabled = c.ToBoolean()
            );
        }

        private void OnModelLoaded(VrmLoadedInfo info)
        {
            _bones.Clear();
            _reducedRotations.Clear();
            _tempRotations.Clear();

            for (var i = (int) HumanBodyBones.Hips; i < (int) HumanBodyBones.LastBone; i++)
            {
                var boneType = (HumanBodyBones) i;
                var bone = info.animator.GetBoneTransform(boneType);
                if (bone != null)
                {
                    _bones[boneType] = bone;
                    _reducedRotations[boneType] = Quaternion.identity;
                    _tempRotations[boneType] = Quaternion.identity;
                }
            }
            _hasModel = true;
            _updateCount = updateInterval;
        }

        private void OnModelUnloaded()
        {
            _bones.Clear();
            _hasModel = false;
        }

        private void Start()
        {
            StartCoroutine(RestoreFullFpsPose());
        }

        private void LateUpdate()
        {
            if (!_hasModel || !_frameReduceEnabled)
            {
                _updateCount = updateInterval;
                return;
            }

            //2つのことをやる
            // 1. 一定間隔で「reduceした状態で適用するボーン値」を取る
            // 2. ボーン情報をreduce値に全部書き換えて描画させてから描画後に元に戻す -> 揺れものの計算をいい感じにするため
            _updateCount += Time.deltaTime;
            if (_updateCount >= updateInterval)
            {
                _updateCount -= updateInterval;
                _reducedHipsPosition = _bones[HumanBodyBones.Hips].position;
                foreach (var bonePair in _bones)
                {
                    var rot = bonePair.Value.localRotation;
                    _reducedRotations[bonePair.Key] = rot;
                    _tempRotations[bonePair.Key] = rot;
                }
            }

            _tempHipsPosition = _bones[HumanBodyBones.Hips].position;
            foreach (var bonePair in _bones)
            {
                _tempRotations[bonePair.Key] = bonePair.Value.localRotation;
            }
            
            _bones[HumanBodyBones.Hips].position = _reducedHipsPosition;
            foreach (var rotPair in _reducedRotations)
            {
                _bones[rotPair.Key].localRotation = rotPair.Value;
            }
        }

        private IEnumerator RestoreFullFpsPose()
        {
            var eof = new WaitForEndOfFrame();
            while (true)
            {
                yield return eof;
                if (!_hasModel || !_frameReduceEnabled)
                {
                    continue;
                }

                _bones[HumanBodyBones.Hips].position = _tempHipsPosition;
                foreach (var rotPair in _tempRotations)
                {
                    _bones[rotPair.Key].localRotation = rotPair.Value;
                }
            }
        }
    }
}
