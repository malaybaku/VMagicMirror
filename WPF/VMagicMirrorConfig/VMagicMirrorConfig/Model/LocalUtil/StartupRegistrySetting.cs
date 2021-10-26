using Microsoft.Win32;
using System;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// Windowsの起動時にVMagicMirrorが自動でスタートする処理に関し、
    /// レジストリ設定を見に行くクラス
    /// </summary>
    class StartupRegistrySetting
    {
        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ProductName = @"Baku.VMagicMirror";

        private string UnityAppPath => SpecialFilePath.UnityAppPath;

        /// <summary>
        /// 実行されているVMagicMirrorがまさに登録されているかどうかを取得します。
        /// </summary>
        /// <returns></returns>
        public bool CheckThisVersionRegistered()
        {
            try
            {
                using (var regKey = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false))
                {
                    return regKey != null &&
                        regKey.GetValue(ProductName, "") is string s &&
                        s == UnityAppPath;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 他バージョンのVMagicMirrorが自動でスタートするようになっているかを取得します。
        /// </summary>
        /// <returns></returns>
        public bool CheckOtherVersionRegistered()
        {
            try
            {
                using var regKey = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                return regKey != null &&
                    regKey.GetValue(ProductName, UnityAppPath) is string s &&
                    s != UnityAppPath;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// いま実行しているVMagicMirrorがWindows起動時に自動でスタートするようにするか、
        /// あるいはWindows起動時に(他バージョンも含めて)VMagicMirrorが起動されないようにします。
        /// </summary>
        /// <param name="enable"></param>
        public void SetThisVersionRegister(bool enable)
        {
            try
            {
                using var regKey = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (regKey != null)
                {
                    if (enable)
                    {
                        regKey.SetValue(ProductName, UnityAppPath);
                    }
                    else
                    {
                        regKey.DeleteValue(ProductName);
                    }
                }
            }
            catch (Exception)
            {
                //諦める
            }
        }
    }
}
