using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UniVRM10;
using VRM;
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
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new();
        private readonly Dictionary<HumanBodyBones, Quaternion> _reducedRotations = new();   
        private readonly Dictionary<HumanBodyBones, Quaternion> _tempRotations = new();

        private Vector3 _reducedHipsPosition;
        private Vector3 _tempHipsPosition;

        private bool _hasModel;
        private float _updateCount = 0f;

        private bool _frameReduceEnabled = false;

        private VRM10InstanceUpdater _instanceUpdater;
        private Vrm10Instance _vrm10Instance;

        [Inject]
        public void Initialize(
            IMessageReceiver receiver, 
            IVRMLoadable vrmLoadable,
            VRM10InstanceUpdater instanceUpdater
            )
        {
            vrmLoadable.VrmLoaded += OnModelLoaded;
            vrmLoadable.VrmDisposing += OnModelUnloaded;
            receiver.AssignCommandHandler(
                VmmCommands.UseFrameReductionEffect,
                c => _frameReduceEnabled = c.ToBoolean());
            _instanceUpdater = instanceUpdater;
        }

        private void OnModelLoaded(VrmLoadedInfo info)
        {
            _bones.Clear();
            _reducedRotations.Clear();
            _tempRotations.Clear();
            _vrm10Instance = info.instance;

            for (var i = (int) HumanBodyBones.Hips; i < (int) HumanBodyBones.LastBone; i++)
            {
                var boneType = (HumanBodyBones) i;
                var bone = info.controlRig.GetBoneTransform(boneType);
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
            _vrm10Instance = null;
            _hasModel = false;
        }

        private void Start()
        {
            _instanceUpdater.PreRuntimeProcess
                .Subscribe(_ => PreRuntimeProcess())
                .AddTo(this);
            _instanceUpdater.PostRuntimeProcess
                .Subscribe(_ => PostRuntimeProcess())
                .AddTo(this);
            // StartCoroutine(RestoreFullFpsPose());
        }

        //やってること
        // 1. 仮想骨が読み取られる前に「reduceした状態で適用するボーン値」を取って
        //   - キャッシュ値で上書きする
        //   - たまにキャッシュ値を更新する
        // 2. 仮想骨が転写されたら、上書きしてしまった値は戻してく
        //   - ※揺れものの計算に対して無害にするためだが、VRM1.0とこの処理は相性がすごく悪いかも…
        private void PreRuntimeProcess()
        {
            if (!_hasModel || !_frameReduceEnabled)
            {
                _updateCount = updateInterval;
                return;
            }

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

        private void PostRuntimeProcess()
        {
            if (!_hasModel || !_frameReduceEnabled)
            {
                return;
            }

            _bones[HumanBodyBones.Hips].position = _tempHipsPosition;
            foreach (var rotPair in _tempRotations)
            {
                _bones[rotPair.Key].localRotation = rotPair.Value;
            }
            
            //TODO: 揺れものの暴れ対策で必要なのはそうだが、メチャクチャ重たいはずなのでもっと軽い方法で代替したい…
            _vrm10Instance.Runtime.ReconstructSpringBone();
        }
    }
}
