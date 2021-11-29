using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xaml;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// JP/EN以外の翻訳テキストがある場合にそれをResourceDictionaryとしてロードできるやつ
    /// </summary>
    class LocalizationDictionaryLoader
    {
        private readonly Dictionary<string, ResourceDictionary> _dictionaries
            = new Dictionary<string, ResourceDictionary>();

        public void LoadFromDefaultLocation()
        {
            _dictionaries.Clear();

            var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (exeDir == null)
            {
                return;
            }

            var searchDir = Path.Combine(exeDir, "Localization");
            if (!Directory.Exists(searchDir))
            {
                return;
            }

            foreach (var filePath in Directory.GetFiles(searchDir))
            {
                var key = Path.GetFileNameWithoutExtension(filePath);
                if (key == LanguageSelector.LangNameJapanese || key == LanguageSelector.LangNameEnglish)
                {
                    LogOutput.Instance.Write("Localization file for Japanese and English is ignored");
                    continue;
                }

                try
                {
                    var reader = new XamlXmlReader(filePath);
                    var dict = XamlServices.Load(reader) as ResourceDictionary;
                    if (dict != null)
                    {

                        _dictionaries[key] = dict;
                        LogOutput.Instance.Write("Localization file loaded: " + key);
                    }
                    else
                    {
                        LogOutput.Instance.Write("Load seems success, but Resource Dictionary was not loaded actually." + filePath);
                    }
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }

        public Dictionary<string, ResourceDictionary> GetLoadedDictionaries() => _dictionaries;

    }
}
