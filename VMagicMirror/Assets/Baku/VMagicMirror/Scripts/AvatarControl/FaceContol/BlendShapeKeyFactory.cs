using System;
using System.Collections.Generic;
using System.Linq;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 名前の情報だけからブレンドシェイプキーを生成する処理を定義します。
    /// VMagicMirrorで名前情報のみからブレンドシェイプを拾いたい場合、ここを経由してキーを取得して下さい。
    /// </summary>
    public static class BlendShapeKeyFactory
    {
        private static readonly Dictionary<string, BlendShapePreset> _presets;

        static BlendShapeKeyFactory()
        {
            _presets = Enum
                .GetNames(typeof(BlendShapePreset))
                .ToDictionary(
                    name => name, 
                    name => (BlendShapePreset) Enum.Parse(typeof(BlendShapePreset), name)
                );
        }

        /// <summary>
        /// ブレンドシェイプの名称を指定して、
        /// それがプリセットの名称ならば明示的にプリセット扱いのブレンドシェイプを返却します。
        /// そうでない場合はオプションのブレンドシェイプ扱いでブレンドシェイプを返却します。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>
        /// VMagicMirrorではプリセットと重複する名称のブレンドシェイプ名があるモデルは変な動きするかも、という事になる。
        /// </remarks>
        public static BlendShapeKey CreateFrom(string name) 
            => _presets.ContainsKey(name)
                ? BlendShapeKey.CreateFromPreset(_presets[name]) 
                : BlendShapeKey.CreateUnknown(name);
    }
}
