using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// リソースディクショナリに書かれた多言語文字列をコードから使えるようにするやつ
    /// </summary>
    public static class LocalizedString
    {
        /// <summary>
        /// キーを指定することで、現在の設定言語に応じた文字列を取得します。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetString(string key)
        {
            //NOTE: ディクショナリの切り替えはLanguageSelectorがよろしくやってるはずなので気にしないでよい
            var dict = Application.Current.Resources.MergedDictionaries[0];
            return (dict.Contains(key) && dict[key] is string result) ? result : "";
        }
    }
}
