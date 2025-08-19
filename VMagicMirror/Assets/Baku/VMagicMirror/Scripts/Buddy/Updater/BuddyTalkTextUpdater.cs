using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// BuddyのセリフUIの状態更新のタイミングを制御するクラス。
    /// </summary>
    /// <remarks>
    /// MonoBehaviour.Updateに任せないほうが治安が良さそうなのでここで管理している
    /// </remarks>
    public class BuddyTalkTextUpdater : ITickable
    {
        private readonly BuddyRuntimeObjectRepository _repository;

        [Inject]
        public BuddyTalkTextUpdater(BuddyRuntimeObjectRepository repository)
        {
            _repository = repository;
        }

        void ITickable.Tick()
        {
            var dt = Time.deltaTime;
            foreach (var repository in _repository.GetRepositories())
            {
                foreach (var tt in repository.TalkTexts)
                {
                    tt.UpdateTextState(dt);
                }
            }
        }
    }
}
