using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    class GameInputSettingModel
    {
        public GameInputSettingModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public GameInputSettingModel(IMessageSender sender)
        {
            _sender = sender;
            var setting = GameInputSetting.Default;

            GamepadKeyAssign = setting.GamepadKeyAssign;
            KeyboardKeyAssign = setting.KeyboardKeyAssign;

            var factory = MessageFactory.Instance;

            GamepadEnabled = new RProperty<bool>(setting.GamepadEnabled, v => SendMessage(factory.UseGamepadForGameInput(v)));
            KeyboardEnabled = new RProperty<bool>(setting.KeyboardEnabled, v => SendMessage(factory.UseKeyboardForGameInput(v)));
            AlwaysRun = new RProperty<bool>(setting.AlwaysRun, v => SendMessage(factory.EnableAlwaysRunGameInput(v)));

            UseMouseToLookAround = new RProperty<bool>(KeyboardKeyAssign.UseMouseLookAround, v =>
            {
                KeyboardKeyAssign.UseMouseLookAround = v;
                SendMessage(factory.UseMouseMoveForLookAroundGameInput(v));
            });

            UseWasdMove = new RProperty<bool>(KeyboardKeyAssign.UseWasdMove, v =>
            {
                KeyboardKeyAssign.UseWasdMove = v;
                SendMessage(factory.EnableWasdMoveGameInput(v));
            });

            UseArrowKeyMove = new RProperty<bool>(KeyboardKeyAssign.UseArrowKeyMove, v =>
            {
                KeyboardKeyAssign.UseArrowKeyMove = v;
                SendMessage(factory.EnableArrowKeyMoveGameInput(v));
            });

            UseShiftRun = new RProperty<bool>(KeyboardKeyAssign.UseShiftRun, v =>
            {
                KeyboardKeyAssign.UseShiftRun = v;
                SendMessage(factory.UseShiftRunGameInput(v));
            });

            UseSpaceJump = new RProperty<bool>(KeyboardKeyAssign.UseSpaceJump, v =>
            {
                KeyboardKeyAssign.UseSpaceJump = v;
                SendMessage(factory.UseSpaceJumpGameInput(v));
            });

        }

        private readonly IMessageSender _sender;

        public event EventHandler<GamepadKeyAssignUpdateEventArgs>? GamepadKeyAssignUpdated;
        public event EventHandler<KeyboardKeyAssignUpdateEventArgs>? KeyboardKeyAssignUpdated;

        public GameInputGamepadKeyAssign GamepadKeyAssign { get; private set; }
        public GameInputKeyboardKeyAssign KeyboardKeyAssign { get; private set; }

        public RProperty<bool> GamepadEnabled { get; }
        public RProperty<bool> KeyboardEnabled { get; }
        public RProperty<bool> AlwaysRun { get; }
        
        public RProperty<bool> UseMouseToLookAround { get; }
        public RProperty<bool> UseWasdMove { get; }
        public RProperty<bool> UseArrowKeyMove { get; }
        public RProperty<bool> UseShiftRun { get; }
        public RProperty<bool> UseSpaceJump { get; }

        public static string GetSettingFolderPath() => SpecialFilePath.SaveFileDir;
        public static string GetFileIoExt() => SpecialFilePath.GameInputSettingFileExt;

        public void LoadSetting(string filePath)
        {
            if (!File.Exists(filePath))
            {
                //初起動時にここを通ることも踏まえて、明示的に現在の設定を投げつけておく
                SendGamepadKeyAssign();
                SendKeyboardKeyAssign();
                return;
            }

            try
            {
                var text = File.ReadAllText(filePath);
                var serializer = new JsonSerializer();
                using var reader = new StringReader(text);
                using var jsonReader = new JsonTextReader(reader);
                
                var setting = serializer.Deserialize<GameInputSetting>(jsonReader);
                if (setting == null || setting.KeyboardKeyAssign == null || setting.GamepadKeyAssign == null)
                {
                    return;
                }

                ApplySetting(setting);
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        public void SaveSetting(string filePath)
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();

            using var writer = new StringWriter(sb);
            using var jsonWriter = new JsonTextWriter(writer);
            try
            {
                serializer.Serialize(jsonWriter, BuildCurrentSetting());
                var json = sb.ToString();
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        public void LoadSettingFromDefaultFile() => LoadSetting(SpecialFilePath.GameInputDefaultFilePath);
        public void SaveSettingToDefaultFile() => SaveSetting(SpecialFilePath.GameInputDefaultFilePath);

        public void SetGamepadButtonAction(GameInputGamepadButton button, GameInputButtonAction action)
        {
            var current = button switch
            {
                GameInputGamepadButton.A => GamepadKeyAssign.ButtonA,
                GameInputGamepadButton.B => GamepadKeyAssign.ButtonB,
                GameInputGamepadButton.X => GamepadKeyAssign.ButtonX,
                GameInputGamepadButton.Y => GamepadKeyAssign.ButtonY,
                GameInputGamepadButton.LB => GamepadKeyAssign.ButtonLButton,
                GameInputGamepadButton.RB => GamepadKeyAssign.ButtonRButton,
                GameInputGamepadButton.LTrigger => GamepadKeyAssign.ButtonLTrigger,
                GameInputGamepadButton.RTrigger => GamepadKeyAssign.ButtonRTrigger,
                GameInputGamepadButton.View => GamepadKeyAssign.ButtonView,
                GameInputGamepadButton.Menu => GamepadKeyAssign.ButtonMenu,
                _ => GameInputButtonAction.None,
            };

            if (action == current)
            {
                return;
            }

            switch(button)
            {
                case GameInputGamepadButton.A: GamepadKeyAssign.ButtonA = action; break;
                case GameInputGamepadButton.B: GamepadKeyAssign.ButtonB = action; break;
                case GameInputGamepadButton.X: GamepadKeyAssign.ButtonX = action; break;
                case GameInputGamepadButton.Y: GamepadKeyAssign.ButtonY = action; break;
                case GameInputGamepadButton.LB: GamepadKeyAssign.ButtonLButton = action; break;
                case GameInputGamepadButton.RB: GamepadKeyAssign.ButtonRButton = action; break;
                case GameInputGamepadButton.LTrigger: GamepadKeyAssign.ButtonLTrigger = action; break;
                case GameInputGamepadButton.RTrigger: GamepadKeyAssign.ButtonRTrigger = action; break;
                case GameInputGamepadButton.View: GamepadKeyAssign.ButtonView = action; break;
                case GameInputGamepadButton.Menu: GamepadKeyAssign.ButtonMenu = action; break;
                default: return;
            }

            SendGamepadKeyAssign();
            GamepadKeyAssignUpdated?.Invoke(this, new GamepadKeyAssignUpdateEventArgs(GamepadKeyAssign));
        }

        public void SetGamepadStickAction(GameInputGamepadStick stick, GameInputStickAction action)
        {
            var current = stick switch
            {
                GameInputGamepadStick.Left => GamepadKeyAssign.StickLeft,
                GameInputGamepadStick.Right => GamepadKeyAssign.StickRight,
                GameInputGamepadStick.DPadLeft => GamepadKeyAssign.DPadLeft,
                _ => GameInputStickAction.None,
            };

            if (action == current)
            {
                return;
            }

            switch (stick)
            {
                case GameInputGamepadStick.Left: GamepadKeyAssign.StickLeft = action; break;
                case GameInputGamepadStick.Right: GamepadKeyAssign.StickRight = action; break;
                case GameInputGamepadStick.DPadLeft: GamepadKeyAssign.DPadLeft = action; break;
                default: return;
            }

            SendGamepadKeyAssign();
            GamepadKeyAssignUpdated?.Invoke(this, new GamepadKeyAssignUpdateEventArgs(GamepadKeyAssign));
        }

        public void SetClickAction(GameInputMouseButton button, GameInputButtonAction action)
        {
            var current = button switch
            {
                GameInputMouseButton.Left => KeyboardKeyAssign.LeftClick,
                GameInputMouseButton.Right => KeyboardKeyAssign.RightClick,
                GameInputMouseButton.Middle => KeyboardKeyAssign.MiddleClick,
                _ => GameInputButtonAction.None,
            };

            if (action == current)
            {
                return;
            }

            switch (button)
            {
                case GameInputMouseButton.Left: KeyboardKeyAssign.LeftClick = action; break;
                case GameInputMouseButton.Right: KeyboardKeyAssign.RightClick = action; break;
                case GameInputMouseButton.Middle: KeyboardKeyAssign.MiddleClick = action; break;
                default: return;
            }

            SendKeyboardKeyAssign();
            KeyboardKeyAssignUpdated?.Invoke(this, new KeyboardKeyAssignUpdateEventArgs(KeyboardKeyAssign));
        }

        public void SetKeyAction(GameInputButtonAction action, string key)
        {
            var current = action switch
            {
                GameInputButtonAction.Jump => KeyboardKeyAssign.JumpKeyCode,
                GameInputButtonAction.Crouch => KeyboardKeyAssign.CrouchKeyCode,
                GameInputButtonAction.Run => KeyboardKeyAssign.RunKeyCode,
                GameInputButtonAction.Trigger => KeyboardKeyAssign.TriggerKeyCode,
                GameInputButtonAction.Punch => KeyboardKeyAssign.PunchKeyCode,
                _ => "",
            };

            if (key == current)
            {
                return;
            }

            switch (action)
            {
                case GameInputButtonAction.Jump: KeyboardKeyAssign.JumpKeyCode = key; break;
                case GameInputButtonAction.Crouch: KeyboardKeyAssign.CrouchKeyCode = key; break;
                case GameInputButtonAction.Run: KeyboardKeyAssign.RunKeyCode = key; break;
                case GameInputButtonAction.Trigger: KeyboardKeyAssign.TriggerKeyCode = key; break;
                case GameInputButtonAction.Punch: KeyboardKeyAssign.PunchKeyCode = key; break;
                default: return;
            }

            SendKeyboardKeyAssign();
            KeyboardKeyAssignUpdated?.Invoke(this, new KeyboardKeyAssignUpdateEventArgs(KeyboardKeyAssign));
        }

        public void ResetToDefault() => ApplySetting(GameInputSetting.Default);


        private GameInputSetting BuildCurrentSetting()
        {
            //NOTE: KeyboardKeyAssignの値は逐一RP<T>と同期するので再書き込みは不要
            return new GameInputSetting()
            {
                GamepadEnabled = GamepadEnabled.Value,
                KeyboardEnabled = KeyboardEnabled.Value,
                AlwaysRun = AlwaysRun.Value,
                KeyboardKeyAssign = KeyboardKeyAssign,
                GamepadKeyAssign = GamepadKeyAssign,
            };
        }

        void ApplySetting(GameInputSetting setting)
        {
            GamepadEnabled.Value = setting.GamepadEnabled;
            KeyboardEnabled.Value = setting.KeyboardEnabled;
            AlwaysRun.Value = setting.AlwaysRun;

            UseMouseToLookAround.Value = setting.KeyboardKeyAssign.UseMouseLookAround;
            UseWasdMove.Value = setting.KeyboardKeyAssign.UseWasdMove;
            UseArrowKeyMove.Value = setting.KeyboardKeyAssign.UseArrowKeyMove;
            UseShiftRun.Value = setting.KeyboardKeyAssign.UseShiftRun;
            UseSpaceJump.Value = setting.KeyboardKeyAssign.UseSpaceJump;

            KeyboardKeyAssign = setting.KeyboardKeyAssign;
            GamepadKeyAssign = setting.GamepadKeyAssign;

            //NOTE: Loadのときだけは冗長になっても良いので送っとく
            SendGamepadKeyAssign();
            SendKeyboardKeyAssign();
            GamepadKeyAssignUpdated?.Invoke(this, new(GamepadKeyAssign));
            KeyboardKeyAssignUpdated?.Invoke(this, new(KeyboardKeyAssign));
        }

        void SendGamepadKeyAssign()
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            using var jsonWriter = new JsonTextWriter(writer);

            serializer.Serialize(jsonWriter, GamepadKeyAssign);
            var json = sb.ToString();
            SendMessage(MessageFactory.Instance.SetGamepadGameInputKeyAssign(json));
        }

        private void SendKeyboardKeyAssign()
        {
            //NOTE: 情報によっては個別に送ってるので冗長になる
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            using var jsonWriter = new JsonTextWriter(writer);

            serializer.Serialize(jsonWriter, KeyboardKeyAssign.GetKeyCodeTranslatedData());
            var json = sb.ToString();
            SendMessage(MessageFactory.Instance.SetKeyboardGameInputKeyAssign(json));
        }


        private void SendMessage(Message message) => _sender.SendMessage(message);
    }

    public class GamepadKeyAssignUpdateEventArgs : EventArgs
    {
        public GamepadKeyAssignUpdateEventArgs(GameInputGamepadKeyAssign data)
        {
            Data = data;
        }
        public GameInputGamepadKeyAssign Data { get; }
    }

    public class KeyboardKeyAssignUpdateEventArgs : EventArgs
    {
        public KeyboardKeyAssignUpdateEventArgs(GameInputKeyboardKeyAssign data)
        {
            Data = data;
        }
        public GameInputKeyboardKeyAssign Data { get; }
    }
}
