using System;
using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class VRoidHubLanguage : MonoBehaviour
    {
        [Serializable]
        struct CaptionItem
        {
            public Text text;
            public string japanese;
            public string english;
        }

        [SerializeField]
        CaptionItem[] translations;

        public string LanguageName { get; private set; } = "";

        private void Start()
        {
            SetLanguage("Japanese");
        }

        public void SetLanguage(string languageName)
        {
            //何もしない: VRoidHubControllerのサンプルは多言語切り替えと相性が悪い構造になっているため。

            //if (LanguageName == languageName)
            //{
            //    return;
            //}

            //LanguageName = languageName;
            //switch (languageName)
            //{
            //    case "Japanese":
            //        for(int i = 0; i < translations.Length; i++)
            //        {
            //            translations[i].text.text = translations[i].japanese;
            //        }
            //        break;
            //    case "English":
            //    default:
            //        for (int i = 0; i < translations.Length; i++)
            //        {
            //            translations[i].text.text = translations[i].english;
            //        }
            //        break;

            //}
        }
    }

}