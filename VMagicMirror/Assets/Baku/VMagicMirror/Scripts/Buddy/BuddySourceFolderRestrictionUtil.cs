using System;
using System.IO;

namespace Baku.VMagicMirror.Buddy
{
    public static class BuddySourceFolderRestrictionUtil
    {
        public static bool IsNgPath(string fullPath)
        {
            var ngFilePath = Path.GetFullPath(SpecialFiles.BuddyReferenceDataGlobalScriptPath);
            if (fullPath.Equals(ngFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // NOTE: 明らかにサブキャラと関係ないフォルダを見に行こうとするのを全体的に禁止する。
            // この処置のpositiveな副作用として、デフォルトサブキャラ(VMM.exe付近のフォルダに存在)の #load は全て無視される
            if (!fullPath.StartsWith(SpecialFiles.BuddyRootDirectory))
            {
                return true;
            }
            
            // NOTEのNOTE: ひとつ手前の条件によって下記のガードが冗長になるので、省いている
            // NOTE: VMagicMirror.exeのあるフォルダ以下の #load を一律で禁止する。
            // 普通にスクリプトを書いて本処理が適用されるのはデフォルトサブキャラのみである
            // (※デフォルトサブキャラで #load が使いたくなったら書き方を考える必要がある)
            // var exeDir = Path.GetDirectoryName(Application.dataPath)?.Replace('/', '\\');
            // if (!string.IsNullOrEmpty(exeDir) && fullPath.StartsWith(exeDir, StringComparison.OrdinalIgnoreCase))
            // {
            //     return true;
            // }

            return false;
        }
    }
}
