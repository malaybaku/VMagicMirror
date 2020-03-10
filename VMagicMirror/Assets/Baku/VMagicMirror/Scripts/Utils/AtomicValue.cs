using System;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// スレッドセーフに値を保持するラッパー。
    /// ただしListをツッコんでListの中を漁ったりするとスレッドセーフじゃなくなるので、
    /// 基本的にはstructを指定して使うことになります
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Atomic<T>
    {
        private readonly object _lock = new object();

        private T _value = default;
        
        /// <summary>
        /// 値を取得、設定します。
        /// </summary>
        public T Value
        {
            get
            {
                lock (_lock) return _value;
            }
            set
            {
                lock (_lock) _value = value;
            }
        }

        /// <summary>
        /// 現在保持している値に対して排他的に何かをします。
        /// プロパティアクセスなど、値の取得ではない間接的な処理をスレッドセーフに行うために用います。
        /// </summary>
        /// <param name="act"></param>
        public void Do(Action<T> act)
        {
            lock (_lock)
            {
                act(_value);
            }
        }
    }
}
