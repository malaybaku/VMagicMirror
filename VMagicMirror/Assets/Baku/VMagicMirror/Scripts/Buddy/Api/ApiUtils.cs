using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using BuddyApi = VMagicMirror.Buddy;

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
        /// <see cref="BuddyApi.ISprite2D"/> とか <see cref="BuddyApi.IVrm"/> とかでAPIが使うパスを絶対パスに変換するすごいやつだよ
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetAssetFullPath(BuddyFolder folder, string path)
        {
            // NOTE: 同一ファイルを複数の文字列でささないように少し配慮している(徹底はしない)。ガード文のあとのほうも同様 
            if (Path.IsPathFullyQualified(path))
            {
                return Path.GetFullPath(path).ToLower(CultureInfo.InvariantCulture);
            }

            var rootDirectory = folder.IsDefaultBuddy
                ? SpecialFiles.DefaultBuddyRootDirectory
                : SpecialFiles.BuddyRootDirectory;
            return Path.GetFullPath(
                Path.Combine(rootDirectory, folder.FolderName, path)
                )
                .ToLower(CultureInfo.InvariantCulture);
        }
        
        public static void Try(BuddyFolder folder, BuddyLogger logger, Action act)
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
                
                logger.LogRuntimeException(folder, ex);
            }
        }

        public static T Try<T>(BuddyFolder folder, BuddyLogger logger, Func<T> func, T valueWhenFailed = default)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                if (Application.isEditor)
                {
                    Debug.LogException(ex);
                }
                
                logger.LogRuntimeException(folder, ex);

                return valueWhenFailed;
            }
        }

        public static async Task TryAsync(BuddyFolder folder, BuddyLogger logger, Func<Task> task)
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                if (Application.isEditor)
                {
                    Debug.LogException(ex);
                }
                logger.LogRuntimeException(folder, ex);
            }
        }
        
        public static TextureLoadResult TryGetTexture2D(string fullPath, out Texture2D texture)
        {    
            if (!File.Exists(fullPath))
            {
                texture = null;
                return TextureLoadResult.FailureFileNotFound;
            }

            var bytes = File.ReadAllBytes(fullPath);
            var result = new Texture2D(32, 32);
            result.LoadImage(bytes);
            result.wrapMode = TextureWrapMode.Clamp;
            result.filterMode = FilterMode.Bilinear;
            result.Apply(false, true);
            texture = result;
            return TextureLoadResult.Success;
        }
    }

    public enum TextureLoadResult
    {
        Success,
        FailureFileNotFound,
    }

    public static class ValueDataExtensions
    {
        #region API to UnityEngine
        
        public static Vector2 ToEngineValue(this BuddyApi.Vector2 v) => new(v.x, v.y);
        public static Vector3 ToEngineValue(this BuddyApi.Vector3 v) => new(v.x, v.y, v.z);
        public static Quaternion ToEngineValue(this BuddyApi.Quaternion v) => new(v.x, v.y, v.z, v.w);

        // NOTE: enumは数値の並びが揃っているものはそのままキャストするだけでOK。これは逆方向でも同様
        public static GamepadKey ToEngineValue(this BuddyApi.GamepadButton key) => (GamepadKey)key;
        public static HumanBodyBones ToEngineValue(this BuddyApi.HumanBodyBones bone) => (HumanBodyBones)bone;

        public static Sprite2DTransitionStyle ToEngineValue(this BuddyApi.Sprite2DTransitionStyle style)
            => (Sprite2DTransitionStyle)style;
        
        #endregion

        #region UnityEngine to API
        
        public static BuddyApi.Vector2 ToApiValue(this Vector2 v) => new(v.x, v.y);
        public static BuddyApi.Vector3 ToApiValue(this Vector3 v) => new(v.x, v.y, v.z);
        public static BuddyApi.Quaternion ToApiValue(this Quaternion v) => new(v.x, v.y, v.z, v.w);

        public static BuddyApi.GamepadButton ToApiValue(this GamepadKey key) => (BuddyApi.GamepadButton)key;
        public static BuddyApi.HumanBodyBones ToApiValue(this HumanBodyBones bone) => (BuddyApi.HumanBodyBones)bone;

        public static BuddyApi.Sprite2DTransitionStyle ToApiValue(this Sprite2DTransitionStyle style)
            => (BuddyApi.Sprite2DTransitionStyle)style;

        public static BuddyApi.Pose ToApiValue(this Pose p) 
            => new(p.position.ToApiValue(), p.rotation.ToApiValue());
        
        #endregion
    }
}
