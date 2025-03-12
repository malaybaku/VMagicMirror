using System;
using System.Linq;
using Microsoft.CodeAnalysis.Scripting;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    
    // TODO: LogLevelは用途が広いのでここじゃないとこで定義したほうが良いかも
    public enum BuddyLogLevel
    {
        Fatal = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4,
    }

    /// <summary>
    /// Buddyのログについて、ファイル出力とWPFへの送信の双方を設定に基づいて行うクラス
    /// </summary>
    public class BuddyLogger
    {
        private readonly BuddySettingsRepository _settingsRepository;
        private readonly BuddyFileLogger _fileLogger;
        private readonly BuddyMessageSender _sender;
        
        [Inject]
        public BuddyLogger(
            BuddySettingsRepository settingsRepository,
            BuddyFileLogger fileLogger,
            BuddyMessageSender sender)
        {
            _settingsRepository = settingsRepository;
            _fileLogger = fileLogger;
            _sender = sender;
        }
        
        public void Log(string buddyId, string message, BuddyLogLevel level)
        {
            LogInternal(buddyId, message, level);
        }
        
        public void LogCompileError(string buddyId, CompilationErrorException ex)
        {
            var message = $"Script has compile error: {ex.Message}";
            LogInternal(buddyId, message, BuddyLogLevel.Fatal);
            LogExceptionInternal(buddyId, ex);
        }
        
        // TODO: FatalじゃなくてErrorくらいの扱いにするオプションが欲しいかも
        public void LogRuntimeException(string buddyId, Exception ex)
        {
            if (!_settingsRepository.DeveloperModeActive.Value)
            {
                LogRuntimeExceptionSimple(buddyId, ex);
                return;
            }

            // NOTE: 開発者モードがオフの状態であらかじめ起動していたBuddyについても
            // スタックトレースを拾おうとするが、それは拾えないので、その場合はSimple版の挙動に帰着する
            
            // スタックトレースからスクリプト内の行番号を抽出
            // NOTE: Roslynのスクリプトは "Submission#0" という名前で実行される(らしい)
            var scriptStackFrame = ex.StackTrace?
                .Split('\n')
                .FirstOrDefault(line => line.Contains("Submission#0"));

            if (scriptStackFrame == null)
            {
                LogRuntimeExceptionSimple(buddyId, ex);
                return;
            }
            
            var parts = scriptStackFrame.Split(' ');
            var lineInfo = parts.FirstOrDefault(p => p.Contains(":line"));
            if (lineInfo == null)
            {
                LogRuntimeExceptionSimple(buddyId, ex);
                return;
            }

            var message = $"Runtime Error [{lineInfo.Trim()}]: {ex.Message}";
            LogInternal(buddyId, message, BuddyLogLevel.Fatal);
            LogExceptionInternal(buddyId, ex);
        }

        private void LogRuntimeExceptionSimple(string buddyId, Exception ex)
        {
            LogInternal(buddyId, ex.Message, BuddyLogLevel.Fatal);
            LogExceptionInternal(buddyId, ex);
        }

        // NOTE: このメソッドではファイルにのみスタックトレース等の詳細を含む情報を記録する。例外に対してLogInternalの直後に呼ぶのが望ましい
        private void LogExceptionInternal(string buddyId, Exception ex)
        {
            _fileLogger.Log(buddyId, ex);
        }
        
        private void LogInternal(string buddyId, string message, BuddyLogLevel level)
        {
            var currentLogLevel = _settingsRepository.LogLevel.Value;
            if (level > currentLogLevel)
            {
                return;
            }

            var content = GetLogHeader(level) + message;
            _fileLogger.Log(buddyId, content);
            _sender.NotifyBuddyLogMessage(buddyId, content, level);
        }

        private static string GetLogHeader(BuddyLogLevel level) => $"[{level}]";
    }
}
