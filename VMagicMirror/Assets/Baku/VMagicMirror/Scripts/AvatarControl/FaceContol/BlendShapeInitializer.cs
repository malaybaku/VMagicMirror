using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary> 毎フレームの冒頭で全ブレンドシェイプの値が0になるように前処理するスクリプト。 </summary>
    /// <remarks>
    /// VMagicMirrorではブレンドシェイプ値は毎フレームごとに細かくAccumulate and Applyするようになっているので、
    /// フレーム開始の時点では表情データが残ってないのが望ましい。
    /// ※ちょっと負荷に効くかもしれないのでそこは要チェック
    /// </remarks>
    public class BlendShapeInitializer : MonoBehaviour
    {
        private static readonly Dictionary<string, BlendShapeKey> _presetKeys = new Dictionary<string, BlendShapeKey>()
        {
            ["Blink"] = new BlendShapeKey(BlendShapePreset.Blink), 
            ["Blink_L"] = new BlendShapeKey(BlendShapePreset.Blink_L), 
            ["Blink_R"] = new BlendShapeKey(BlendShapePreset.Blink_R), 

            ["LookLeft"] = new BlendShapeKey(BlendShapePreset.LookLeft), 
            ["LookRight"] = new BlendShapeKey(BlendShapePreset.LookRight), 
            ["LookUp"] = new BlendShapeKey(BlendShapePreset.LookUp), 
            ["LookDown"] = new BlendShapeKey(BlendShapePreset.LookDown), 

            ["A"] = new BlendShapeKey(BlendShapePreset.A), 
            ["I"] = new BlendShapeKey(BlendShapePreset.I), 
            ["U"] = new BlendShapeKey(BlendShapePreset.U), 
            ["E"] = new BlendShapeKey(BlendShapePreset.E), 
            ["O"] = new BlendShapeKey(BlendShapePreset.O), 

            ["Joy"] = new BlendShapeKey(BlendShapePreset.Joy), 
            ["Angry"] = new BlendShapeKey(BlendShapePreset.Angry), 
            ["Sorrow"] = new BlendShapeKey(BlendShapePreset.Sorrow), 
            ["Fun"] = new BlendShapeKey(BlendShapePreset.Fun),
        };

        private static readonly BlendShapeKey[] _lipSyncKeys = new[]
        {
            new BlendShapeKey(BlendShapePreset.A),
            new BlendShapeKey(BlendShapePreset.I),
            new BlendShapeKey(BlendShapePreset.U),
            new BlendShapeKey(BlendShapePreset.E),
            new BlendShapeKey(BlendShapePreset.O),
        };
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _proxy = info.blendShape;
                _hasModel = true;
                //NOTE: ReloadClipsのなかで_hasModelによるガードがかかってるのを踏まえてこういう順番です
                ReloadClips();
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _proxy = null;
            };
        }

        private bool _hasModel;
        private VRMBlendShapeProxy _proxy;
        private BlendShapeKey[] _keys = new BlendShapeKey[0];

        public void ReloadClips()
        {
            if (!_hasModel)
            {
                return;
            }
            
            _keys = CreateKeys(
                _proxy.BlendShapeAvatar
                    .Clips
                    .Select(c => c.BlendShapeName)
            );
        }

        public void InitializeBlendShapes(bool keepLipSync)
        {
            if (!_hasModel)
            {
                return;
            }

            for (int i = 0; i < _keys.Length; i++)
            {
                if (keepLipSync && _lipSyncKeys.Contains(_keys[i]))
                {
                    continue;
                }
                _proxy.AccumulateValue(_keys[i], 0);
            }
        }
        
        private void Update()
        {
            if (!_hasModel)
            {
                return;
            }
            
            for (int i = 0; i < _keys.Length; i++)
            {
                //NOTE: ゼロのやつを上書きすると負荷がムダに増えそうなので避けてます
                if (_proxy.GetValue(_keys[i]) > 0)
                {
                    _proxy.AccumulateValue(_keys[i], 0);
                }
            }
        }


        private static BlendShapeKey[] CreateKeys(IEnumerable<string> names)
        {
            return names
                .Select(n =>
                    _presetKeys.ContainsKey(n)
                        ? _presetKeys[n]
                        : new BlendShapeKey(n)
                )
                .ToArray();
        }
    }
}
