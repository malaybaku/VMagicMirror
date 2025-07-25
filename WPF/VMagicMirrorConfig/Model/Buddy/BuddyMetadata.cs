﻿using System;
using System.IO;

namespace Baku.VMagicMirrorConfig
{
    // Buddyフォルダの内容から定まる、基本的にBuddyの制作者が定めるサブキャラの情報
    public class BuddyMetadata
    {
        public BuddyMetadata(
            bool isDefaultBuddy,
            string folderPath,
            string id, 
            BuddyLocalizedText displayName, 
            string creator,
            string creatorUrl, 
            string version, 
            BuddyPropertyMetadata[] properties
            )
        {
            IsDefaultBuddy = isDefaultBuddy;
            FolderPath = folderPath;
            FolderName = Path.GetFileName(folderPath);
            DisplayName = displayName;
            Id = id;
            Creator = creator;
            CreatorUrl = creatorUrl;
            Version = version;
            Properties = properties;

            // NOTE: このprefixのつけかたはUnity側も把握していて、prefixの有無でデフォルトサブキャラかどうかが判定される
            BuddyId = BuddyId.Create(FolderName, isDefaultBuddy);
        }

        public bool IsDefaultBuddy { get; }

        // フォルダだけファイル構造から定まり、かつアプリ上で一意識別子に使おうとする点が特殊
        public string FolderPath { get; }
        public string FolderName { get; }
        public BuddyId BuddyId { get; }

        public BuddyLocalizedText DisplayName { get; }

        // NOTE: manifest.json上のIdはBuddy間で通信したくなったときに備えた仕様でBuddyIdとは別の概念。
        // BuddyIdは実行時に一意だが、Idはv4.0.0の時点では一意なことは保証されない。
        // WPFでは基本的に無視する
        public string Id { get; } = "";
        public string Creator { get; } = "";
        public string CreatorUrl { get; } = "";
        public string Version { get; } = "";

        public BuddyPropertyMetadata[] Properties { get; } = Array.Empty<BuddyPropertyMetadata>();
    }
}
