using System;
using Baku.VMagicMirror.Buddy.Api.Interface;
using UnityEngine;
using HumanBodyBones = UnityEngine.HumanBodyBones;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Baku.VMagicMirror.Buddy.Api
{
    public static class ApiUtils
    {
        /// <summary>
        /// フォルダとファイルの絶対パスを指定して、ファイルがdir以下にあるかどうかを判定する
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool IsChildDirectory(string dir, string file)
        {
            return file.StartsWith(dir, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 指定したファイルがBuddyフォルダ内のファイルかどうかを判定する
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool IsInBuddyDirectory(string file)
            => IsChildDirectory(SpecialFiles.BuddyRootDirectory, file);

        public static void Try(string buddyId, Action act)
        {
            try
            {
                act();
            }
            catch (Exception ex)
            {
                if (Application.isEditor)
                {
                    Debug.LogException(ex);
                }
                
                BuddyLogger.Instance.Log(buddyId, ex);
            }
        }
    }

    public static class ValueDataExtensions
    {
        #region API to UnityEngine
        
        public static Vector2 ToEngineValue(this Interface.Vector2 v) => new(v.x, v.y);
        public static Vector3 ToEngineValue(this Interface.Vector3 v) => new(v.x, v.y, v.z);
        public static Quaternion ToEngineValue(this Interface.Quaternion v) => new(v.x, v.y, v.z, v.w);

        // NOTE: enumは数値まで揃えてるやつはそのままキャストすればOK。これは逆方向でも同様
        public static GamepadKey ToEngineValue(this Interface.GamepadKey key) => (GamepadKey)key;
        public static HumanBodyBones ToEngineValue(this Interface.HumanBodyBones bone) => (HumanBodyBones)bone;
        
        #endregion

        #region UnityEngine to API
        
        public static Interface.Vector2 ToApiValue(this Vector2 v) => new(v.x, v.y);
        public static Interface.Vector3 ToApiValue(this Vector3 v) => new(v.x, v.y, v.z);
        public static Interface.Quaternion ToApiValue(this Quaternion v) => new(v.x, v.y, v.z, v.w);

        public static Interface.GamepadKey ToApiValue(this GamepadKey key) => (Interface.GamepadKey)key;
        public static Interface.HumanBodyBones ToApiValue(this HumanBodyBones bone) => (Interface.HumanBodyBones)bone;

        public static Interface.Pose ToApiValue(this UnityEngine.Pose p) 
            => new(p.position.ToApiValue(), p.rotation.ToApiValue());
        
        #endregion
    }
}
