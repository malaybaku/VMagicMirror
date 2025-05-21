using System;
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
        private readonly SingleBuddyPropertyRepository _impl;
        
        public PropertyApi(SingleBuddyPropertyRepository impl)
        {
            _impl = impl;
        }
        
        public override string ToString() => nameof(BuddyApi.IProperty);

        public event Action<string> ActionRequested;
        internal void InvokeActionInternal(string propertyName) => ActionRequested?.Invoke(propertyName);
        
        public bool GetBool(string key) => _impl.GetBool(key);
        public int GetInt(string key) => _impl.GetInt(key);
        public float GetFloat(string key) => _impl.GetFloat(key);
        public string GetString(string key) => _impl.GetString(key);
        public BuddyApi.Vector2 GetVector2(string key) => _impl.GetVector2(key).ToApiValue();
        public BuddyApi.Vector3 GetVector3(string key) => _impl.GetVector3(key).ToApiValue();
        public BuddyApi.Quaternion GetQuaternion(string key) => _impl.GetQuaternion(key).ToApiValue();
    }
}
