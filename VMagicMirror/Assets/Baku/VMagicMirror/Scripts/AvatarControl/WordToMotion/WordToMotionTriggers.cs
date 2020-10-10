using System;
using System.Linq;
using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>  </summary>
    public class WordToMotionTriggers : MonoBehaviour
    {
        [Tooltip("ボタン押下イベントが押されたらしばらくイベント送出をストップするクールダウンタイム")]
        [SerializeField] private float cooldownTime = 0.3f;

        [Tooltip("この時間だけキー入力が無かったらワードが途切れたものとして入力履歴をクリアする。")]
        [SerializeField] private float forgetTime = 1.0f;

        [Inject]
        public void Initialize(
            IMessageReceiver receiver, IMessageSender sender, 
            IKeyMouseEventSource keyMouseEventSource,
            XInputGamePad gamepadListener,
            MidiInputObserver midiObserver
            )
        {
            _keyboard =new KeyboardToWordToMotion(keyMouseEventSource);
            _midi = new MidiToWordToMotion(receiver, sender, midiObserver);
            _gamepad = new GamepadToWordToMotion(gamepadListener);
            _wordAnalyzer = new WordAnalyzer();

            _keyboard.RequestExecuteWordToMotionItem += v =>
            {
                if (UseKeyboardInput && _coolDownCount <= 0)
                {
                    _coolDownCount = cooldownTime;
                    RequestExecuteWordToMotionItem?.Invoke(v);
                }
            };

            _midi.RequestExecuteWordToMotionItem += v =>
            {
                if (UseMidiInput && _coolDownCount <= 0)
                {
                    _coolDownCount = cooldownTime;
                    RequestExecuteWordToMotionItem?.Invoke(v);
                }
            };
            
            _gamepad.RequestExecuteWordToMotionItem += v =>
            {
                if (UseGamepadInput && _coolDownCount <= 0)
                {
                    _coolDownCount = cooldownTime;
                    RequestExecuteWordToMotionItem?.Invoke(v);
                }
            };
            
            _wordAnalyzer.WordDetected += word =>
            {
                if (UseKeyboardWordTypingForWordToMotion)
                {
                    _coolDownCount = cooldownTime;
                    RequestExecuteWord?.Invoke(word);
                }
            };

            keyMouseEventSource.PressedRawKeys.Subscribe(keyName =>
            {
                if (UseKeyboardWordTypingForWordToMotion)
                {
                    _count = forgetTime;
                    _wordAnalyzer.Add(KeyName2Char(keyName));
                }
            });
            
        }
        
        /// <summary>Word to Motionの要素をインデックス指定で実行してほしいときに発火する。</summary>
        public event Action<int> RequestExecuteWordToMotionItem;

        /// <summary>Word to Motionの要素を名前指定で実行してほしいときに発火する。</summary>
        public event Action<string> RequestExecuteWord;
        
        public bool UseKeyboardInput { get; set; } = false;
        public bool UseMidiInput { get; set; } = false;
        public bool UseGamepadInput { get; set; } = false;
        public bool UseKeyboardWordTypingForWordToMotion { get; set; } = true;

        
        private KeyboardToWordToMotion _keyboard = null;
        private MidiToWordToMotion _midi = null;
        private GamepadToWordToMotion _gamepad = null;
        private WordAnalyzer _wordAnalyzer = null;
        
        private float _coolDownCount = 0;
        private float _count = 0;
        
        
        public void LoadItems(MotionRequestCollection motionRequests)
        {
            _wordAnalyzer.LoadWordSet(
                motionRequests.Requests.Select(r => r.Word).ToArray()
            );
        }

        private void Update()
        {
            if (_coolDownCount > 0)
            {
                _coolDownCount -= Time.deltaTime;
            }
            
            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _count = forgetTime;
                _wordAnalyzer.Clear();
            }
        }
        
        private static char KeyName2Char(string keyName)
        {
            if (keyName.Length == 1)
            {
                //a-z
                return keyName.ToLower()[0];
            }
            else if (keyName.Length == 2 && keyName[0] == 'D' && char.IsDigit(keyName[1]))
            {
                //D0 ~ D9 (テンキーじゃないほうの0~9)
                return keyName[1];
            }
            else if (keyName.Length == 7 && keyName.StartsWith("NumPad") && char.IsDigit(keyName[6]))
            {
                //NumPad0 ~ NumPad9 (テンキーの0~9)
                return keyName[6];
            }
            else
            {
                //TEMP: 「ヘンな文字でワードが途切れた」という情報だけ残す
                return ' ';
            }
        }        
    }
}
