using System.Collections.Generic;
using UnityEngine;
using BuddyApi = VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    /// <summary>
    /// ユーザーが調整したプロパティを取得できるAPI。
    /// サブキャラの表示位置やスピード、ユーザー名の変更などに対応する
    /// </summary>
    public class PropertyApi : BuddyApi.IProperty
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

        public BuddyApi.Vector2 GetVector2(string key) 
            => Get(key) is Vector2 v ? v.ToApiValue() : BuddyApi.Vector2.zero;

        public BuddyApi.Vector3 GetVector3(string key)
            => Get(key) is Vector3 v ? v.ToApiValue() : BuddyApi.Vector3.zero;

        public BuddyApi.Quaternion GetQuaternion(string key)
            => Get(key) is Quaternion v ? v.ToApiValue() : BuddyApi.Quaternion.identity;
    }
}
