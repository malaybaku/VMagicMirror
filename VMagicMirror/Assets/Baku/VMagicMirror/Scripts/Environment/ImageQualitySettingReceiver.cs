using System;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ImageQualitySettingReceiver : PresenterBase
    {
        private const string DefaultQualityName = "High";

        private readonly IMessageReceiver _receiver;
        private readonly ReactiveProperty<int> _targetFramerate = new(60);

        [Inject]
        public ImageQualitySettingReceiver(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(VmmCommands.SetImageQuality,
                c => SetImageQuality(c.GetStringValue())
            );
            _receiver.BindIntProperty(VmmCommands.SetTargetFramerate, _targetFramerate);

            _receiver.AssignQueryHandler(
                VmmCommands.GetQualitySettingsInfo,
                q =>
                {
                    q.Result = JsonUtility.ToJson(new ImageQualityInfo()
                    {
                        ImageQualityNames = QualitySettings.names,
                        CurrentQualityIndex = QualitySettings.GetQualityLevel(),
                    });
                });
            
            _receiver.AssignQueryHandler(
                VmmCommands.ApplyDefaultImageQuality,
                q => { 
                    SetImageQuality(DefaultQualityName);
                    q.Result = DefaultQualityName;
                });

            _targetFramerate
                .Subscribe(SetTargetFramerate)
                .AddTo(this);
        }
        
        private static void SetImageQuality(string name)
        {
            var names = QualitySettings.names;
            //foreachにしてないのはIndexOfより手軽にパフォーマンス取れそうだから
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                {
                    QualitySettings.SetQualityLevel(i, true);
                    return;
                }
            }
        }

        private static void SetTargetFramerate(int value)
        {
            Debug.Log($"{nameof(SetTargetFramerate)}: {value}");
            // - FPSが0以下の場合、vSyncを有効化してモニターのリフレッシュレートに合わせる要求だと解釈する
            // - 30未満のFPSを指定された場合も異常値扱いし、vSyncが有効な状態に帰着させる
            if (value < 30)
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = 30;
            }
            else
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = value;
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
