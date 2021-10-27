namespace Baku.VMagicMirror
{
    public abstract class VmmLipSyncContextBase : OVRLipSyncContextBase
    {
        /// <summary>
        /// [dB]単位で感度を受け取る
        /// </summary>
        public virtual int Sensitivity { get; set; }
        
        public abstract void StopRecording();
        public abstract void StartRecording(string microphoneName);
        
        /// <summary>
        /// 現在録音をしていれば録音に使っているデバイス名、そうでなければ空文字列を返します。
        /// </summary>
        public abstract string DeviceName { get; }

        /// <summary>
        /// 利用可能なマイク名の一覧を返します。 
        /// </summary>
        /// <returns></returns>
        public abstract string[] GetAvailableDeviceNames();
    }
}
