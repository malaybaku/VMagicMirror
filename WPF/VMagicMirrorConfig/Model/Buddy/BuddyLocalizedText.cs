
using System;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// サブキャラ用のローカライズされた文字列を保持する構造体。
    /// manifest.json の定義から生成される
    /// </summary>
    public readonly struct BuddyLocalizedText
    {
        public BuddyLocalizedText(string ja, string en)
        {
            _ja = ja;
            _en = en;
        }

        private readonly string _ja;
        private readonly string _en;
        
        public string Get(bool isJapanese)
        {
            if (isJapanese)
            {
                return 
                    !string.IsNullOrEmpty(_ja) ? _ja :
                    !string.IsNullOrEmpty(_en) ? _en : 
                    "";
            }
            else
            {
                return
                    !string.IsNullOrEmpty(_en) ? _en :
                    !string.IsNullOrEmpty(_ja) ? _ja :
                    "";
            }
        }

        // NOTE: 「一方だけでも空でなければGet()自体の結果が空じゃなくなる…ということに基づく判定
        public bool IsEmpty => string.IsNullOrEmpty(_ja) && string.IsNullOrEmpty(_en);

        /// <summary>
        /// 言語によらず同じテキストになるようなローカライズテキストを生成する
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static BuddyLocalizedText Const(string value) => new(value, value);
        public static BuddyLocalizedText Empty() => new("", "");
    }
}