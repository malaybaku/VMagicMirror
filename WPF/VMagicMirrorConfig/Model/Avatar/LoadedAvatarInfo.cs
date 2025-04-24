using Baku.VMagicMirror;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// ロードされたアバターの情報のうちGUIの表示に影響があるものを保持する
    /// </summary>
    internal class LoadedAvatarInfo
    {
        public LoadedAvatarInfo() : this(ModelResolver.Instance.Resolve<IMessageReceiver>())
        {
        }

        public LoadedAvatarInfo(IMessageReceiver receiver)
        {
            receiver.ReceivedCommand += OnReceiveCommand;
        }

        private void OnReceiveCommand(CommandReceivedData e)
        {
            if (e.Command is VmmServerCommands.SetModelDoesNotSupportPen)
            {
                ModelDoesNotSupportPen.Value = e.ToBool();
            }
        } 

        public RProperty<bool> ModelDoesNotSupportPen { get; } = new RProperty<bool>(false);



        //TODO: パーフェクトシンク適正についても、このモデルで扱うのが設計上良さそう
    }
}
