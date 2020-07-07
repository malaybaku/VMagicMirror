using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像ベースのハンドトラッキング処理まわりのレシーバークラス
    /// </summary>
    public class HandTrackerReceiver : MonoBehaviour
    {
        //TODO: HandTracker側にこれ生やしてコンストラクタインジェクションにしたいな～
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableImageBasedHandTracking,
                c => handTracker.ImageProcessEnabled = c.ToBoolean()
            );
        }

        [SerializeField] private HandTracker handTracker = null;
    }
}
