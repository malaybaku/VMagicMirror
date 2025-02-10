using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class TransformsApi
    {
        private readonly Dictionary<string, Transform2DApi> _transform2d;

        public TransformsApi(Dictionary<string, LuaScriptTransform2DInstance> transform2Ds)
        {
            _transform2d = transform2Ds.ToDictionary(
                pair => pair.Key,
                pair => new Transform2DApi(pair.Value)
            );
        }

        [Preserve]
        public Transform2DApi GetTransform2D(string key) => _transform2d.GetValueOrDefault(key);
    }
    
    public class Transform2DApi
    {
        public Transform2DApi(LuaScriptTransform2DInstance instance)
        {
            _instance = instance;
        }

        private readonly LuaScriptTransform2DInstance _instance;
        
        /// <summary> NOTE: Sprite2DApiなど、他のAPIで引数として本APIを受け取ったときに必要に応じて使う </summary>
        /// <returns></returns>
        internal LuaScriptTransform2DInstance GetInstance() => _instance;
        
        [Preserve] public Vector2 Position => _instance.Position;
        [Preserve] public float Scale => _instance.Scale;
        [Preserve] public Quaternion Rotation => _instance.Rotation;
    }
}
