using System.Collections.Generic;
using UniVRM10;

namespace Baku.VMagicMirror
{
    // NOTE: VMCProtocol起点のブレンドシェイプについては以下のように捌く
    // 理想
    // - Blink / LipSync / LookAt系 -> 無視
    // - それ以外の通常BlendShape -> 見る
    // - パーフェクトシンクのカスタムキー -> 無視
    // - パーフェクトシンク以外のカスタムのキー -> 見る
    // 現実 (※負荷 + 実装対効果が読めないので凝らない方向で妥協してる)
    // - 喜怒哀楽 + 驚き -> 見る
    // - それ以外 -> ぜんぶ無視

    // NOTE:
    // 「VMCP由来のLipSyncやBlinkは無視するが、WordToMotionやFaceSwitchで指定された場合は無視しない」という仕様になっている。
    // これがフクザツすぎるとなった場合、WtMとかFaceSwitchから指定したBlink/LipSyncのブレンドシェイプも一律で無視しちゃうのもアリ
    
    /// <summary>
    /// Word to Motionなどの明示的な操作によって適用された現在の表情キーを1つだけ保持するクラス。
    /// 保持した値はサブキャラ用のスクリプトAPIで公開する
    /// </summary>
    public class UserOperationBlendShapeResultRepository
    {
        public bool HasActiveKey { get; private set; }
        public ExpressionKey ActiveKey { get; private set; } = ExpressionKey.Neutral; 
        
        private readonly HashSet<ExpressionKey> _vmcpKeys = new()
        {
            ExpressionKey.Happy,
            ExpressionKey.Angry,
            ExpressionKey.Sad,
            ExpressionKey.Relaxed,
            ExpressionKey.Surprised,
        };

        // 以下のpublic関数のどれか一つをBlendShapeResultSetterが毎フレーム呼んでくれる…というのが期待値。
        // BlendShapeResultSetter側では補間のことは無視してよい

        /// <summary>
        /// WtMもFaceSwitchもVMCPも効いてないときに呼ぶことで、適用中の表情は特段ない…という扱いにする
        /// </summary>
        public void SetAsInactive() => HasActiveKey = false;

        /// <summary>
        /// WtMの適用内容を指定することで「WtMでメインで効かせてる表情 == アクティブな表情」という扱いにする
        /// </summary>
        /// <param name="content"></param>
        public void SetWordToMotionResult(WordToMotionBlendShapeApplyContent content)
        {
            var activeKey = ExpressionKey.Neutral;
            var maxBlendShapeValue = -1f;
            foreach (var (key, value) in content.Keys)
            {
                if (value > maxBlendShapeValue)
                {
                    maxBlendShapeValue = value;
                    activeKey = key;
                }
            }

            if (maxBlendShapeValue >= 0f)
            {
                ActiveKey = activeKey;
                HasActiveKey = true;
            }
            else
            {
                // 普通来ないはず: Word to Motionの表情は適用してるのに有効なキーがなぜかない…という状態
                HasActiveKey = false;
            }
        }

        /// <summary>
        /// FaceSwitchの適用状況を指定することで、そのExpressionKeyがアクティブな表情という扱いにする
        /// </summary>
        /// <param name="content"></param>
        public void SetFaceSwitchResult(FaceSwitchKeyApplyContent content)
        {
            HasActiveKey = true;
            ActiveKey = content.Key;
        }

        /// <summary>
        /// VMCP由来の表情を指定することで、もしVMCPから特定の表情が来ていればそれをアクティブな表情とみなす
        /// </summary>
        /// <param name="values"></param>
        public void SetVmcpResult(IReadOnlyDictionary<ExpressionKey, float> values)
        {
            var activeKey = ExpressionKey.Neutral;
            var maxBlendShapeValue = -1f;
            foreach (var pair in values)
            {
                if (!_vmcpKeys.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value > maxBlendShapeValue)
                {
                    activeKey = pair.Key;
                    maxBlendShapeValue = pair.Value;
                }
            }

            if (maxBlendShapeValue > 0)
            {
                HasActiveKey = true;
                ActiveKey = activeKey;
            }
            else
            {
                HasActiveKey = false;
            }
        }
    }
}
