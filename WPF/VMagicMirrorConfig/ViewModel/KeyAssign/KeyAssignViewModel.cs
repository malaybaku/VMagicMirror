using Microsoft.Win32;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{

    public class KeyAssignViewModel : ViewModelBase
    {
        public KeyAssignViewModel() : this(
            ModelResolver.Instance.Resolve<GameInputSettingModel>()
            )
        {
        }

        internal KeyAssignViewModel(GameInputSettingModel model)
        {
            _model = model;

            var gamepad = IsInDesignMode ? new GameInputGamepadKeyAssign() : model.GamepadKeyAssign;
            var keyboard = IsInDesignMode ? new GameInputKeyboardKeyAssign() : model.KeyboardKeyAssign;

            ButtonA = new RProperty<GameInputButtonAction>(
                gamepad.ButtonA, a => SetGamepadButtonAction(GameInputGamepadButton.A, a));
            ButtonB = new RProperty<GameInputButtonAction>(
                gamepad.ButtonB, a => SetGamepadButtonAction(GameInputGamepadButton.B, a));
            ButtonX = new RProperty<GameInputButtonAction>(
                gamepad.ButtonX, a => SetGamepadButtonAction(GameInputGamepadButton.X, a));
            ButtonY = new RProperty<GameInputButtonAction>(
                gamepad.ButtonY, a => SetGamepadButtonAction(GameInputGamepadButton.Y, a));

            ButtonRB = new RProperty<GameInputButtonAction>(
                gamepad.ButtonRButton, a => SetGamepadButtonAction(GameInputGamepadButton.RB, a));
            ButtonLB = new RProperty<GameInputButtonAction>(
                gamepad.ButtonLButton, a => SetGamepadButtonAction(GameInputGamepadButton.LB, a));
            ButtonRTrigger = new RProperty<GameInputButtonAction>(
                gamepad.ButtonRTrigger, a => SetGamepadButtonAction(GameInputGamepadButton.RTrigger, a));
            ButtonLTrigger = new RProperty<GameInputButtonAction>(
                gamepad.ButtonLTrigger, a => SetGamepadButtonAction(GameInputGamepadButton.LTrigger, a));
            ButtonView = new RProperty<GameInputButtonAction>(
                gamepad.ButtonView, a => SetGamepadButtonAction(GameInputGamepadButton.View, a));
            ButtonMenu = new RProperty<GameInputButtonAction>(
                gamepad.ButtonMenu, a => SetGamepadButtonAction(GameInputGamepadButton.Menu, a));

            StickLeft = new RProperty<GameInputStickAction>(
                gamepad.StickLeft, a => SetGamepadStickAction(GameInputGamepadStick.Left, a));
            StickRight = new RProperty<GameInputStickAction>(
                gamepad.StickRight, a => SetGamepadStickAction(GameInputGamepadStick.Right, a));
            DPadLeft = new RProperty<GameInputStickAction>(
                gamepad.DPadLeft, a => SetGamepadStickAction(GameInputGamepadStick.DPadLeft, a));


            LeftClick = new RProperty<GameInputButtonAction>(
                keyboard.LeftClick, a => SetMouseClickAction(GameInputMouseButton.Left, a));
            RightClick = new RProperty<GameInputButtonAction>(
                keyboard.RightClick, a => SetMouseClickAction(GameInputMouseButton.Right, a));
            MiddleClick = new RProperty<GameInputButtonAction>(
                keyboard.MiddleClick, a => SetMouseClickAction(GameInputMouseButton.Middle, a));

            ResetSettingsCommand = new ActionCommand(ResetSetting);
            LoadSettingFileCommand = new ActionCommand(LoadSetting);
            SaveSettingFileCommand = new ActionCommand(SaveSetting);

            if (!IsInDesignMode)
            {
                WeakEventManager<GameInputSettingModel, GamepadKeyAssignUpdateEventArgs>.AddHandler(
                    _model,
                    nameof(_model.GamepadKeyAssignUpdated),
                    OnGamepadKeyAssignUpdated
                    );

                WeakEventManager<GameInputSettingModel, KeyboardKeyAssignUpdateEventArgs>.AddHandler(
                    _model,
                    nameof(_model.KeyboardKeyAssignUpdated),
                    OnKeyboardKeyAssignUpdated
                    );
            }
        }

        private readonly GameInputSettingModel _model;
        private bool _silentMode = false;

        public RProperty<bool> GamepadEnabled => _model.GamepadEnabled;
        public RProperty<bool> KeyboardEnabled => _model.KeyboardEnabled;

        public RProperty<bool> AlwaysRun => _model.AlwaysRun;

        public RProperty<GameInputButtonAction> ButtonA { get; }
        public RProperty<GameInputButtonAction> ButtonB { get; }
        public RProperty<GameInputButtonAction> ButtonX { get; }
        public RProperty<GameInputButtonAction> ButtonY { get; }

        //NOTE: LTriggerはボタンと連続値どっちがいいの、みたいな話もある
        public RProperty<GameInputButtonAction> ButtonLB { get; }
        public RProperty<GameInputButtonAction> ButtonLTrigger { get; }
        public RProperty<GameInputButtonAction> ButtonRB { get; }
        public RProperty<GameInputButtonAction> ButtonRTrigger { get; }

        public RProperty<GameInputButtonAction> ButtonView { get; }
        public RProperty<GameInputButtonAction> ButtonMenu { get; }

        public RProperty<GameInputStickAction> DPadLeft { get; }
        public RProperty<GameInputStickAction> StickLeft { get; }
        public RProperty<GameInputStickAction> StickRight { get; }


        public RProperty<GameInputButtonAction> LeftClick { get; }
        public RProperty<GameInputButtonAction> RightClick { get; }
        public RProperty<GameInputButtonAction> MiddleClick { get; }

        public RProperty<bool> UseMouseToLookAround => _model.UseMouseToLookAround;

        public RProperty<bool> UseWasdMove => _model.UseWasdMove;
        public RProperty<bool> UseArrowKeyMove => _model.UseArrowKeyMove;
        public RProperty<bool> UseShiftRun => _model.UseShiftRun;
        public RProperty<bool> UseSpaceJump => _model.UseSpaceJump;

        public RProperty<string> JumpKeyCode { get; } = new RProperty<string>("");
        public RProperty<string> RunKeyCode { get; } = new RProperty<string>("");
        public RProperty<string> CrouchKeyCode { get; } = new RProperty<string>("");

        public RProperty<string> TriggerKeyCode { get; } = new RProperty<string>("");
        public RProperty<string> PunchKeyCode { get; } = new RProperty<string>("");

        public ActionCommand ResetSettingsCommand { get; }
        public ActionCommand SaveSettingFileCommand { get; }
        public ActionCommand LoadSettingFileCommand { get; }

        public GameInputStickActionItemViewModel[] StickActions => GameInputStickActionItemViewModel.AvailableItems;
        public GameInputButtonActionItemViewModel[] ButtonActions => GameInputButtonActionItemViewModel.AvailableItems;


        private void SetGamepadButtonAction(GameInputGamepadButton button, GameInputButtonAction action)
        {
            if (_silentMode)
            { 
                return;
            }
            _model.SetGamepadButtonAction(button, action);
        }

        public void SetGamepadStickAction(GameInputGamepadStick stick, GameInputStickAction action)
        {
            if (_silentMode)
            {
                return;
            }
            _model.SetGamepadStickAction(stick, action);
        }

        public void SetMouseClickAction(GameInputMouseButton button, GameInputButtonAction action)
        {
            if (_silentMode)
            {
                return;
            }
            _model.SetClickAction(button, action);
        }

        private void OnGamepadKeyAssignUpdated(object? sender, GamepadKeyAssignUpdateEventArgs e)
            => UpdateGamepadKeyAssign(e.Data);
        private void OnKeyboardKeyAssignUpdated(object? sender, KeyboardKeyAssignUpdateEventArgs e)
            => UpdateKeyboardKeyAssign(e.Data);

        private void UpdateGamepadKeyAssign(GameInputGamepadKeyAssign data)
        {
            _silentMode = true;
            try
            {
                ButtonA.Value = data.ButtonA;
                ButtonB.Value = data.ButtonB;
                ButtonX.Value = data.ButtonX;
                ButtonY.Value = data.ButtonY;

                ButtonLB.Value = data.ButtonLButton;
                ButtonRB.Value = data.ButtonRButton;
                ButtonLTrigger.Value = data.ButtonLTrigger;
                ButtonRTrigger.Value = data.ButtonRTrigger;

                ButtonView.Value = data.ButtonView;
                ButtonMenu.Value = data.ButtonMenu;

                StickLeft.Value = data.StickLeft;
                StickRight.Value = data.StickRight;
                DPadLeft.Value = data.DPadLeft;
            }
            finally
            {
                _silentMode = false;
            }            
        }

        private void UpdateKeyboardKeyAssign(GameInputKeyboardKeyAssign data)
        {
            _silentMode = true;
            try
            {
                LeftClick.Value = data.LeftClick;
                RightClick.Value = data.RightClick;
                MiddleClick.Value = data.MiddleClick;
                //これ以外はDataではなく個別のPropertyとして飛んでくるものしか使ってないので、一旦OK
            }
            finally
            {
                _silentMode = false;
            }
        }

        private void SaveSetting()
        {
            const string ext = SpecialFilePath.GameInputSettingFileExt;
            var dialog = new SaveFileDialog()
            {
                Title = "Save VMagicMirror Game Input Setting",
                Filter = $"VMM Game Input Setting (*{ext})|*{ext}",
                DefaultExt = ext,
                AddExtension = true,
            };

            if (dialog.ShowDialog() == true)
            {
                _model.SaveSetting(dialog.FileName);
            }
        }

        private void LoadSetting()
        {
            const string ext = SpecialFilePath.GameInputSettingFileExt;

            var dialog = new OpenFileDialog()
            {
                Title = "Load VMagicMirror Game Input Setting",
                Filter = $"VMM Game Input Setting (*{ext})|*{ext}",
                DefaultExt = ext,
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true)
            {
                _model.LoadSetting(dialog.FileName);
            }
        }

        private void ResetSetting()
        {
            SettingResetUtils.ResetSingleCategoryAsync(() => _model.ResetToDefault());
        }

    }


    /// <summary>
    /// ゲーム入力のうちスティックで取れるアクションを定義したもの
    /// </summary>
    public class GameInputStickActionItemViewModel
    {
        public GameInputStickActionItemViewModel(GameInputStickAction action, string localizationKey)
        {
            Action = action;
            _localizationKey = localizationKey;
            Label.Value = LocalizedString.GetString(_localizationKey);
            LanguageSelector.Instance.LanguageChanged +=
                () => Label.Value = LocalizedString.GetString(_localizationKey);
        }

        private readonly string _localizationKey;
        public GameInputStickAction Action { get; }
        public RProperty<string> Label { get; } = new RProperty<string>("");

        //NOTE: immutable arrayのほうが性質は良いのでそうしてもよい
        public static GameInputStickActionItemViewModel[] AvailableItems { get; } = new GameInputStickActionItemViewModel[]
        {
            new (GameInputStickAction.None, "GameInputKeyAssign_Action_None"),
            new (GameInputStickAction.Move, "GameInputKeyAssign_StickAction_Move"),
            new (GameInputStickAction.LookAround, "GameInputKeyAssign_StickAction_LookAround"),
        };
    }

    /// <summary>
    /// ゲーム入力のうちスティックで取れるアクションを定義したもの
    /// </summary>
    public class GameInputButtonActionItemViewModel
    {
        public GameInputButtonActionItemViewModel(GameInputButtonAction action, string localizationKey)
        {
            Action = action;
            _localizationKey = localizationKey;
            Label.Value = LocalizedString.GetString(_localizationKey);
            LanguageSelector.Instance.LanguageChanged +=
                () => Label.Value = LocalizedString.GetString(_localizationKey);
        }

        private readonly string _localizationKey;
        public GameInputButtonAction Action { get; }
        public RProperty<string> Label { get; } = new RProperty<string>("");

        //NOTE: immutable arrayのほうが性質は良いのでそうしてもよい
        public static GameInputButtonActionItemViewModel[] AvailableItems { get; } = new GameInputButtonActionItemViewModel[]
        {
            new (GameInputButtonAction.None, "GameInputKeyAssign_Action_None"),
            new (GameInputButtonAction.Jump, "GameInputKeyAssign_ButtonAction_Jump"),
            new (GameInputButtonAction.Crouch, "GameInputKeyAssign_ButtonAction_Crouch"),
            new (GameInputButtonAction.Run, "GameInputKeyAssign_ButtonAction_Run"),
            new (GameInputButtonAction.Trigger, "GameInputKeyAssign_ButtonAction_Trigger"),
            new (GameInputButtonAction.Punch, "GameInputKeyAssign_ButtonAction_Punch"),
        };
    }
}
