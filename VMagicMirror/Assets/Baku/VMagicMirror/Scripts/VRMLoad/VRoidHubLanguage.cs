using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Baku.VMagicMirror
{
    public class VRoidHubLanguage : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        private CaptionItem[] translations = null;

        public string LanguageName { get; private set; } = "";

        private void Start()
        {
            SetLanguage("Japanese");
            handler.Commands.Subscribe(message =>
            {
                if (message.Command == MessageCommandNames.Language)
                {
                    SetLanguage(message.Content);
                }
            });
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


        [Serializable]
        struct CaptionItem
        {
            public Text text;
            public string japanese;
            public string english;
        }
    }
}