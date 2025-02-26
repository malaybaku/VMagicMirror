namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface IGlbApi : IObject3DApi
    {
        // TODO: LoadAsyncとかがよい？
        // 実装都合でタスク化するのも検討していいが、
        void Load(string path);
        void Show();

        // NOTE: 遷移方法を指定する感じの引数も欲しくなりそう
        string[] GetAnimationNames();

        void RunAnimation(string name);
        void StopAnimation();

        //TODO: エフェクト (サイズキープしてぷにぷにするとか) も欲しい
    }
}
