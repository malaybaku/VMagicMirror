using System;

namespace Baku.VMagicMirrorConfig
{
    public readonly struct GameInputActionKey : IEquatable<GameInputActionKey>
    {

        public GameInputActionKey(GameInputButtonAction actionType, GameInputCustomAction customAction) 
        {
            ActionType = actionType;
            CustomActionKey = actionType is GameInputButtonAction.Custom ? customAction.CustomKey : "";
        }

        private GameInputActionKey(GameInputButtonAction actionType, string key)
        {
            ActionType = actionType;
            CustomActionKey = key;
        }

        //NOTE: データの期待としてCustomじゃないActionに対するCustomAction.CustomKeyは必ず空…というのを想定している
        public GameInputButtonAction ActionType { get; }
        public string CustomActionKey { get; }
        public GameInputCustomAction CustomAction => new() { CustomKey = CustomActionKey };


        public bool Equals(GameInputActionKey other)
        {
            return 
                ActionType == other.ActionType &&
                CustomActionKey == other.CustomActionKey;
        }

        public override bool Equals(object? obj) => obj is GameInputActionKey other && Equals(other);
        
        public override int GetHashCode() => HashCode.Combine(ActionType, CustomActionKey);

        public override string ToString() => $"ActionKey:{ActionType}{(ActionType is GameInputButtonAction.Custom ? "-" + CustomActionKey : "")}";

        public static GameInputActionKey BuiltIn(GameInputButtonAction action)
            => new(action, new GameInputCustomAction());
        public static GameInputActionKey Custom(string key)
            => new(GameInputButtonAction.Custom, key);

        public static GameInputActionKey Empty { get; } = new(GameInputButtonAction.None, "");
    }
}
