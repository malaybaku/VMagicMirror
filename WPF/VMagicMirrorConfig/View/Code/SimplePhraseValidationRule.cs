using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Baku.VMagicMirrorConfig.View
{
    /// <summary>
    /// 大文字/小文字/数字/半角スペースのみで構成された文字列のみを有効入力扱いするバリデーションルール
    /// </summary>
    /// <remarks>
    /// OK: "There are 3 balls"
    /// OK: "Hello"
    /// OK: "hoge"
    /// NG: "can't" -> アポストロフィがダメ
    /// NG: "ok." -> ピリオドがダメ
    /// NG: "hey!" -> エクスクラメーションがダメ
    /// </remarks>
    public class SimplePhraseValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!(value is string s))
            {
                return new ValidationResult(false, "The input is not a text");
            }

            if (Regex.IsMatch(s, "[^0-9a-zA-Z\\s]"))
            {
                return new ValidationResult(false, "Please input only alphabets, numbers, or space (a-z, A-Z, 0-9, \" \")");
            }

            return ValidationResult.ValidResult;
        }
    }
}
