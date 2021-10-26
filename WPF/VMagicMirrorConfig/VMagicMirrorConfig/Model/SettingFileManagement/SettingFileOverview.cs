using System;
using System.IO;
using System.Xml.Serialization;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// 設定ファイルの概観を示したデータです。
    /// </summary>
    class SettingFileOverview
    {
        public SettingFileOverview(int index, bool exist, string modelName, DateTime lastUpdateTime)
        {
            Index = index;
            Exist = exist;
            ModelName = modelName;
            LastUpdateTime = lastUpdateTime;
        }

        /// <summary>
        /// セーブデータの番号を取得します。0はオートセーブを表します。
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// すでに保存されたファイルがあるかどうかを取得します。
        /// </summary>
        public bool Exist { get; }

        /// <summary>
        /// ロードされていたモデルの名前を取得します。
        /// ファイルが存在しない場合やモデルをロードしていなかったデータの場合、空文字列が入ります。
        /// </summary>
        public string ModelName { get; }

        /// <summary>
        /// ファイルの最終更新日、つまりファイルが保存された時刻を取得します。
        /// 実装上はWindowsのファイルメタデータをそのまんま見に行った値が入ります。
        /// ファイルが無い場合は現在時刻とかのテキトーな値が入ります。
        /// </summary>
        public DateTime LastUpdateTime { get; }

        public static SettingFileOverview CreateInvalid(int index)
            => new SettingFileOverview(index, false, "", DateTime.Now);

        /// <summary>
        /// ファイルパスと想定インデックスのペアからデータを生成します。
        /// 中でXMLパースが走ってたりするので注意して下さい。
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static SettingFileOverview CreateOverviewFromFile(string filePath, int index)
        {
            if (!File.Exists(filePath))
            {
                return CreateInvalid(index);
            }

            try
            {
                var lastUpdated = new FileInfo(filePath).LastWriteTime;
                using (var sr = new StreamReader(filePath))
                {
                    var serializer = new XmlSerializer(typeof(SaveData));
                    var saveData = (SaveData?)serializer.Deserialize(sr);
                    if (saveData == null)
                    {
                        LogOutput.Instance.Write("Failed to get model name from serialized save data.");
                        return new SettingFileOverview(index, true, "", lastUpdated);
                    }

                    string modelName = saveData.LastLoadedVrmName ?? "";
                    return new SettingFileOverview(index, true, modelName, lastUpdated);
                }
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return CreateInvalid(index);
            }
        }
    }
}
