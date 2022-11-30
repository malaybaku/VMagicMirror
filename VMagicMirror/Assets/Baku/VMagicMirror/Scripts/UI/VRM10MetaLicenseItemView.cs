using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class VRM10MetaLicenseItemView : MonoBehaviour
    {
        [SerializeField] private GameObject noneTextObject = null;
        [SerializeField] private Button openUrlButton = null;
        [SerializeField] private TextMeshProUGUI urlText = null;

        //NOTE: 勝手にApplication.OpenUrlしてもそんなに害はないが、いちおうMetaViewControllerに読ます
        public IObservable<string> OpenUrlRequested => openUrlButton
            .OnClickAsObservable()
            .Select(_ => urlText.text);

        public void SetUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                urlText.gameObject.SetActive(false);
                openUrlButton.gameObject.SetActive(false);
                noneTextObject.SetActive(true);
            }
            else
            {
                urlText.text = url;
                urlText.gameObject.SetActive(true);
                openUrlButton.gameObject.SetActive(true);
                noneTextObject.SetActive(false);
            }
        }
    }
}
