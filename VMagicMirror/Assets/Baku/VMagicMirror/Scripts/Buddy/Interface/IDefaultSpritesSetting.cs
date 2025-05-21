namespace VMagicMirror.Buddy
{
    /// <summary>
    /// まばたき、口パクの画像差分を含むスプライト画像で構成されたデフォルト立ち絵を操作するための設定を定義します。
    /// </summary>
    public interface IDefaultSpritesSetting
    {
        /// <summary>
        /// メインアバターと無関係にまばたきを行う場合のまばたきの最小間隔を秒単位で取得、設定します。
        /// 既定値は <c>10</c> です。
        /// </summary>
        float BlinkIntervalMin { get; set; }
        /// <summary>
        /// メインアバターと無関係にまばたきを行う場合のまばたきの最長間隔を秒単位で取得、設定します。
        /// 既定値は <c>20</c> です。
        /// </summary>
        float BlinkIntervalMax { get; set; }

        /// <summary>
        /// メインアバターの瞬きを考慮してサブキャラのまばたきを制御するかどうかを取得、設定します。
        /// 既定値は <c>true</c> です。
        /// </summary>
        /// <remarks>
        /// このフラグが有効な場合、メインアバターの瞬きに合わせてサブキャラが適当なディレイでまばたき動作を行います。
        /// 
        /// このフラグはメインアバターの状態を参照して動作します。
        /// <see cref="IRootApi.AvatarOutputFeatureEnabled"/> が <c>false</c> の場合、この値を設定していてもメインアバターとの同期は行われません。
        /// </remarks>
        bool SyncBlinkBlendShapeToMainAvatar { get; set; }
        
        /// <summary>
        /// メインアバターのリップシンクを考慮してサブキャラのまばたき、口パク動作を制御するかどうかを取得、設定します。
        /// 既定値は <c>true</c> です。
        /// </summary>
        /// <remarks>
        /// このフラグが有効な場合、メインアバターのリップシンクに対してサブキャラのデフォルト立ち絵をある程度動作させます。
        /// 
        /// このフラグはメインアバターの状態を参照して動作します。
        /// <see cref="IRootApi.AvatarOutputFeatureEnabled"/> が <c>false</c> の場合、この値を設定していてもメインアバターとの同期は行われません。
        /// </remarks>
        bool SyncMouthBlendShapeToMainAvatar { get; set; }
    }
}
