using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 直接UnityでWPF用の設定ファイルを読み込むクラス。
    /// どうしても初期化で取得したい変数があるときだけ使う
    /// </summary>
    public class DirectSettingFileReader
    {
        public void Load()
        {
            try
            {
                LoadPropertiesFromSettingFile();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                TransparentBackground = false;
            }
        }
        
        public bool TransparentBackground { get; private set; }
        
        private void LoadPropertiesFromSettingFile()
        {
            if (!SettingFileExists())
            {
                return;
            }

            using (var sr = new StreamReader(GetSettingFilePath()))
            {
                var doc = XDocument.Load(sr);
                var node = doc.XPathSelectElement("/SaveData/WindowSetting/IsTransparent");
                TransparentBackground = 
                    bool.TryParse(node.Value, out bool result) && result;
            }
        }
        
        //NOTE: ファイルパスはWPFのSpecialFilePathクラスでも使っているので合わせる必要あり
        private const string AutoSaveSettingFileName = "_autosave";

        //実行中のVMagicMirrorの設定が保存された設定ファイルのパスを取得します。
        public static string GetSettingFilePath()
            => Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "ConfigApp",
                AutoSaveSettingFileName
            );

        /// <summary>
        /// <see cref="GetSettingFilePath"/>のパスに設定ファイルがあるかどうかを確認します。
        /// </summary>
        /// <returns></returns>
        public static bool SettingFileExists() => File.Exists(GetSettingFilePath());
    }
}
