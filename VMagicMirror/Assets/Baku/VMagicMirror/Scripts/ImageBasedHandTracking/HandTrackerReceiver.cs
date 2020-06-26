using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像ベースのハンドトラッキング処理まわりのレシーバークラス
    /// </summary>
    public class HandTrackerReceiver : MonoBehaviour
    {
        [Inject]
        public void Initialize(ReceivedMessageHandler handler) => _handler = handler;
        private ReceivedMessageHandler _handler;

        [SerializeField] private HandTracker handTracker = null;
        
        private void Start()
        {
            _handler.Commands.Subscribe(c =>
            {
                switch (c.Command)
                {
                    case MessageCommandNames.EnableImageBasedHandTracking:
                        handTracker.ImageProcessEnabled = c.ToBoolean();
                        break;
                }
            });

        }
    }
}
