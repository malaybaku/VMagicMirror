namespace VMagicMirror.Buddy
{
    /// <summary> 音声の再生に関連するAPIです。 </summary>
    public interface IAudio
    {
        /// <summary>
        /// ファイルパスを指定して音声を再生します。
        /// </summary>
        /// <param name="path">音声データのファイルパス</param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        void Play(string path, float volume = 1.0f, float pitch = 1.0f);

        // NOTE: mp3をサポートするような実装が手元にないので一旦無し。
        // /// <summary>
        // /// バイナリを指定して音声を再生します。音声データはwavファイルかmp3ファイル相当のバイナリであることが必要です。
        // /// </summary>
        // /// <param name="data"></param>
        // void Play(byte[] data);
    }
}
