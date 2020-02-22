using System;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class LogOutput
    {
        //指針: log.txtというテキストがあれば随時appendする(安全重視で毎回書き込んでファイル閉じる)
        private const string LogTextName = "log.txt";

        //なんとなくシングルトン
        private static LogOutput _instance;
        public static LogOutput Instance
            => _instance ?? (_instance = new LogOutput());
        private LogOutput()
        {
            _logFileDir = GetLogFileDir();
            _logFilePath = GetLogFilePath(_logFileDir);
            if (File.Exists(_logFilePath))
            {
                File.Delete(_logFilePath);
            }
            if (Directory.Exists(_logFileDir))
            {
                File.WriteAllText(_logFilePath, "");
            }
        }

        private readonly object _writeLock = new object();
        private readonly string _logFileDir;
        private readonly string _logFilePath;

        public void Write(string text)
        {
            if (!File.Exists(_logFilePath)) { return; }

            lock (_writeLock)
            {
                try
                {
                    using (var sw = new StreamWriter(_logFilePath, true))
                    {
                        sw.WriteLine(text);
                    }
                }
                catch (Exception)
                {
                    //諦める
                }
            }
        }

        public void Write(Exception ex)
        {
            if (ex == null) { return; }

            Write(
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\n" +
                ex.GetType().Name + "\n" +
                ex.Message + "\n" +
                ex.StackTrace
                );
        }


        private string GetLogFileDir()
        {
            string logDir = Path.GetDirectoryName(Path.GetDirectoryName(Application.streamingAssetsPath));
            if (File.Exists(Path.Combine(logDir, "VmagicMirror.exe")))
            {
                return logDir;
            }
            else
            {
                return "";
            }
        }

        private string GetLogFilePath(string dirPath) 
            => File.Exists(Path.Combine(dirPath, "VmagicMirror.exe")) ? 
            Path.Combine(dirPath, LogTextName) : 
            "";
    }
}
