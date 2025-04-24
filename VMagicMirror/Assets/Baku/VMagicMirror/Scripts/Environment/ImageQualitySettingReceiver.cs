using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ImageQualitySettingReceiver
    {
        private bool _halfFpsModeActive = false;
        
        public ImageQualitySettingReceiver(IMessageReceiver receiver, string defaultQualityName)
        {
            receiver.AssignCommandHandler(VmmCommands.SetImageQuality,
                c => SetImageQuality(c.GetStringValue())
            );
            receiver.AssignCommandHandler(
                VmmCommands.SetHalfFpsMode,
                message => SetHalfFpsMode(message.ToBoolean())
                );

            receiver.AssignQueryHandler(
                VmmCommands.GetQualitySettingsInfo,
                q =>
                {
                    q.Result = JsonUtility.ToJson(new ImageQualityInfo()
                    {
                        ImageQualityNames = QualitySettings.names,
                        CurrentQualityIndex = QualitySettings.GetQualityLevel(),
                    });
                });
            
            receiver.AssignQueryHandler(
                VmmCommands.ApplyDefaultImageQuality,
                q => { 
                    SetImageQuality(defaultQualityName);
                    q.Result = defaultQualityName;
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

        private void SetHalfFpsMode(bool isHalfMode)
        {
            _halfFpsModeActive = isHalfMode;
            SetFrameRateWithQuality(QualitySettings.GetQualityLevel());
        }

        private void SetFrameRateWithQuality(int qualityLevel)
        {
            //Very LowまたはLowの場合は明示的にCPU負荷を抑えたいはずなので、FPSも30まで下げる。
            //Medium以上の場合はVSyncに注意したうえでFPS60付近を狙いたいが、
            //以下のような事情を踏まえ、ユーザーが明示的にハーフFPSにしたかどうかで判断していく。
            // - VSyncを切っちゃうとティアリングが出る
            // - モニター自体のリフレッシュレート変更は要求すべきでない
            // - モニターのリフレッシュレート(というか平均FPS)を見て云々、とするのが比較的めんどう
            Application.targetFrameRate = (qualityLevel < 2) ? 30 : 60;
            QualitySettings.vSyncCount =
                (qualityLevel < 2) ? 0 :
                _halfFpsModeActive ? 2 : 1;
        }
    }
    
    [Serializable]
    public class ImageQualityInfo
    {
        public string[] ImageQualityNames;
        public int CurrentQualityIndex;
    }
}
