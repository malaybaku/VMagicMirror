namespace VMagicMirror.Buddy
{
    /// <summary> 音声の再生に関連するAPIです。 </summary>
    public interface IAudio
    { 
        /// <summary>
        /// ファイルパスを指定して音声を再生します。
        /// </summary>
        /// <param name="path">音声データのファイルパス</param>
        void Play(string path);

        /// <summary>
        /// バイナリを指定して音声を再生します。音声データはwavファイルかmp3ファイル相当のバイナリであることが必要です。
        /// </summary>
        /// <param name="data"></param>
        void Play(byte[] data);
    }
}
