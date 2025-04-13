using System;
using System.Windows;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class FaceTrackerEyeCalibrationViewModel : ViewModelBase
    {
        private readonly IMessageSender _sender;
        private readonly EyeBlendShapePreviewReceiver _previewReceiver;
        private readonly MotionSettingModel _model;

        public FaceTrackerEyeCalibrationViewModel() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<EyeBlendShapePreviewReceiver>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>()
            )
        {
        }

        internal FaceTrackerEyeCalibrationViewModel(
            IMessageSender sender,
            EyeBlendShapePreviewReceiver previewReceiver,
            MotionSettingModel model
            ) 
        {
            _sender = sender;
            _previewReceiver = previewReceiver;
            _model = model;

            EnableEyeBlendShapeValuePreview = new RProperty<bool>(
                false,
                v => _sender.SendMessage(MessageFactory.Instance.SetEyeBlendShapePreviewActive(v))
            );

            if (!IsInDesignMode)
            {
                WeakEventManager<EyeBlendShapePreviewReceiver, EyeBlendShapeValuesEventArgs>.AddHandler(
                    previewReceiver,
                    nameof(previewReceiver.EyeBlendShapeValuesReceived),
                    OnEyeBlendShapeValuesReceived
                );
            }
        }

        private void OnEyeBlendShapeValuesReceived(object? sender, EyeBlendShapeValuesEventArgs e)
        {
            LeftEyeBlendShape.SetValue(e.Values.LeftBlinkPercent);
            RightEyeBlendShape.SetValue(e.Values.RightBlinkPercent);
        }

        public EyeBlendShapeRangeViewModel LeftEyeBlendShape { get; } = new();
        public EyeBlendShapeRangeViewModel RightEyeBlendShape { get; } = new();


        public RProperty<int> EyeOpenBlinkValue => _model.WebCamEyeOpenBlinkValue;
        public RProperty<int> EyeCloseBlinkValue=> _model.WebCamEyeCloseBlinkValue;

        public RProperty<bool> EnableEyeBlendShapeValuePreview { get; }


        private ActionCommand? _resetCurrentBlendShapeValueCommand;
        public ICommand ResetCurrentBlendShapeValueCommand => _resetCurrentBlendShapeValueCommand ??= new ActionCommand(ResetCurrentBlendShapeValue);

        private void ResetCurrentBlendShapeValue()
        {
            LeftEyeBlendShape.ResetValues();
            RightEyeBlendShape.ResetValues();
        }

        public void RequestDisableEyeBlendShapeValuePreview()
        {
            _sender.SendMessage(MessageFactory.Instance.SetEyeBlendShapePreviewActive(false));
        }
    }
}
