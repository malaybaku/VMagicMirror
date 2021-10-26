using MaterialDesignThemes.Wpf;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    public class LayoutSettingViewModel : SettingViewModelBase
    {
        internal LayoutSettingViewModel(
            LayoutSettingSync model, GamepadSettingSync gamepadModel, IMessageSender sender, IMessageReceiver receiver
            ) : base(sender)
        {
            _model = model;
            _gamepadModel = gamepadModel;

            QuickSaveViewPointCommand = new ActionCommand<string>(async s => await _model.QuickSaveViewPoint(s));
            QuickLoadViewPointCommand = new ActionCommand<string>(_model.QuickLoadViewPoint);

            OpenTextureReplaceTipsUrlCommand = new ActionCommand(
                () => UrlNavigate.Open(LocalizedString.GetString("URL_tips_texture_replace"))
                );

            ResetCameraPositionCommand = new ActionCommand(() => SendMessage(MessageFactory.Instance.ResetCameraPosition()));

            ResetCameraSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetCameraSetting)
                );

            ResetDeviceLayoutCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetDeviceLayout)
                );

            ResetHidSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetHidSetting)
                );
            ResetCameraSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetCameraSetting)
                );
            ResetMidiSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetMidiSetting)
                );
            ShowPenUnavaiableWarningCommand = new ActionCommand(ShowPenUnavailableWarning);

            _model.SelectedTypingEffectId.PropertyChanged += (_, __) =>
            {
                TypingEffectItem = TypingEffectSelections
                    .FirstOrDefault(v => v.Id == _model.SelectedTypingEffectId.Value);
            };

            _typingEffectItem = TypingEffectSelections[0];
            receiver.ReceivedCommand += OnReceiveCommand;

        }

        private readonly LayoutSettingSync _model;
        //NOTE: ゲームパッド設定(表示/非表示)も使うため、ここに記載。ちょっと例外的な措置ではある
        private readonly GamepadSettingSync _gamepadModel;

        private void OnReceiveCommand(object? sender, CommandReceivedEventArgs e)
        {
            if (e.Command == ReceiveMessageNames.UpdateDeviceLayout)
            {
                //NOTE: Unity側から来た値なため、送り返さないでよいことに注意
                _model.DeviceLayout.SilentSet(e.Args);
            }
            else if (e.Command == ReceiveMessageNames.SetModelDoesNotSupportPen)
            {
                _model.ModelDoesNotSupportPen.Value = bool.TryParse(e.Args, out var result) ? result : false;
            }
        }

        public RProperty<int> CameraFov => _model.CameraFov;
        public RProperty<bool> EnableFreeCameraMode => _model.EnableFreeCameraMode;

        public RProperty<bool> EnableMidiRead => _model.EnableMidiRead;

        //NOTE: カメラ位置、デバイスレイアウト、クイックセーブした視点については、ユーザーが直接いじる想定ではない

        #region 視点のクイックセーブ/ロード

        //NOTE: これらの値はUIで「有効なデータを持ってるかどうか」という間接的な情報として使う
        public RProperty<string> QuickSave1 => _model.QuickSave1;
        public RProperty<string> QuickSave2 => _model.QuickSave2;
        public RProperty<string> QuickSave3 => _model.QuickSave3;

        public ActionCommand<string> QuickSaveViewPointCommand { get; }
        public ActionCommand<string> QuickLoadViewPointCommand { get; }

        #endregion

        public ActionCommand ResetCameraPositionCommand { get; }


        // デバイス類の表示/非表示
        public RProperty<bool> HidVisibility => _model.HidVisibility;
        public RProperty<bool> PenVisibility => _model.PenVisibility;
        public RProperty<bool> MidiControllerVisibility => _model.MidiControllerVisibility;
        public RProperty<bool> GamepadVisibility => _gamepadModel.GamepadVisibility;
        public RProperty<bool> PenUnavailable => _model.ModelDoesNotSupportPen;

        public RProperty<bool> EnableDeviceFreeLayout => _model.EnableDeviceFreeLayout;

        #region タイピングエフェクト

        public RProperty<int> SelectedTypingEffectId => _model.SelectedTypingEffectId;

        private TypingEffectSelectionItem? _typingEffectItem = null;
        public TypingEffectSelectionItem? TypingEffectItem
        {
            get => _typingEffectItem;
            set
            {
                //ここのガード文はComboBoxを意識した書き方なことに注意
                if (value == null || _typingEffectItem == value || (_typingEffectItem != null && _typingEffectItem.Id == value.Id))
                {
                    return;
                }

                _typingEffectItem = value;
                SelectedTypingEffectId.Value = _typingEffectItem.Id;
                RaisePropertyChanged();
            }
        }

        public TypingEffectSelectionItem[] TypingEffectSelections { get; } = new TypingEffectSelectionItem[]
        {
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexNone, "None", PackIconKind.EyeOff),
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexText, "Text", PackIconKind.Abc),
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexLight, "Light", PackIconKind.FlashOn),
            //new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexLaser, "Laser", PackIconKind.Wand),
            new TypingEffectSelectionItem(LayoutSetting.TypingEffectIndexButtefly, "Butterfly", PackIconKind.DotsHorizontal),
        };

        #endregion

        public ActionCommand OpenTextureReplaceTipsUrlCommand { get; }

        public ActionCommand ResetDeviceLayoutCommand { get; }
        public ActionCommand ResetHidSettingCommand { get; }
        public ActionCommand ResetCameraSettingCommand { get; }
        public ActionCommand ResetMidiSettingCommand { get; }

        public ActionCommand ShowPenUnavaiableWarningCommand { get; }

        private async void ShowPenUnavailableWarning()
        {
            var indication = MessageIndication.WarnInfoAboutPenUnavaiable();
            await MessageBoxWrapper.Instance.ShowAsync(indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OK);
        }

        //TODO: Recordで書きたい…
        public class TypingEffectSelectionItem
        {
            public TypingEffectSelectionItem(int id, string name, PackIconKind iconKind)
            {
                Id = id;
                EffectName = name;
                IconKind = iconKind;
            }
            public int Id { get; }
            public string EffectName { get; }
            public PackIconKind IconKind { get; }
        }
    }

}
