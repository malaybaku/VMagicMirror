using System.Linq;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    // NOTE:
    // - Updaterという名前にしているが、今のところitemのqueueに関するイベントのリダイレクトのみを行う
    // - 将来的にTalkTextInstanceの位置調整とかのタイミングを(MonoBehaviour.Updateとかではなく)本クラスから制御するようにしてもよい
    // - TalkText由来のHitTestについて本クラスが何かを提供してもよい
    public class BuddyTalkTextUpdater : PresenterBase, ITickable
    {
        private readonly BuddySpriteCanvas _spriteCanvas;
        private readonly BuddyTalkTextEventBroker _broker;
        private readonly BuddyRuntimeObjectRepository _repository;

        [Inject]
        public BuddyTalkTextUpdater(
            BuddySpriteCanvas spriteCanvas,
            BuddyTalkTextEventBroker broker,
            BuddyRuntimeObjectRepository repository
        )
        {
            _spriteCanvas = spriteCanvas;
            _broker = broker;
            _repository = repository;
        }

        public override void Initialize()
        {
            // テキスト処理に関するイベントをbrokerにリダイレクトする
            _spriteCanvas.TalkTextCreated
                .Subscribe(instance =>
                {
                    // NOTE: AddTo(this)だとライフサイクルが長すぎてダメ
                    instance.ItemDequeued
                        .Subscribe(item => _broker.DequeueItem(item))
                        .AddTo(instance);
                    instance.ItemFinished
                        .Subscribe(item => _broker.FinishItem(item))
                        .AddTo(instance);
                })
                .AddTo(this);
        }

        void ITickable.Tick()
        {
            var dt = Time.deltaTime;
            foreach (var tt in _repository
                .GetRepositories()
                .SelectMany(r => r.TalkTexts))
            {
                tt.UpdateTextState(dt);
            }
        }

        /// <summary>
        /// とくにウィンドウの背景が透過しているときに呼ぶことで、セリフUIにマウスが被っているかどうかを判定する。
        /// 呼び出し元ではメインアバターへのマウスオーバーと同様、この関数がtrueを返すときはクリックスルーを無効化することが望ましい
        /// </summary>
        /// <param name="mousePosition"></param>
        /// <returns></returns>
        public bool IsPointerOnTalkTextUI(Vector2 mousePosition)
        {
            //TODO: implement
            return false;
        }
    }
}
