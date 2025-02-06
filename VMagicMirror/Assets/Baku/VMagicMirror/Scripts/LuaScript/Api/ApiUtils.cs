using System;
using UnityEngine;

namespace Baku.VMagicMirror.LuaScript.Api
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

        public static void Try(Action act)
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
                LogOutput.Instance.Write("Error in script execution:");
                LogOutput.Instance.Write(ex);
            }
        }
    }    
}
