using System;
using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    public class ImageQualitySettingReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler _handler;

        private void Start()
        {
            _handler.Commands.Subscribe(c =>
            {
                switch (c.Command)
                {
                    case MessageCommandNames.SetImageQuality:
                        SetImageQuality(c.Content);
                        break;
                }
            });
            _handler.QueryRequested += OnQueryReceived;
        }

        private void OnQueryReceived(ReceivedQuery q)
        {
            if (q.Command == MessageQueryNames.GetQualitySettingsInfo)
            {
                q.Result = JsonUtility.ToJson(new ImageQualityInfo()
                {
                    ImageQualityNames = QualitySettings.names,
                    CurrentQualityIndex = QualitySettings.GetQualityLevel(),
                });
            }
        }

        private void SetImageQuality(string name)
        {
            var names = QualitySettings.names;
            //foreachにしてないのはIndexOfより手軽にパフォーマンス取れそうだから
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                {
                    QualitySettings.SetQualityLevel(i, true);
                    return;
                }
            }
        }
    }
    
    [Serializable]
    public class ImageQualityInfo
    {
        public string[] ImageQualityNames;
        public int CurrentQualityIndex;
    }
}
