using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    //TODO: MonoBehaviorいらなさそう OR 呼び出し元が直接Accumulatorにアクセスすることにしてクラスを削除
    /// <summary> ブレンドシェイプの値が0になる前処理 </summary>
    public class BlendShapeInitializer : MonoBehaviour
    {
        [Inject]
        public void Initialize(ExpressionAccumulator accumulator)
        {
            _accumulator = accumulator;
        }

        private ExpressionAccumulator _accumulator;
        
        /// <summary>
        /// すべてのクリップにゼロを当て込みます。
        /// </summary>
        public void InitializeBlendShapes()
        {
            _accumulator.ResetValues();
        }

        /// <summary>
        /// 指定したキーのクリップ値をゼロにします。
        /// </summary>
        /// <param name="keys"></param>
        public void InitializeBlendShapes(ExpressionKey[] keys)
        {
            _accumulator.SetZero(keys);
        }
    }
}
