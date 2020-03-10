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
        [Inject] private ReceivedMessageHandler _handler;

        [SerializeField] private HandTracker handTracker = null;
        
        private void Start()
        {
            _handler.Commands.Subscribe(c =>
            {
                switch (c.Command)
                {
                    case MessageCommandNames.EnableImageBaseHandTracking:
                        handTracker.ImageProcessEnabled = c.ToBoolean();
                        break;
                }
            });

        }
    }
}
