using System.Collections.Generic;
using System.Threading.Tasks;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class VrmApi : IVrm
    {
        private readonly BuddyVrmInstance _instance;
        private readonly BuddyLogger _logger;
        private BuddyFolder BuddyFolder { get; }

        public VrmApi(BuddyFolder buddyFolder, BuddyVrmInstance instance, BuddyLogger logger)
        {
            BuddyFolder = buddyFolder;
            _instance = instance;
            _logger = logger;
            Transform = new Transform3D(instance.GetTransform3D());
        }
        
        public override string ToString() => nameof(IVrm);

        public ITransform3D Transform { get; }

        public async Task LoadAsync(string path) => await ApiUtils.TryAsync(
            BuddyFolder,
            _logger,
            async () =>
            {
                //TODO: パス不正とかファイルが見つからないとかの場合に1回だけエラーが出したい
                // Sprite2Dの処理を使い回せるとgood
                var fullPath = ApiUtils.GetAssetFullPath(BuddyFolder, path);
                await _instance.LoadAsync(fullPath);
            });

        // NOTE: こっちのほうが成功しやすいが、スレッド例外とかは普通に出るはずなのでくくっておく
        public async Task LoadPresetAsync(string name) => await ApiUtils.TryAsync(
            BuddyFolder,
            _logger,
            async () => await _instance.LoadPresetAsync(name)
            );

        public void Show() => _instance.SetActive(true);
        public void Hide() => _instance.SetActive(false);

        public void SetBoneRotation(HumanBodyBones bone, Quaternion localRotation)
            => _instance.SetBoneRotation(bone.ToEngineValue(), localRotation.ToEngineValue());
        public void SetHipsLocalPosition(Vector3 position) 
            => _instance.SetHipsLocalPosition(position.ToEngineValue());
        public void SetHipsPosition(Vector3 position) 
            => _instance.SetHipsPosition(position.ToEngineValue());

        public void SetBoneRotations(IReadOnlyDictionary<HumanBodyBones, Quaternion> localRotations)
        {
            // NOTE: Dictごと渡す方が早そうな場合、Keyの型変換をAPIで行うのをサボってもOK
            foreach (var (bone, rotation) in localRotations)
            {
                _instance.SetBoneRotation(bone.ToEngineValue(), rotation.ToEngineValue());
            }
        }

        public void SetMuscles(float?[] muscles) => _instance.SetMuscles(muscles);

        public string[] GetCustomBlendShapeNames() => _instance.GetCustomBlendShapeNames();

        public bool HasBlendShape(string name) => _instance.HasCustomBlendShape(name);

        public float GetBlendShape(string name, bool customClip) => _instance.GetBlendShapeValue(name, customClip);

        public void SetBlendShape(string name, bool customClip, float value)
            => _instance.SetBlendShapeValue(name, customClip, value);
        
        public void RunVrma(IVrmAnimation animation, bool loop, bool immediate)
        {
            var api = (VrmAnimationApi)animation;
            _instance.RunVrma(api.Instance, immediate);
        }

        public void StopVrma(bool immediate) => _instance.StopVrma(immediate);
    }
}
