namespace Baku.VMagicMirror
{
    public interface IAccessoryFileActions
    {
        void Dispose();
        void Update(float deltaTime);
        void UpdateLayout(AccessoryItemLayout layout);
        //NOTE: isVisibleが切り替わってなくても呼ばれる事がある(現行実装では冗長に呼び出しても基本無害なので…)
        void OnVisibilityChanged(bool isVisible);

        //NOTE: 連番画像にしか意味がない
        //TODO: やっつけ感があるので何か直してほしい
        bool TryGetDuration(out float duration);
        void ResetTime();
        void ClampEndUntilHidden();
    }

    public abstract class AccessoryFileActionsBase : IAccessoryFileActions
    {
        public virtual void Dispose() { }
        public virtual void Update(float deltaTime) { }
        public virtual void UpdateLayout(AccessoryItemLayout layout) { }
        public virtual void OnVisibilityChanged(bool isVisible) { }
        public virtual bool TryGetDuration(out float duration)
        {
            duration = 0f;
            return false;
        }
        public virtual void ResetTime() { }
        public virtual void ClampEndUntilHidden() { }
    }

    /// <summary>
    /// ファイルの実態がなく、アクセサリーがロード出来なかったときにカラの実装を差し込む
    /// </summary>
    public class EmptyFileActions : AccessoryFileActionsBase
    {
    }
}