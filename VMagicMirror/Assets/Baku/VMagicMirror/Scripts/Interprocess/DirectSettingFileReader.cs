using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 直接UnityでWPF用の設定ファイルを読み込むクラス。どうしても初期化で取得したい変数があるときだけ使う
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
            if (!SpecialFiles.AutoSaveSettingFileExists())
            {
                return;
            }

            try
            {
                using var sr = new StreamReader(SpecialFiles.AutoSaveSettingFilePath);
                var doc = XDocument.Load(sr);
                var node = doc.XPathSelectElement("/SaveData/WindowSetting/IsTransparent");
                TransparentBackground = node != null && bool.TryParse(node.Value, out var result) && result;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
    }
}
