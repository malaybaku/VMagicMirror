using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// マウスボタン/キー入力を、あたかもプロセス間で受け取った値であるかのようにメッセージハンドラへリダイレクトする
    /// </summary>
    public class InputAsMessage : MonoBehaviour
    {
        [SerializeField] private ReceivedMessageHandler handler = null;
        [SerializeField] private InputChecker inputChecker = null;
        
        private void Update()
        {
            while (inputChecker.PressedKeys.TryDequeue(out string key))
            {
                handler.ReceiveCommand(new ReceivedCommand(MessageCommandNames.KeyDown, key));
            }

            while (inputChecker.MouseButtonEvents.TryDequeue(out string info))
            {
                handler.ReceiveCommand(new ReceivedCommand(MessageCommandNames.MouseButton, info));
            }
        }
    }
}
