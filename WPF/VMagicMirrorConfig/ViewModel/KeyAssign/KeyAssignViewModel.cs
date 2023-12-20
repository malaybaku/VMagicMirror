using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

            ButtonA = new RProperty<GameInputActionKey>(
                gamepad.ButtonAKey, a => SetGamepadButtonAction(GameInputGamepadButton.A, a));
            ButtonB = new RProperty<GameInputActionKey>(
                gamepad.ButtonBKey, a => SetGamepadButtonAction(GameInputGamepadButton.B, a));
            ButtonX = new RProperty<GameInputActionKey>(
                gamepad.ButtonXKey, a => SetGamepadButtonAction(GameInputGamepadButton.X, a));
            ButtonY = new RProperty<GameInputActionKey>(
                gamepad.ButtonYKey, a => SetGamepadButtonAction(GameInputGamepadButton.Y, a));

            ButtonRB = new RProperty<GameInputActionKey>(
                gamepad.ButtonRButtonKey, a => SetGamepadButtonAction(GameInputGamepadButton.RB, a));
            ButtonLB = new RProperty<GameInputActionKey>(
                gamepad.ButtonLButtonKey, a => SetGamepadButtonAction(GameInputGamepadButton.LB, a));
            ButtonRTrigger = new RProperty<GameInputActionKey>(
                gamepad.ButtonRTriggerKey, a => SetGamepadButtonAction(GameInputGamepadButton.RTrigger, a));
            ButtonLTrigger = new RProperty<GameInputActionKey>(
                gamepad.ButtonLTriggerKey, a => SetGamepadButtonAction(GameInputGamepadButton.LTrigger, a));
            ButtonView = new RProperty<GameInputActionKey>(
                gamepad.ButtonViewKey, a => SetGamepadButtonAction(GameInputGamepadButton.View, a));
            ButtonMenu = new RProperty<GameInputActionKey>(
                gamepad.ButtonMenuKey, a => SetGamepadButtonAction(GameInputGamepadButton.Menu, a));

            StickLeft = new RProperty<GameInputStickAction>(
                gamepad.StickLeft, a => SetGamepadStickAction(GameInputGamepadStick.Left, a));
            StickRight = new RProperty<GameInputStickAction>(
                gamepad.StickRight, a => SetGamepadStickAction(GameInputGamepadStick.Right, a));
            DPadLeft = new RProperty<GameInputStickAction>(
                gamepad.DPadLeft, a => SetGamepadStickAction(GameInputGamepadStick.DPadLeft, a));


            LeftClick = new RProperty<GameInputActionKey>(
                keyboard.LeftClickKey, a => SetMouseClickAction(GameInputMouseButton.Left, a));
            RightClick = new RProperty<GameInputActionKey>(
                keyboard.RightClickKey, a => SetMouseClickAction(GameInputMouseButton.Right, a));
            MiddleClick = new RProperty<GameInputActionKey>(
                keyboard.MiddleClickKey, a => SetMouseClickAction(GameInputMouseButton.Middle, a));

            ResetSettingsCommand = new ActionCommand(ResetSetting);
            LoadSettingFileCommand = new ActionCommand(LoadSetting);
            SaveSettingFileCommand = new ActionCommand(SaveSetting);

            OpenDocUrlCommand = new ActionCommand(
                () => UrlNavigate.Open(LocalizedString.GetString("URL_docs_game_input"))
                );

            ButtonActions = Array.Empty<GameInputActionKey>();

            if (IsInDesignMode)
            {
                return;
            }

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

            ButtonActions = _model.LoadAvailableActionKeys();

            var keyAssignViewModels = new List<GameInputKeyAssignItemViewModel>();
            keyAssignViewModels.AddRange(
                new (GameInputButtonAction action, string keyCode)[]
                {
                    (GameInputButtonAction.Jump,_model.KeyboardKeyAssign.JumpKeyCode),
                    (GameInputButtonAction.Crouch, _model.KeyboardKeyAssign.CrouchKeyCode),
                    (GameInputButtonAction.Run, _model.KeyboardKeyAssign.RunKeyCode),
                    (GameInputButtonAction.Trigger, _model.KeyboardKeyAssign.TriggerKeyCode),
                    (GameInputButtonAction.Punch, _model.KeyboardKeyAssign.PunchKeyCode),
                }.Select(pair => CreateKeyAssignItemViewModel(
                    GameInputActionKey.BuiltIn(pair.action),
                    pair.keyCode
                    )
                ));

            keyAssignViewModels.AddRange(_model
                .LoadCustomActionKeys()
                .Select(actionKey => CreateKeyAssignItemViewModel(
                    actionKey,
                    _model.FindKeyCodeOfCustomAction(actionKey)
                    )
                ));


            foreach (var keyAssign in keyAssignViewModels)
            {
                KeyAssigns.Add(keyAssign);
            }
        }

        private readonly GameInputSettingModel _model;
        private bool _silentMode = false;

        public RProperty<bool> GamepadEnabled => _model.GamepadEnabled;
        public RProperty<bool> KeyboardEnabled => _model.KeyboardEnabled;

        public RProperty<bool> AlwaysRun => _model.AlwaysRun;
        public RProperty<GameInputLocomotionStyle> LocomotionStyle => _model.LocomotionStyle;

        public RProperty<GameInputActionKey> ButtonA { get; }
        public RProperty<GameInputActionKey> ButtonB { get; }
        public RProperty<GameInputActionKey> ButtonX { get; }
        public RProperty<GameInputActionKey> ButtonY { get; }

        //NOTE: LTriggerはボタンと連続値どっちがいいの、みたいな話もある
        public RProperty<GameInputActionKey> ButtonLB { get; }
        public RProperty<GameInputActionKey> ButtonLTrigger { get; }
        public RProperty<GameInputActionKey> ButtonRB { get; }
        public RProperty<GameInputActionKey> ButtonRTrigger { get; }

        public RProperty<GameInputActionKey> ButtonView { get; }
        public RProperty<GameInputActionKey> ButtonMenu { get; }

        public RProperty<GameInputStickAction> DPadLeft { get; }
        public RProperty<GameInputStickAction> StickLeft { get; }
        public RProperty<GameInputStickAction> StickRight { get; }


        public RProperty<GameInputActionKey> LeftClick { get; }
        public RProperty<GameInputActionKey> RightClick { get; }
        public RProperty<GameInputActionKey> MiddleClick { get; }

        public RProperty<bool> UseMouseToLookAround => _model.UseMouseToLookAround;

        public RProperty<bool> UseWasdMove => _model.UseWasdMove;
        public RProperty<bool> UseArrowKeyMove => _model.UseArrowKeyMove;
        public RProperty<bool> UseShiftRun => _model.UseShiftRun;
        public RProperty<bool> UseSpaceJump => _model.UseSpaceJump;

        public ActionCommand ResetSettingsCommand { get; }
        public ActionCommand SaveSettingFileCommand { get; }
        public ActionCommand LoadSettingFileCommand { get; }
        public ActionCommand OpenDocUrlCommand { get; }

        public ObservableCollection<GameInputKeyAssignItemViewModel> KeyAssigns { get; } = new();

        public GameInputLocomotionStyleViewModel[] LocomotionStyles => GameInputLocomotionStyleViewModel.AvailableItems;
        public GameInputStickActionItemViewModel[] StickActions => GameInputStickActionItemViewModel.AvailableItems;

//        public GameInputButtonActionItemViewModel[] ButtonActions => GameInputButtonActionItemViewModel.AvailableItems;
        public GameInputActionKey[] ButtonActions { get; }

        private void SetGamepadButtonAction(GameInputGamepadButton button, GameInputActionKey actionKey)
        {
            if (_silentMode)
            { 
                return;
            }
            _model.SetGamepadButtonAction(button, actionKey);
        }

        public void SetGamepadStickAction(GameInputGamepadStick stick, GameInputStickAction action)
        {
            if (_silentMode)
            {
                return;
            }
            _model.SetGamepadStickAction(stick, action);
        }

        public void SetMouseClickAction(GameInputMouseButton button, GameInputActionKey actionKey)
        {
            if (_silentMode)
            {
                return;
            }
            _model.SetClickAction(button, actionKey);
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
                ButtonA.Value = data.ButtonAKey;
                ButtonB.Value = data.ButtonBKey;
                ButtonX.Value = data.ButtonXKey;
                ButtonY.Value = data.ButtonYKey;

                ButtonLB.Value = data.ButtonLButtonKey;
                ButtonRB.Value = data.ButtonRButtonKey;
                ButtonLTrigger.Value = data.ButtonLTriggerKey;
                ButtonRTrigger.Value = data.ButtonRTriggerKey;

                ButtonView.Value = data.ButtonViewKey;
                ButtonMenu.Value = data.ButtonMenuKey;

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
                LeftClick.Value = data.LeftClickKey;
                RightClick.Value = data.RightClickKey;
                MiddleClick.Value = data.MiddleClickKey;

                KeyAssigns.FirstOrDefault(a => a.ActionKey.ActionType == GameInputButtonAction.Jump)?.SetKey(data.JumpKeyCode);
                KeyAssigns.FirstOrDefault(a => a.ActionKey.ActionType == GameInputButtonAction.Crouch)?.SetKey(data.CrouchKeyCode);
                KeyAssigns.FirstOrDefault(a => a.ActionKey.ActionType == GameInputButtonAction.Run)?.SetKey(data.RunKeyCode);
                KeyAssigns.FirstOrDefault(a => a.ActionKey.ActionType == GameInputButtonAction.Trigger)?.SetKey(data.TriggerKeyCode);
                KeyAssigns.FirstOrDefault(a => a.ActionKey.ActionType == GameInputButtonAction.Punch)?.SetKey(data.PunchKeyCode);

                foreach(var customAction in data.CustomActions)
                {
                    var customKey = GameInputActionKey.Custom(customAction.CustomAction.CustomKey);
                    var assignedItem = KeyAssigns.FirstOrDefault(a => a.ActionKey.Equals(customKey));
                    if (assignedItem == null)
                    {
                        assignedItem = CreateKeyAssignItemViewModel(customKey, customAction.KeyCode);
                        KeyAssigns.Add(assignedItem);
                    }
                    else
                    {
                        assignedItem.SetKey(customAction.KeyCode);
                    }
                }

                //Model側が持ってないカスタムモーションのぶんを削除
                //TODO: もうちょいカッコよく書きたい…
                var removeTarget = KeyAssigns
                    .Where(k =>
                        k.ActionKey.ActionType is GameInputButtonAction.Custom &&
                        !data.CustomActions.Any(a => a.CustomAction.CustomKey == k.ActionKey.CustomActionKey)
                    )
                    .ToArray();
                foreach( var item in removeTarget) 
                {
                    DisposeKeyAssignItemViewModel(item);
                }
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

        private GameInputKeyAssignItemViewModel CreateKeyAssignItemViewModel(GameInputActionKey actionKey, string keyCode)
        {
            var vm = new GameInputKeyAssignItemViewModel(actionKey, keyCode);
            vm.RegisteredKeyChanged += OnRegisteredKeyChanged;
            return vm;
        }

        private void DisposeKeyAssignItemViewModel(GameInputKeyAssignItemViewModel vm)
        {
            vm.RegisteredKeyChanged -= OnRegisteredKeyChanged;
            KeyAssigns.Remove(vm);
        }

        private void OnRegisteredKeyChanged((GameInputActionKey actionKey, string keyCode) value)
            => _model.SetKeyAction(value.actionKey, value.keyCode);

        private void ResetSetting()
        {
            SettingResetUtils.ResetSingleCategoryAsync(() => _model.ResetToDefault());
        }

    }

    /// <summary>
    /// ゲーム入力で移動入力をどういう風に扱うかのオプションを示すViewModel
    /// </summary>
    public class GameInputLocomotionStyleViewModel
    {
        public GameInputLocomotionStyleViewModel(GameInputLocomotionStyle style, string localizationKey)
        {
            Style = style;
            _localizationKey = localizationKey;
            Label.Value = LocalizedString.GetString(_localizationKey);
            LanguageSelector.Instance.LanguageChanged +=
                () => Label.Value = LocalizedString.GetString(_localizationKey);
        }

        private readonly string _localizationKey;
        public GameInputLocomotionStyle Style { get; }
        public RProperty<string> Label { get; } = new RProperty<string>("");

        //NOTE: immutable arrayのほうが性質は良いのでそうしてもよい
        public static GameInputLocomotionStyleViewModel[] AvailableItems { get; } = new GameInputLocomotionStyleViewModel[]
        {
            new (GameInputLocomotionStyle.FirstPerson, "GameInputKeyAssign_LocomotionStyle_FirstPerson"),
            new (GameInputLocomotionStyle.ThirdPerson, "GameInputKeyAssign_LocomotionStyle_ThirdPerson"),
            new (GameInputLocomotionStyle.SideView2D, "GameInputKeyAssign_LocomotionStyle_SideView2D"),
        };
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
            if (!string.IsNullOrEmpty(_localizationKey))
            {
                Label.Value = LocalizedString.GetString(_localizationKey);
                LanguageSelector.Instance.LanguageChanged +=
                    () => Label.Value = LocalizedString.GetString(_localizationKey);
            }
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
