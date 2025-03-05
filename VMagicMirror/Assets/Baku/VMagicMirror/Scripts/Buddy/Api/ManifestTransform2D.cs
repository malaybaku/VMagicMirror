using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    /// <summary>
    /// manifest.json由来のTransform2DをAPIとして見せるためのガワ
    /// </summary>
    public class ManifestTransform2D : IReadOnlyTransform2D
    {
        public ManifestTransform2D(BuddyManifestTransform2DInstance instance)
        {
            _instance = instance;
        }

        private readonly BuddyManifestTransform2DInstance _instance;
        
        /// <summary> NOTE: Sprite2DApiなど、他のAPIで引数として本APIを受け取ったときに必要に応じて使う </summary>
        /// <returns></returns>
        internal BuddyManifestTransform2DInstance GetInstance() => _instance;
        
        // NOTE: Manifest由来のTransform2Dは常にCanvas直下に配置されるので、Localかどうかは考慮しないでOK
        public Vector2 LocalPosition => Position;
        public Vector2 Position => _instance.Position.ToApiValue();

        public Quaternion LocalRotation => Rotation;
        public Quaternion Rotation => _instance.Rotation.ToApiValue();

        public Vector2 LocalScale => _instance.Scale.ToApiValue();
    }
}
