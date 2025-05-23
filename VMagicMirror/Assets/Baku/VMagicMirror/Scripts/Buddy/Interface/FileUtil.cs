using System.Text;

namespace VMagicMirror.Buddy.IO
{
    /// <summary>
    /// ファイルに関連したAPIを提供します。
    /// </summary>
    /// <remarks>
    /// <para>
    /// このクラスではファイルの存在判定や読み取り専用の処理など、比較的安全に実行できるAPIを <see cref="System.IO"/> のAPIと等価に提供します。
    /// VMagicMirrorではスクリプトを安全に実行しやすくするため、<see cref="System.IO"/> 系のAPIを直接使うことは制限されています。
    /// 各メソッドの説明については <see cref="System.IO.File"/> の同名のメソッドを参照して下さい。
    /// </para>
    ///
    /// <para>
    /// サブキャラのスクリプトで本クラスによってファイルパスを扱う場合、原則として絶対パスを使用して下さい。
    /// スクリプトのカレントディレクトリが <c>main.csx</c> のあるフォルダと一致することは保証されていません。
    /// </para>
    /// </remarks>
    public static class File
    {
        public static bool Exists(string path) => System.IO.File.Exists(path);
        
        public static string ReadAllText(string path) => System.IO.File.ReadAllText(path);
        public static string ReadAllText(string path, Encoding encoding) 
            => System.IO.File.ReadAllText(path, encoding);

        public static string[] ReadAllLines(string path) => System.IO.File.ReadAllLines(path);
        public static string[] ReadAllLines(string path, Encoding encoding) 
            => System.IO.File.ReadAllLines(path, encoding);

        public static byte[] ReadAllBytes(string path) => System.IO.File.ReadAllBytes(path);
    }

    /// <summary>
    /// ディレクトリに関連したAPIを提供します。
    /// </summary>
    /// <remarks>
    /// <para>
    /// VMagicMirrorではスクリプトを安全に実行しやすくするため、<see cref="System.IO"/> 系のAPIを直接使うことは制限されています。
    /// このクラスではディレクトリの存在判定など、比較的安全に実行できるAPIを、 <see cref="System.IO"/> のAPIと等価に提供します。
    /// 各メソッドの説明については <see cref="System.IO.Directory"/> の同名のメソッドを参照して下さい。
    /// </para>
    ///
    /// <para>
    /// サブキャラのスクリプトで本クラスによってファイルパスを扱う場合、原則として絶対パスを使用して下さい。
    /// スクリプトのカレントディレクトリが <c>main.csx</c> のあるフォルダと一致することは保証されていません。
    /// </para>
    /// </remarks>
    public static class Directory
    {
        public static bool Exists(string path) => System.IO.Directory.Exists(path);
        
        public static string[] GetFiles(string path) => System.IO.Directory.GetFiles(path);
        public static string[] GetFiles(string path, string searchPattern) 
            => System.IO.Directory.GetFiles(path, searchPattern);

        public static string[] GetDirectories(string path) => System.IO.Directory.GetDirectories(path);
        public static string[] GetDirectories(string path, string searchPattern)
            => System.IO.Directory.GetDirectories(path, searchPattern);
    }

    /// <summary>
    /// ファイルパスに関連したAPIを提供します。
    /// </summary>
    /// <remarks>
    /// <para>
    /// VMagicMirrorではスクリプトを安全に実行しやすくするため、<see cref="System.IO"/> 系のAPIを直接使うことは制限されています。
    /// このクラスではPathの比較的安全に実行できるAPIを、 <see cref="System.IO"/> のAPIと等価に提供します。
    /// 各メソッドの説明については <see cref="System.IO.Directory"/> の同名のメソッドを参照して下さい。
    /// </para>
    ///
    /// <para>
    /// サブキャラのスクリプトで本クラスによってファイルパスを扱う場合、原則として絶対パスを使用して下さい。
    /// スクリプトのカレントディレクトリが <c>main.csx</c> のあるフォルダと一致することは保証されていません。
    /// </para>
    /// </remarks>
    public static class Path
    {
        public static string Combine(string path1, string path2)
            => System.IO.Path.Combine(path1, path2);
        public static string Combine(string path1, string path2, string path3)
            => System.IO.Path.Combine(path1, path2, path3);
        public static string Combine(string path1, string path2, string path3, string path4)
            => System.IO.Path.Combine(path1, path2, path3, path4);
        public static string Combine(params string[] paths)
            => System.IO.Path.Combine(paths);

        public static string GetFullPath(string path)
            => System.IO.Path.GetFullPath(path);
        public static string GetDirectoryName(string path)
            => System.IO.Path.GetDirectoryName(path);
        public static string GetFileName(string path)
            => System.IO.Path.GetFileName(path);
        public static string GetFileNameWithoutExtension(string path)
            => System.IO.Path.GetFileNameWithoutExtension(path);
        public static string GetExtension(string path)
            => System.IO.Path.GetExtension(path);
        
        
    }
}
