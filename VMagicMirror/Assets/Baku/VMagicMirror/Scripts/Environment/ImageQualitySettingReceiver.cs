using System;
using UnityEngine;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    public class ImageQualitySettingReceiver
    {
        
        
        public ImageQualitySettingReceiver(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(VmmCommands.SetImageQuality,
                c => SetImageQuality(c.Content)
            );

            receiver.AssignQueryHandler(
                VmmQueries.GetQualitySettingsInfo,
                q =>
                {
                    q.Result = JsonUtility.ToJson(new ImageQualityInfo()
                    {
                        ImageQualityNames = QualitySettings.names,
                        CurrentQualityIndex = QualitySettings.GetQualityLevel(),
                    });
                });
            SetFrameRateWithQuality(QualitySettings.GetQualityLevel());
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
                    SetFrameRateWithQuality(i);
                    return;
                }
            }
        }

        private void SetFrameRateWithQuality(int qualityLevel)
        {
            //Very LowまたはLowの場合は明示的にCPU負荷を抑えたいはずなので、FPSも30まで下げる。
            //Medium以上の場合、デフォルト挙動に戻す
            Application.targetFrameRate = (qualityLevel < 2) ? 30 : -1;
        }
    }
    
    [Serializable]
    public class ImageQualityInfo
    {
        public string[] ImageQualityNames;
        public int CurrentQualityIndex;
    }
}
