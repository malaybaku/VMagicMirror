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
    public class BuddyLogger 
    {
        private static BuddyLogger _instance;
        public static BuddyLogger Instance => _instance ??= new BuddyLogger();

        private readonly string _dir;
        private readonly Dictionary<string, BuddySingleFileLogger> _loggers = new();

        private BuddyLogger()
        {
            _dir = SpecialFiles.BuddyLogFileDir;
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
            Directory.CreateDirectory(_dir);
        }

        public void Log(string buddyId, string content)
        {
            var logger = GetLogger(buddyId);
            logger.Log(content);
        }

        public void Log(string buddyId, Exception ex)
        {
            var logger = GetLogger(buddyId);
            logger.Log(ex);
        }

        // NOTE: 1回のアプリケーション実行中にフォルダのリネーム起因で別のBuddyに同じBuddyIdが割り当てられた場合、
        // その2つ(以上)のBuddyのログは同じファイルに記録される。これはby-design
        private BuddySingleFileLogger GetLogger(string buddyId)
        {
            // NOTE: ファイルパスとして使うので、トラブル防止のためにlowerに統一してしまう
            buddyId = buddyId.ToLower();
            
            if (_loggers.TryGetValue(buddyId, out var cached))
            {
                return cached;
            }

            var logger = new BuddySingleFileLogger(
                Path.Combine(_dir, buddyId + ".log")
            );
            _loggers[buddyId] = logger;
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
            // ※エディタ実行時に「ファイル出力 + ログ出力する」にするのもアリ
            if (Application.isEditor)
            {
                Debug.Log($"[Buddy:{_fileName}] {text}");
                return;
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
