﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    class GameInputSettingModel
    {
        public GameInputSettingModel() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<CustomMotionList>()
            )
        {
        }

        public GameInputSettingModel(IMessageSender sender, CustomMotionList customMotionList)
        {
            _sender = sender;
            _motionList = customMotionList;
            var setting = GameInputSetting.LoadDefault();
            
            GamepadKeyAssign = setting.GamepadKeyAssign;
            KeyboardKeyAssign = setting.KeyboardKeyAssign;

            GamepadEnabled = new RProperty<bool>(setting.GamepadEnabled, v => SendMessage(MessageFactory.UseGamepadForGameInput(v)));
            KeyboardEnabled = new RProperty<bool>(setting.KeyboardEnabled, v => SendMessage(MessageFactory.UseKeyboardForGameInput(v)));
            AlwaysRun = new RProperty<bool>(setting.AlwaysRun, v => SendMessage(MessageFactory.EnableAlwaysRunGameInput(v)));

            LocomotionStyle = new RProperty<GameInputLocomotionStyle>(
                GetLocomotionStyle(setting.LocomotionStyleValue),
                v => SendMessage(MessageFactory.SetGameInputLocomotionStyle((int)v))
            );

            UseMouseToLookAround = new RProperty<bool>(KeyboardKeyAssign.UseMouseLookAround, v =>
            {
                KeyboardKeyAssign.UseMouseLookAround = v;
                SendMessage(MessageFactory.UseMouseMoveForLookAroundGameInput(v));
            });

            UseWasdMove = new RProperty<bool>(KeyboardKeyAssign.UseWasdMove, v =>
            {
                KeyboardKeyAssign.UseWasdMove = v;
                SendMessage(MessageFactory.EnableWasdMoveGameInput(v));
            });

            UseArrowKeyMove = new RProperty<bool>(KeyboardKeyAssign.UseArrowKeyMove, v =>
            {
                KeyboardKeyAssign.UseArrowKeyMove = v;
                SendMessage(MessageFactory.EnableArrowKeyMoveGameInput(v));
            });

            UseShiftRun = new RProperty<bool>(KeyboardKeyAssign.UseShiftRun, v =>
            {
                KeyboardKeyAssign.UseShiftRun = v;
                SendMessage(MessageFactory.UseShiftRunGameInput(v));
            });

            UseSpaceJump = new RProperty<bool>(KeyboardKeyAssign.UseSpaceJump, v =>
            {
                KeyboardKeyAssign.UseSpaceJump = v;
                SendMessage(MessageFactory.UseSpaceJumpGameInput(v));
            });
        }

        private readonly IMessageSender _sender;
        private readonly CustomMotionList _motionList;
        private GameInputActionKey[]? _customActionKeys = null;
        
        public event EventHandler<GamepadKeyAssignUpdateEventArgs>? GamepadKeyAssignUpdated;
        public event EventHandler<KeyboardKeyAssignUpdateEventArgs>? KeyboardKeyAssignUpdated;

        public GameInputGamepadKeyAssign GamepadKeyAssign { get; private set; }
        public GameInputKeyboardKeyAssign KeyboardKeyAssign { get; private set; }

        public RProperty<bool> GamepadEnabled { get; }
        public RProperty<bool> KeyboardEnabled { get; }
        public RProperty<bool> AlwaysRun { get; }
        public RProperty<GameInputLocomotionStyle> LocomotionStyle { get; }
        
        public RProperty<bool> UseMouseToLookAround { get; }
        public RProperty<bool> UseWasdMove { get; }
        public RProperty<bool> UseArrowKeyMove { get; }
        public RProperty<bool> UseShiftRun { get; }
        public RProperty<bool> UseSpaceJump { get; }

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
            //NOTE: 起動直後にアプリケーションが終了する場合ここを通る
            //たぶん問題ないはずだが、カスタムモーションの登録状況に責任を持ちにくいので止めておく感じにしている
            if (!_motionList.IsInitialized)
            {
                return;
            }

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

        public async void InitializeAsync()
        {
            LoadSetting(SpecialFilePath.GameInputDefaultFilePath);

            // 設定ファイルの読み込みより後でカスタムモーションの一覧が来るとカスタムモーション一覧の整合性チェックをしてない状態が作れてしまうので、
            // 明示的に待機 + チェックをやっておく
            await _motionList.WaitCustomMotionInitializeAsync();
            CheckCustomActionKeys();
        }

        public void SaveSettingToDefaultFile() => SaveSetting(SpecialFilePath.GameInputDefaultFilePath);

        public void SetGamepadButtonAction(GameInputGamepadButton button, GameInputActionKey actionKey)
        {
            var currentKey = button switch
            {
                GameInputGamepadButton.A => GamepadKeyAssign.ButtonAKey,
                GameInputGamepadButton.B => GamepadKeyAssign.ButtonBKey,
                GameInputGamepadButton.X => GamepadKeyAssign.ButtonXKey,
                GameInputGamepadButton.Y => GamepadKeyAssign.ButtonYKey,
                GameInputGamepadButton.LB => GamepadKeyAssign.ButtonLButtonKey,
                GameInputGamepadButton.RB => GamepadKeyAssign.ButtonRButtonKey,
                GameInputGamepadButton.LTrigger => GamepadKeyAssign.ButtonLTriggerKey,
                GameInputGamepadButton.RTrigger => GamepadKeyAssign.ButtonRTriggerKey,
                GameInputGamepadButton.View => GamepadKeyAssign.ButtonViewKey,
                GameInputGamepadButton.Menu => GamepadKeyAssign.ButtonMenuKey,
                _ => GameInputActionKey.Empty,
            };

            if (actionKey.Equals(currentKey))
            {
                return;
            }

            switch(button)
            {
                case GameInputGamepadButton.A:
                    GamepadKeyAssign.ButtonA = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonA = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.B:
                    GamepadKeyAssign.ButtonB = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonB = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.X:
                    GamepadKeyAssign.ButtonX = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonX = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.Y:
                    GamepadKeyAssign.ButtonY = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonY = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.LB:
                    GamepadKeyAssign.ButtonLButton = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonLButton = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.RB:
                    GamepadKeyAssign.ButtonRButton = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonRButton = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.LTrigger:
                    GamepadKeyAssign.ButtonLTrigger = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonLTrigger = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.RTrigger:
                    GamepadKeyAssign.ButtonRTrigger = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonRTrigger = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.View:
                    GamepadKeyAssign.ButtonView = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonView = actionKey.CustomAction;
                    break;
                case GameInputGamepadButton.Menu:
                    GamepadKeyAssign.ButtonMenu = actionKey.ActionType;
                    GamepadKeyAssign.CustomButtonMenu = actionKey.CustomAction;
                    break;
                default:
                    return;
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

        public void SetClickAction(GameInputMouseButton button, GameInputActionKey actionKey)
        {
            var current = button switch
            {
                GameInputMouseButton.Left => KeyboardKeyAssign.LeftClickKey,
                GameInputMouseButton.Right => KeyboardKeyAssign.RightClickKey,
                GameInputMouseButton.Middle => KeyboardKeyAssign.MiddleClickKey,
                _ => GameInputActionKey.Empty,
            };

            if (actionKey.Equals(current))
            {
                return;
            }

            switch (button)
            {
                case GameInputMouseButton.Left:
                    KeyboardKeyAssign.LeftClick = actionKey.ActionType;
                    KeyboardKeyAssign.CustomLeftClick = actionKey.CustomAction;
                    break;
                case GameInputMouseButton.Right:
                    KeyboardKeyAssign.RightClick = actionKey.ActionType;
                    KeyboardKeyAssign.CustomRightClick = actionKey.CustomAction;
                    break;
                case GameInputMouseButton.Middle: 
                    KeyboardKeyAssign.MiddleClick = actionKey.ActionType;
                    KeyboardKeyAssign.CustomMiddleClick = actionKey.CustomAction;
                    break;
                default: return;
            }

            SendKeyboardKeyAssign();
            KeyboardKeyAssignUpdated?.Invoke(this, new(KeyboardKeyAssign));
        }

        public void SetKeyAction(GameInputActionKey actionKey, string key)
        {
            var current = actionKey.ActionType switch
            {
                GameInputButtonAction.Jump => KeyboardKeyAssign.JumpKeyCode,
                GameInputButtonAction.Crouch => KeyboardKeyAssign.CrouchKeyCode,
                GameInputButtonAction.Run => KeyboardKeyAssign.RunKeyCode,
                GameInputButtonAction.Trigger => KeyboardKeyAssign.TriggerKeyCode,
                GameInputButtonAction.Punch => KeyboardKeyAssign.PunchKeyCode,
                GameInputButtonAction.Custom => FindKeyCodeOfCustomAction(actionKey),
                _ => "",
            };

            if (key == current)
            {
                return;
            }

            switch (actionKey.ActionType)
            {
                case GameInputButtonAction.Jump: KeyboardKeyAssign.JumpKeyCode = key; break;
                case GameInputButtonAction.Crouch: KeyboardKeyAssign.CrouchKeyCode = key; break;
                case GameInputButtonAction.Run: KeyboardKeyAssign.RunKeyCode = key; break;
                case GameInputButtonAction.Trigger: KeyboardKeyAssign.TriggerKeyCode = key; break;
                case GameInputButtonAction.Punch: KeyboardKeyAssign.PunchKeyCode = key; break;
                case GameInputButtonAction.Custom:
                    var target = KeyboardKeyAssign
                        .CustomActions
                        .FirstOrDefault(a => a.CustomAction.CustomKey == actionKey.CustomActionKey);
                    if (target == null)
                    {
                        return;
                    }

                    target.KeyCode = key;
                    break;
                default: return;
            }

            SendKeyboardKeyAssign();
            KeyboardKeyAssignUpdated?.Invoke(this, new(KeyboardKeyAssign));
        }

        // Unity側からカスタムモーション一覧を受けとった後で呼ぶことで、
        // 現在ファイルからロードしているアクションとの整合性をチェックする。
        //
        // キーボード設定: 過不足があると、
        // - 指定した一覧にはあるのに設定として保持してない -> キーアサインなしの状態で追加
        // - 指定した一覧に入ってないものが設定に含まれる   -> 削除 
        // 
        // ゲームパッド、およびマウスクリック: 過不足があると、
        // - 存在しないカスタムアクションを指定しているボタンやクリックに対するアクションの割当がNoneにリセットされる
        private void CheckCustomActionKeys()
        {
            // Unityから一覧を受け取る前だと一致チェックできないので、修正を試みない
            if (!_motionList.IsInitialized)
            {
                return;
            }

            var actionKeys = LoadCustomActionKeys();

            var currentKeys = KeyboardKeyAssign
                .CustomActions
                .Select(a => GameInputActionKey.Custom(a.CustomAction.CustomKey))
                .ToHashSet();

            if (currentKeys.SetEquals(actionKeys))
            {
                return;
            }

            //ボタンとクリックの整合性をとる
            // - クリックはこの後のキーボード整合性チェックのあとで必ずデータが送られるので、個別に送らないでOK
            // - ゲームパッド部分は、ここで修正が入ったら明示的に送り直しておく (※実際は送らないでもUnity側で無視するはずではあるが)
            UpdateGamepadAndMouseCustomActionBasedOnActionKeys(actionKeys);

            UpdateKeyboardCustomActionBasedOnActionKeys(actionKeys);
        }

        //NOTE: この関数は
        // - 現在ロード中のキーボード設定とUnity側で認識しているカスタムモーションを見比べて、一覧が整合してない場合に呼び出すのを想定している
        // - この関数を呼び出すと
        //   - ゲームパッドのボタンに存在しないカスタムアクションがあった場合は「なし」に割当が修正され、それをUnityにも送信する
        //   - マウスクリックに対しても存在しないカスタムアクションがあったら「なし」に割当を修正するが、Unityにはデータを送信しない
        private void UpdateGamepadAndMouseCustomActionBasedOnActionKeys(GameInputActionKey[] actionKeys)
        {
            //mouse
            if (!actionKeys.Contains(KeyboardKeyAssign.LeftClickKey))
            {
                KeyboardKeyAssign.LeftClick = GameInputButtonAction.None;
                KeyboardKeyAssign.CustomLeftClick.CustomKey = "";
            }

            if (!actionKeys.Contains(KeyboardKeyAssign.RightClickKey))
            {
                KeyboardKeyAssign.RightClick = GameInputButtonAction.None;
                KeyboardKeyAssign.CustomRightClick.CustomKey = "";
            }

            if (!actionKeys.Contains(KeyboardKeyAssign.MiddleClickKey))
            {
                KeyboardKeyAssign.MiddleClick = GameInputButtonAction.None;
                KeyboardKeyAssign.CustomMiddleClick.CustomKey = "";
            }

            //gamepad
            if (GamepadKeyAssign.TryResetMissingCustomAction(actionKeys))
            {
                SendGamepadKeyAssign();
                GamepadKeyAssignUpdated?.Invoke(this, new(GamepadKeyAssign));
            }
        }

        //NOTE: この関数は
        // - 現在ロード中のキーボード設定とUnity側で認識しているカスタムモーションを見比べて、一覧が整合してない場合に呼び出すのを想定している
        // - この関数を呼び出すと、必ずキーボードの設定をUnity側へ送信し直す
        private void UpdateKeyboardCustomActionBasedOnActionKeys(GameInputActionKey[] actionKeys)
        {
            var resultCustomActions = new KeyboardKeyWithGameInputCustomAction[actionKeys.Length];
            for (var i = 0; i < resultCustomActions.Length; i++)
            {
                var key = actionKeys[i];
                resultCustomActions[i] = new KeyboardKeyWithGameInputCustomAction()
                {
                    CustomAction = new GameInputCustomAction() { CustomKey = key.CustomActionKey },
                    KeyCode = FindKeyCodeOfCustomAction(key),
                };
            }
            KeyboardKeyAssign.CustomActions = resultCustomActions.ToArray();

            SendKeyboardKeyAssign();
            KeyboardKeyAssignUpdated?.Invoke(this, new(KeyboardKeyAssign));
        }

        public void ResetToDefault() => ApplySetting(GameInputSetting.LoadDefault());

        /// <summary>
        /// NOTE: BuiltInアクションに対しても定義できるが、直近で必要ないため使っていない
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public string FindKeyCodeOfCustomAction(GameInputActionKey action)
        {
            var item = KeyboardKeyAssign.CustomActions
                .FirstOrDefault(a => a.CustomActionKey.Equals(action));
                
            if (item != null)
            {
                return item.KeyCode;
            }
            else
            {
                return "";
            }
        }

        public GameInputActionKey[] LoadCustomActionKeys()
        {
            if (!_motionList.IsInitialized)
            {
                return Array.Empty<GameInputActionKey>();
            }

            if (_customActionKeys == null)
            {
                _customActionKeys = _motionList.VrmaCustomMotionClipNames
                    .Select(GameInputActionKey.Custom)
                    .ToArray();
            }
            return _customActionKeys;
        }

        //NOTE:
        // 起動直後でcustomMotionListが空の状態で呼ぶと .vrma を含まない結果が戻ってしまうが、
        // この問題は特にケアしない(ゲーム入力の設定ウィンドウが開くまでは呼ばれないはずなので)
        public GameInputActionKey[] GetAvailableActionKeys()
        {
            var result = new List<GameInputActionKey>()
            {
                GameInputActionKey.BuiltIn(GameInputButtonAction.None),
                GameInputActionKey.BuiltIn(GameInputButtonAction.Jump),
                GameInputActionKey.BuiltIn(GameInputButtonAction.Crouch),
                GameInputActionKey.BuiltIn(GameInputButtonAction.Run),
                GameInputActionKey.BuiltIn(GameInputButtonAction.Trigger),
                GameInputActionKey.BuiltIn(GameInputButtonAction.Punch),
            };

            result.AddRange(LoadCustomActionKeys());
            return result.ToArray();
        }


        private GameInputSetting BuildCurrentSetting()
        {
            //NOTE: KeyboardKeyAssignの値は逐一RP<T>と同期するので再書き込みは不要
            return new GameInputSetting()
            {
                GamepadEnabled = GamepadEnabled.Value,
                KeyboardEnabled = KeyboardEnabled.Value,
                LocomotionStyleValue = (int)LocomotionStyle.Value,
                AlwaysRun = AlwaysRun.Value,
                KeyboardKeyAssign = KeyboardKeyAssign,
                GamepadKeyAssign = GamepadKeyAssign,
            };
        }

        private void ApplySetting(GameInputSetting setting)
        {
            GamepadEnabled.Value = setting.GamepadEnabled;
            KeyboardEnabled.Value = setting.KeyboardEnabled;
            LocomotionStyle.Value = GetLocomotionStyle(setting.LocomotionStyleValue);
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

            CheckCustomActionKeys();
        }

        private void SendGamepadKeyAssign()
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            using var jsonWriter = new JsonTextWriter(writer);

            serializer.Serialize(jsonWriter, GamepadKeyAssign);
            var json = sb.ToString();
            SendMessage(MessageFactory.SetGamepadGameInputKeyAssign(json));
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
            SendMessage(MessageFactory.SetKeyboardGameInputKeyAssign(json));
        }

        private GameInputLocomotionStyle GetLocomotionStyle(int value)
        {
            //NOTE: 未来のバージョンから知らん値が降ってきたとき用のガード
            return value >= 0 && value <= (int)GameInputLocomotionStyle.SideView2D
                ? (GameInputLocomotionStyle)value
                : GameInputLocomotionStyle.FirstPerson;
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
