using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.GameInput
{
    //NOTE: MonoBehaviourじゃなくていいんだけど[serializeField]からいじりたいので一時的に…
    public class GameInputSourceSwitcher : MonoBehaviour, IGameInputSourceSwitcher
    {
        public enum GameInputSourceType
        {
            None,
            GamePad,
            Keyboard,
        }

        [SerializeField] private GameInputSourceType sourceType = GameInputSourceType.None;
        
        private readonly ReactiveProperty<IGameInputSource> _source
            = new ReactiveProperty<IGameInputSource>(EmptyGameInputSource.Instance);
        public IReadOnlyReactiveProperty<IGameInputSource> Source => _source;

        private GamepadGameInputSource _gamePadInput;
        private KeyboardGameInputSource _keyboardInput;
        
        
        [Inject]
        public void Initialize(IMessageReceiver receiver, GamepadGameInputSource gamePadInput, KeyboardGameInputSource keyboardInput)
        {
            _gamePadInput = gamePadInput;
            _keyboardInput = keyboardInput;
            //TODO: receiverの指示ベースでどれを使うか切り替える
        }
        
        //TODO: receiverベースで切り替えるようになったらUpdateは不要
        private void Update()
        {
            _source.Value =
                sourceType == GameInputSourceType.GamePad ? _gamePadInput :
                sourceType == GameInputSourceType.Keyboard ? (IGameInputSource) _keyboardInput :
                EmptyGameInputSource.Instance;
            
            _gamePadInput.SetActive(_source.Value == _gamePadInput);
            _keyboardInput.SetActive(_source.Value == _keyboardInput);
        }
    }
}
