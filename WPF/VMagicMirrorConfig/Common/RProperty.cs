using System;
using System.ComponentModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// ReactivePropertyの機能をめちゃくちゃ削減したもの。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RProperty<T> : NotifiableBase
    {
        /// <summary>
        /// 初期値と、および値が変化したときのPropertyChanged呼び出し以外の処理を指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="onChanged"></param>
        public RProperty(T value, Action<T> onChanged)
        {
            _onChanged = onChanged;
            _value = value;
        }

        /// <summary>
        /// 初期値のみを指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="value"></param>
        public RProperty(T value) : this(value, _ => { })
        {
        }


        private readonly Action<T> _onChanged;
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                if (SetValue(ref _value, value))
                {
                    _onChanged(value);
                }
            }
        }

        /// <summary>
        /// NOTE: nullableなRPropertyに対してValueのSetterを呼びたいときに使う。
        /// コレを使わざるを得ないのは循環参照が発生してるケースの可能性が高いため、ちょっと注意
        /// </summary>
        /// <param name="value"></param>
        public void Set(T value) => Value = value;

        /// <summary>
        /// 値は変更するが、イベントやコールバックは呼ばない。
        /// Unity側から値を受信し、その値がUI上で表示不要であるような、ごく一部のケースでのみ使う
        /// </summary>
        /// <param name="value"></param>
        public void SilentSet(T value) { _value = value; }

        /// <summary>
        /// 便宜的にプロパティが変更された扱いにしたい場合に呼ぶ。
        /// ViewModelとの同期ずれが起きてそうなModelで明示的に呼ぶなど、特殊な状況でのみ用いる
        /// </summary>
        public void ForceRaisePropertyChanged() => RaisePropertyChanged();

        /// <summary>
        /// 弱いイベントパターンでプロパティ変更イベントを購読します。
        /// ViewModelからModelのイベントを購読する場合に使用します。
        /// </summary>
        /// <param name="handler"></param>
        public void AddWeakEventHandler(EventHandler<PropertyChangedEventArgs> handler)
            => WeakEventManager<RProperty<T>, PropertyChangedEventArgs>.AddHandler(this, nameof(PropertyChanged), handler);
    }
}
