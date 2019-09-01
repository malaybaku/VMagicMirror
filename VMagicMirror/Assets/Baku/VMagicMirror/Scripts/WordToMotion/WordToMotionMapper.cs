using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 単語一覧とモーションのマッピングを管理するクラス
    /// </summary>
    /// <remarks>
    /// 機能上はMonoBehaviorの必然性があまりないが<see cref="Application"/>に依存してる事には注意
    /// </remarks>
    public class WordToMotionMapper : MonoBehaviour
    {
        [Serializable]
        private struct BuiltInAnimationClip
        {
            public string name;
            public AnimationClip clip;
        }

        [SerializeField]
        private BuiltInAnimationClip[] _builtInClips = null;

        //note: アロケーション都合で配列として公開するが、他所からは書きかえない想定
        public MotionRequest[] Requests { get; set; } = new MotionRequest[0];

        public MotionRequest FindMotionRequest(string word)
            => Requests?.FirstOrDefault(r => r.Word == word);

        /// <summary>
        /// ビルトインアニメーションを、名称を指定して取得します。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public AnimationClip FindBuiltInAnimationClipOrDefault(string name)
        {
            for (int i = 0; i< _builtInClips.Length; i++)
            {
                if (_builtInClips[i].name == name)
                {
                    return _builtInClips[i].clip;
                }
            }
            return null;
        }

        //NOTE: Bvhは一旦ここでハンドルしないようにしてみる(ここに実装足してもOK)
    }
}
