using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.Buddy.Api
{
    /// <summary>
    /// ユーザーが調整したプロパティを取得できるAPI。
    /// サブキャラの表示位置やスピード、ユーザー名の変更などに対応する
    /// </summary>
    public class PropertyApi
    {
        // NOTE:
        // - リロードする場合はインスタンスが丸ごと破棄される(べきである)ため、Clear()関数はない
        // - WPF側で初期化のシーケンスとかメッセージの送信順を担保する前提のため、IsInitialized() みたいなのも不要
        private readonly Dictionary<string, object> _values = new();

        // APIからはコレだけ使う。パラメータの型はサブキャラの作成者が知ってるはずなので教えない。
        // ※必要そうになったらGetType(string key)とかを公開してもよい
        [Preserve]
        public object Get(string key) => _values.GetValueOrDefault(key);

        internal void AddOrUpdate(BuddyProperty property) => _values[property.Name] = property.Value;
        
        internal void Clear() => _values.Clear();
    }
}
