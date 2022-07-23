using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    class PreferenceFileManager
    {
        public PreferenceFileManager() : this(ModelResolver.Instance.Resolve<HotKeySettingModel>())
        {
        }

        public PreferenceFileManager(HotKeySettingModel hotKeySetting)
        {
            _hotKeySetting = hotKeySetting;
        }

        private readonly HotKeySettingModel _hotKeySetting;

        public void Save()
        {
            var data = new PreferenceData()
            {
                HotKeySetting = _hotKeySetting.Save(),
            };
            SaveInternal(data);
        }

        public void Load()
        {
            var data = LoadInternal();
            _hotKeySetting.Load(data.HotKeySetting);
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(SpecialFilePath.PreferenceFilePath))
            {
                File.Delete(SpecialFilePath.PreferenceFilePath);
            }
        }

        private void SaveInternal(PreferenceData data)
        {
            var sb = new StringBuilder();
            using var sw = new StringWriter(sb);
            new JsonSerializer().Serialize(sw, data);
            File.WriteAllText(SpecialFilePath.PreferenceFilePath, sb.ToString());
        }

        
        private PreferenceData LoadInternal()
        {
            if (!File.Exists(SpecialFilePath.PreferenceFilePath))
            {
                return PreferenceData.LoadDefault();
            }

            try
            {
                var text = File.ReadAllText(SpecialFilePath.PreferenceFilePath);
                using var sr = new StringReader(text);
                using var jr = new JsonTextReader(sr);
                var jsonSerializer = new JsonSerializer();
                var result = jsonSerializer.Deserialize<PreferenceData>(jr) ?? PreferenceData.LoadDefault();
                return result.Validate();
            }
            catch(Exception ex)
            { 
                LogOutput.Instance.Write(ex);
                return PreferenceData.LoadDefault();
            }

        }
    }
}
