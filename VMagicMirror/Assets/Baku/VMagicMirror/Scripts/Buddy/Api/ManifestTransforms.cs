using System.Collections.Generic;
using System.Linq;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class ManifestTransformsApi : IManifestTransforms
    {
        private readonly Dictionary<string, ManifestTransform2D> _transform2d;
        private readonly Dictionary<string, ManifestTransform3D> _transform3d;

        public ManifestTransformsApi(
            IReadOnlyDictionary<string, BuddyManifestTransform2DInstance> transform2Ds,
            IReadOnlyDictionary<string, BuddyManifestTransform3DInstance> transform3Ds)
        {
            _transform2d = transform2Ds.ToDictionary(
                pair => pair.Key,
                pair => new ManifestTransform2D(pair.Value)
            );

            _transform3d = transform3Ds.ToDictionary(
                pair => pair.Key,
                pair => new ManifestTransform3D(pair.Value)
            );
        }

        public IReadOnlyTransform2D GetTransform2D(string key) => _transform2d.GetValueOrDefault(key);
        public IReadOnlyTransform3D GetTransform3D(string key) => _transform3d.GetValueOrDefault(key);
        
        public override string ToString() => nameof(IManifestTransforms);
    }
}
