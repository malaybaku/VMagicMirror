using System.Collections.Generic;
using Baku.VMagicMirror.Buddy.Api.Interface;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace Baku.VMagicMirror.Buddy.Api
{
    /// <summary>
    /// ユーザーが調整したプロパティを取得できるAPI。
    /// サブキャラの表示位置やスピード、ユーザー名の変更などに対応する
    /// </summary>
    public class PropertyApi : IPropertyApi
    {
        // NOTE:
        // - リロードする場合はインスタンスが丸ごと破棄される(べきである)ため、Clear()関数はない
        // - WPF側で初期化のシーケンスとかメッセージの送信順を担保する前提のため、IsInitialized() みたいなのも不要
        private readonly Dictionary<string, object> _values = new();

        internal void AddOrUpdate(BuddyProperty property) => _values[property.Name] = property.Value;

        internal void Clear() => _values.Clear();

        private object Get(string key) => _values.GetValueOrDefault(key);

        // NOTE: boolじゃない場合、一様にfalse扱いされる
        public bool GetBool(string key) => Get(key) is true;
        public int GetInt(string key) => Get(key) is int v ? v : 0;
        public float GetFloat(string key) => Get(key) is float v ? v : 0f;
        public string GetString(string key) => Get(key) is string v ? v : "";

        public Interface.Vector2 GetVector2(string key) 
            => Get(key) is Vector2 v ? v.ToApiValue() : Interface.Vector2.zero;

        public Interface.Vector3 GetVector3(string key)
            => Get(key) is Vector3 v ? v.ToApiValue() : Interface.Vector3.zero;

        public Interface.Quaternion GetQuaternion(string key)
            => Get(key) is Quaternion v ? v.ToApiValue() : Interface.Quaternion.identity;
    }
}
