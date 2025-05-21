using System;

namespace VMagicMirror.Buddy
{
    /// <summary> 音声の再生に関連するAPIです。 </summary>
    public interface IAudio
    {
        /// <summary>
        /// ファイルパスを指定して音声を再生します。
        /// </summary>
        /// <param name="path">音声データのファイルパス</param>
        /// <param name="volume">音声のボリュームを 0 以上、1 以下の値で指定します。指定しない場合は <c>1.0f</c> であるものとして扱われます。</param>
        /// <param name="pitch">音声のピッチを、1を基準として指定します。指定しない場合は <c>1.0f</c> になります。</param>
        /// <param name="key"><see cref="AudioStarted"/> で実際に音声再生が開始したことを検出するときの一意識別子が必要な場合、その文字列を指定します。指定しない場合は空文字列になります。</param>
        /// <remarks>
        /// この関数を呼び出してから音声再生が開始するまでにはディレイが発生する場合があります。
        /// 実際に音声の再生が開始したことは <see cref="AudioStarted"/> イベントで確認できます。
        /// </remarks>
        void Play(string path, float volume = 1.0f, float pitch = 1.0f, string key = "");

        // NOTE: mp3をサポートするような実装が手元にないので一旦無し。
        // /// <summary>
        // /// バイナリを指定して音声を再生します。音声データはwavファイルかmp3ファイル相当のバイナリであることが必要です。
        // /// </summary>
        // /// <param name="data"></param>
        // void Play(byte[] data);
        
        /// <summary>
        /// 音声の再生を停止します。
        /// </summary>
        /// <param name="key">
        /// 特定の音声の再生を停止する場合、<see cref="Play"/>で指定したのと同じ値を指定します。
        /// このサブキャラが再生している全ての音声を停止する場合、空文字列を指定します。
        /// デフォルトでは空文字列が指定されたものとして扱われます。
        /// </param>
        /// <remarks>
        /// この関数は <see cref="Play"/> を呼び出していない場合やすでに音声が再生終了している場合にも実行できますが、その場合は何も起こりません。
        /// </remarks>
        void Stop(string key = "");

        /// <summary> <see cref="Play"/> で指定して音声の再生を開始するときに発火します。 </summary>
        event Action<AudioStartedInfo> AudioStarted;
        
        /// <summary> <see cref="Play"/> で指定した音声の再生が停止するときに発火します。 </summary>
        event Action<AudioStoppedInfo> AudioStopped;
    }

    /// <summary>
    /// <see cref="IAudio.AudioStarted"/> イベントに付随する情報です。
    /// </summary>
    public readonly struct AudioStartedInfo
    {
        public AudioStartedInfo(string key, float length)
        {
            Key = key;
            Length = length;
        }
    
        /// <summary> <see cref="IAudio.Play"/>で指定した <c>key</c> の値を取得します。 </summary>
        public string Key { get; }
        
        /// <summary> 音声の再生時間を秒単位で取得します。 </summary>
        /// <remarks>
        /// この値は <see cref="IAudio.Play"/> で指定した <c>pitch</c> を考慮しない値です。
        /// </remarks>
        public float Length { get; }
    }
    
    /// <summary>
    /// <see cref="IAudio.Play"/> で再生した音声が停止した理由を表す値です。
    /// </summary>
    public enum AudioStoppedReason
    {
        /// <summary> 不明な理由 </summary>
        Unknown,
        /// <summary> 音声の終端まで再生を完了した </summary>
        Completed,
        /// <summary> <see cref="IAudio.Stop"/> によって再生を停止した </summary>
        Stopped,
        /// <summary> 他の音源の再生によって、再生が中断された </summary>
        Interrupted,
    }
    
    /// <summary>
    /// <see cref="IAudio.AudioStopped"/> イベントに付随する情報です。
    /// </summary>
    public readonly struct AudioStoppedInfo
    {
        public AudioStoppedInfo(string key, AudioStoppedReason reason)
        {
            Key = key;
            Reason = reason;
        }
        
        /// <summary> <see cref="IAudio.Play"/>で指定した <c>key</c> の値を取得します。 </summary>
        public string Key { get; }
    
        /// <summary> 音声が停止した理由を取得します。 </summary>
        public AudioStoppedReason Reason { get; }
    }
}
