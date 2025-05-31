using Baku.VMagicMirror.Buddy.Api;

namespace Baku.VMagicMirror.Buddy
{
    public interface IScriptCaller
    {
        void Initialize();
        void Dispose();
        
        /// <summary> アプリ実行時にサブキャラを一意識別できるようなIdを取得する。 </summary>
        BuddyId BuddyId { get; }
        
        /// <summary> アプリの起動中にBuddyを一意識別できるような、Buddyを含むフォルダの情報を取得する。 </summary>
        BuddyFolder BuddyFolder { get; }

        /// <summary> エントリポイントになるスクリプトファイルの絶対パスを取得する </summary>
        string EntryScriptPath { get; }

        /// <summary> エントリポイントになるスクリプトのあるディレクトリの絶対パスを取得する。 </summary>
        string EntryScriptDirectory { get; }
        
        /// <summary>
        /// スクリプトに対してTransform操作用のAPI実装を登録する。スクリプトの実行開始前に呼び出されているのが望ましい。
        /// </summary>
        /// <param name="api"></param>
        void SetTransformsApi(ManifestTransformsApi api);
    }
}