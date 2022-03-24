using System;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    class FaceSettingReceiver
    {
        public FaceSettingReceiver() : this(
            ModelResolver.Instance.Resolve<IMessageReceiver>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<FaceMotionBlendShapeNameStore>()
            )
        {
        }

        public FaceSettingReceiver(
            IMessageReceiver receiver, MotionSettingModel motionSetting, FaceMotionBlendShapeNameStore store)
        {
            _motionSetting = motionSetting;
            _store = store;

            //ちょっと横着だが、ついでにやってしまう: ブレンドシェイプ名管理のために、設定で使われているクリップ名は覚える必要がある
            _motionSetting.FaceNeutralClip.PropertyChanged += (_, __) =>
                _store.Refresh(_motionSetting.FaceNeutralClip.Value, _motionSetting.FaceOffsetClip.Value);
            _motionSetting.FaceOffsetClip.PropertyChanged += (_, __) =>
                _store.Refresh(_motionSetting.FaceNeutralClip.Value, _motionSetting.FaceOffsetClip.Value);

            receiver.ReceivedCommand += OnReceiveCommand;
        }

        private readonly MotionSettingModel _motionSetting;
        private readonly FaceMotionBlendShapeNameStore _store;

        private void OnReceiveCommand(object? sender, CommandReceivedEventArgs e)
        {
            switch (e.Command)
            {
                case ReceiveMessageNames.SetCalibrationFaceData:
                    //NOTE: Unity側がすでにこの値を把握しているので、投げ返す必要がない
                    _motionSetting.CalibrateFaceData.SilentSet(e.Args);
                    break;
                case ReceiveMessageNames.AutoAdjustResults:
                    _motionSetting.SetAutoAdjustResults(e.Args);
                    break;
                case ReceiveMessageNames.ExtraBlendShapeClipNames:
                    try
                    {
                        //いちおう信頼はするけどIPCだし…みたいな書き方。FaceSwitchと同じ
                        var names = e.Args
                            .Split(',')
                            .Where(w => !string.IsNullOrEmpty(w))
                            .ToArray();
                        _store.Refresh(names);
                    }
                    catch (Exception ex)
                    {
                        LogOutput.Instance.Write(ex);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
