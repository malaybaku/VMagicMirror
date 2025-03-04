namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// 音声の再生に関連するAPIです。
    /// </summary>
    public interface IAudioApi
    { 
        /// <summary>
        /// ファイルパスを指定して音声を再生します。
        /// </summary>
        /// <param name="path">音声データのファイルパス</param>
        void Play(string path);
    }
}
