using System.Collections.Generic;
using System.Linq;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class TransformsApi : ITransformsApi
    {
        private readonly Dictionary<string, Transform2DApi> _transform2d;

        public TransformsApi(Dictionary<string, BuddyTransform2DInstance> transform2Ds)
        {
            _transform2d = transform2Ds.ToDictionary(
                pair => pair.Key,
                pair => new Transform2DApi(pair.Value)
            );
        }

        public ITransform2DApi GetTransform2D(string key) => _transform2d.GetValueOrDefault(key);
    }
    
    public class Transform2DApi : ITransform2DApi
    {
        public Transform2DApi(BuddyTransform2DInstance instance)
        {
            _instance = instance;
        }

        private readonly BuddyTransform2DInstance _instance;
        
        /// <summary> NOTE: Sprite2DApiなど、他のAPIで引数として本APIを受け取ったときに必要に応じて使う </summary>
        /// <returns></returns>
        internal BuddyTransform2DInstance GetInstance() => _instance;
        
        public Vector2 Position => _instance.Position.ToApiValue();
        public float Scale => _instance.Scale;
        public Quaternion Rotation => _instance.Rotation.ToApiValue();
    }
}
