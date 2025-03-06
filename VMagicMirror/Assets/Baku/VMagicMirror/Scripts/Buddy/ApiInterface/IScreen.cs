namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// アバターを表示しているウィンドウの状態に関するAPIです。
    /// </summary>
    public interface IScreen
    {
        /// <summary>
        /// ウィンドウの横幅をピクセル単位で取得します。
        /// </summary>
        /// <remarks>
        /// この値はモニターのDPIや解像度によって変化します。
        /// <see cref="Height"/>と合わせて、アスペクト比の確認などに使用できます。
        /// </remarks>
        int Width { get; }

        /// <summary>
        /// ウィンドウの縦幅をピクセル単位で取得します。
        /// </summary>
        /// <remarks>
        /// この値はモニターのDPIや解像度によって変化します。
        /// <see cref="Width"/>と合わせて、アスペクト比の確認などに使用できます。
        /// </remarks>
        int Height { get; }

        /// <summary>
        /// ウィンドウが透過表示の状態になっていれば <c>true</c>、そうでなければ <c>false</c> を取得します。
        /// </summary>
        bool IsTransparent { get; }
    }
}