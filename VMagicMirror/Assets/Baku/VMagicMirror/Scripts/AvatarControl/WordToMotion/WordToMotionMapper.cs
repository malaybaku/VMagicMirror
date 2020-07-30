using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> 単語一覧とモーションのマッピングを管理するクラス </summary>
    public class WordToMotionMapper 
    {
        private readonly BuiltInMotionClipItem[] _builtInClips;

        public WordToMotionMapper(BuiltInMotionClipData builtInClips)
        {
            _builtInClips = builtInClips.items.ToArray();
        }

        //note: アロケーション都合で配列として公開するが、他所からは書きかえない想定
        public MotionRequest[] Requests { get; set; } = new MotionRequest[0];

        /// <summary>
        /// 起動のきっかけになる単語を指定してモーション要素を取得します。
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public MotionRequest FindMotionRequest(string word)
            => Requests?.FirstOrDefault(r => r.Word == word);

        /// <summary>
        /// インデックスを指定してモーション要素を取得します。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MotionRequest FindMotionByIndex(int index)
            => (Requests != null && index >= 0 && index < Requests.Length) ? Requests[index] : null;

        /// <summary>
        /// ビルトインアニメーションを、名称を指定して取得します。見つからない場合はnullを返します。
        /// </summary>
        /// <param name="clipName"></param>
        /// <returns></returns>
        public AnimationClip FindBuiltInAnimationClipOrDefault(string clipName)
        {
            for (int i = 0; i < _builtInClips.Length; i++)
            {
                if (_builtInClips[i].name == clipName)
                {
                    return _builtInClips[i].clip;
                }
            }
            return null;
        }

        //NOTE: Bvhは一旦ここでハンドルしないようにしてみる(ここに実装足してもOK)
    }
}
