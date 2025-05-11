using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// <see cref="LogOutput"/>と似ているが、サブキャラ用のログを出力するクラス。
    /// サブキャラ1つごとにファイルを1つ生成する点が異なる
    /// </summary>
    public class BuddyFileLogger 
    {
        private readonly Dictionary<BuddyId, BuddySingleFileLogger> _loggers = new();

        public BuddyFileLogger()
        {
            RefreshLogDirectory(SpecialFiles.BuddyLogFileDir);
            RefreshLogDirectory(SpecialFiles.DefaultBuddyLogFileDir);
        }

        private void RefreshLogDirectory(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);
        }

        public void Log(BuddyFolder folder, string content)
        {
            var logger = GetLogger(folder);
            logger.Log(content);
        }

        public void Log(BuddyFolder folder, Exception ex)
        {
            var logger = GetLogger(folder);
            logger.Log(ex);
        }

        // NOTE: 1回のアプリケーション実行中にフォルダのリネーム起因で別のBuddyに同じBuddyIdが割り当てられた場合、
        // その2つ(以上)のBuddyのログは同じファイルに記録される。これはby-design
        private BuddySingleFileLogger GetLogger(BuddyFolder folder)
        {
            var id = folder.BuddyId;
            if (_loggers.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var logger = new BuddySingleFileLogger(SpecialFiles.GetBuddyLogFilePath(folder));
            _loggers[id] = logger;
            return logger;
        }
    }

    public class BuddySingleFileLogger
    {
        private readonly string _filePath;
        private readonly string _fileName;
        private readonly object _writeLock = new();

        public BuddySingleFileLogger(string filePath)
        {
            _filePath = filePath;
            _fileName = Path.GetFileName(_filePath);
        }
        
        public void Log(string text)
        {
            // NOTE:
            // エディタで気軽に見れるようにエディタコンソールにも出力する。
            // そのうえでファイルI/Oの挙動はビルドに準じたいので、ファイルにも出力しておく
            if (Application.isEditor)
            {
                Debug.Log($"[Buddy:{_fileName}] {text}");
            }

            if (!File.Exists(_filePath))
            {
                lock (_writeLock)
                {
                    File.WriteAllText(_filePath, "");
                }
            }

            lock (_writeLock)
            {
                try
                {
                    using var sw = new StreamWriter(_filePath, true);
                    sw.WriteLine(text);
                }
                catch (Exception)
                {
                    //諦める
                }
            }
        }

        public void Log(Exception ex)
        {
            if (ex != null)
            {
                Log(ExToString(ex));
            }
        }
        
        private static string ExToString(Exception ex)
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\n" +
                ex.GetType().Name + "\n" +
                ex.Message + "\n" +
                ex.StackTrace;
        }   
    }
}
