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
            ["Blink"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink), 
            ["Blink_L"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_L), 
            ["Blink_R"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_R), 

            ["LookLeft"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.LookLeft), 
            ["LookRight"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.LookRight), 
            ["LookUp"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.LookUp), 
            ["LookDown"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.LookDown), 

            ["A"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.A), 
            ["I"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.I), 
            ["U"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.U), 
            ["E"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.E), 
            ["O"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.O), 

            ["Joy"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.Joy), 
            ["Angry"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.Angry), 
            ["Sorrow"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.Sorrow), 
            ["Fun"] = BlendShapeKey.CreateFromPreset(BlendShapePreset.Fun),
        };

        private static readonly BlendShapeKey[] _lipSyncKeys = new[]
        {
            BlendShapeKey.CreateFromPreset(BlendShapePreset.A),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.I),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.U),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.E),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.O),
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

        /// <summary>
        /// すべてのクリップにゼロを当て込みます。
        /// </summary>
        public void InitializeBlendShapes()
        {
            if (!_hasModel)
            {
                return;
            }

            for (int i = 0; i < _keys.Length; i++)
            {
                _proxy.AccumulateValue(_keys[i], 0);
            }
        }

        /// <summary>
        /// 指定したキーのクリップ値をゼロにします。
        /// </summary>
        /// <param name="keys"></param>
        public void InitializeBlendShapes(BlendShapeKey[] keys)
        {
            if (!_hasModel)
            {
                return;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                _proxy.AccumulateValue(keys[i], 0);
            }
        }

        
        private static BlendShapeKey[] CreateKeys(IEnumerable<string> names)
        {
            return names
                .Select(n =>
                    _presetKeys.ContainsKey(n)
                        ? _presetKeys[n]
                        : BlendShapeKey.CreateUnknown(n)
                )
                .ToArray();
        }
    }
}
