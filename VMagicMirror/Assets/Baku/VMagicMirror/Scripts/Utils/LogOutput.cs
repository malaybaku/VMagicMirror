using System;
using System.IO;

namespace Baku.VMagicMirror
{
    public class LogOutput
    {
        //指針: log.txtというテキストがあれば随時appendする(安全重視で毎回書き込んでファイル閉じる)
        private static LogOutput _instance;
        public static LogOutput Instance => _instance ??= new LogOutput();
        
        private LogOutput()
        {
            _logFilePath = SpecialFiles.LogFilePath;
            if (File.Exists(_logFilePath))
            {
                File.Delete(_logFilePath);
            }
            if (Directory.Exists(SpecialFiles.LogFileDir))
            {
                File.WriteAllText(_logFilePath, "");
            }
        }

        private readonly object _writeLock = new object();
        private readonly string _logFilePath;

        public static string ExToString(Exception ex)
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\n" +
               ex.GetType().Name + "\n" +
               ex.Message + "\n" +
               ex.StackTrace;
        }   
        
        public void Write(string text)
        {
            if (!File.Exists(_logFilePath))
            {
                return;
            }

            lock (_writeLock)
            {
                try
                {
                    using var sw = new StreamWriter(_logFilePath, true);
                    sw.WriteLine(text);
                }
                catch (Exception)
                {
                    //諦める
                }
            }
        }

        public void Write(Exception ex)
        {
            if (ex != null)
            {
                Write(ExToString(ex));
            }
        }
    }
}
