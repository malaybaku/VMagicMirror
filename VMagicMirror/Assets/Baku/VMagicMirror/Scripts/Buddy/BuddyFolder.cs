namespace Baku.VMagicMirror.Buddy
{
    public readonly struct BuddyFolder
    {
        /// <summary>
        /// ユーザー定義サブキャラならフォルダ名、デフォルトサブキャラなら ">" + フォルダ名として定義されたID文字列。
        /// NOTE: ログ情報をIPCで送るときに例外的に使っている。それ以外では、BuddyFolderの下位情報としてのIdを参照するのは避けるべき
        /// </summary>
        public BuddyId BuddyId { get; }

        /// <summary> フォルダ名を小文字にした文字列 </summary>
        public string FolderName { get; }
        
        /// <summary> デフォルトサブキャラのフォルダならtrue </summary>
        public bool IsDefaultBuddy { get; }
        
        public BuddyFolder(BuddyId buddyId, string folderName, bool isDefaultBuddy)
        {
            BuddyId = buddyId;
            FolderName = folderName;
            IsDefaultBuddy = isDefaultBuddy;
        }

        public static BuddyFolder Create(BuddyId buddyId)
        {
            var isDefaultBuddy = buddyId.Value[0] == '>';
            var folderName = isDefaultBuddy ? buddyId.Value[1..] : buddyId.Value;
            
            // NOTE: equalityとかでめんどくさくならないように、folderNameのほうも小文字に寄せてしまう
            return new BuddyFolder(buddyId, folderName.ToLower(), isDefaultBuddy);
        }
    }
}
