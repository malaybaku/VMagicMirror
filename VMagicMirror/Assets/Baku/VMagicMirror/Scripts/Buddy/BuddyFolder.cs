namespace Baku.VMagicMirror.Buddy
{
    public readonly struct BuddyFolder
    {
        /// <summary>
        /// ユーザー定義サブキャラならフォルダ名、デフォルトサブキャラなら ">" + フォルダ名として定義されたID文字列。
        /// NOTE: ログ情報をIPCで送るときに例外的に使っている。それ以外では、BuddyFolderの下位情報としてのIdを参照するのは避けるべき
        /// </summary>
        public string BuddyId { get; }

        /// <summary> フォルダ名を小文字にした文字列 </summary>
        public string FolderName { get; }
        
        /// <summary> デフォルトサブキャラのフォルダならtrue </summary>
        public bool IsDefaultBuddy { get; }
        
        public BuddyFolder(string buddyId, string folderName, bool isDefaultBuddy)
        {
            BuddyId = buddyId;
            FolderName = folderName;
            IsDefaultBuddy = isDefaultBuddy;
        }

        public static BuddyFolder Create(string buddyId)
        {
            var isDefaultBuddy = buddyId[0] == '>';
            var folderName = isDefaultBuddy ? buddyId[1..] : buddyId;
            
            // NOTE: equalityとかでめんどくさくならないように小文字にしてしまう
            return new BuddyFolder(buddyId.ToLower(), folderName.ToLower(), isDefaultBuddy);
        }
    }
}
