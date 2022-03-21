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

        private void OnReceiveCommand(object? sender, CommandReceivedEventArgs e)
        {
            if (e.Command == ReceiveMessageNames.SetModelDoesNotSupportPen)
            {
                ModelDoesNotSupportPen.Value = bool.TryParse(e.Args, out var result) && result;
            }
        } 

        public RProperty<bool> ModelDoesNotSupportPen { get; } = new RProperty<bool>(false);



        //TODO: パーフェクトシンク適正についても、このモデルで扱うのが設計上良さそう
    }
}
