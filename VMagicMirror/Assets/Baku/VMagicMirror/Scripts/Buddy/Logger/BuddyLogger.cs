using System;
using System.Linq;
using Baku.VMagicMirror.Buddy.Api;
using Microsoft.CodeAnalysis.Scripting;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
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
    public class BuddyLogger : ITickable
    {
        private readonly BuddySettingsRepository _settingsRepository;
        private readonly BuddyFileLogger _fileLogger;
        private readonly IMessageSender _sender;
        private readonly BuddyLogCounter _counter = new();

        private readonly Atomic<float> _realtimeSinceStartup = new();
        
        [Inject]
        public BuddyLogger(
            BuddySettingsRepository settingsRepository,
            BuddyFileLogger fileLogger,
            IMessageSender sender)
        {
            _settingsRepository = settingsRepository;
            _fileLogger = fileLogger;
            _sender = sender;
        }

        void ITickable.Tick()
        {
            _counter.Update(Time.deltaTime);
            _realtimeSinceStartup.Value = Time.realtimeSinceStartup;
        }

        public void Log(BuddyFolder folder, string message, BuddyLogLevel level)
        {
            LogInternal(folder, message, level);
        }

        // NOTE: 実装がたまたま単なるLog Fatal Errorと共通だが、意味合いとしてはCompileErrorと同格くらいに特殊なのでメソッドは分けてます
        public void LogScriptAnalyzeError(BuddyFolder folder, string message)
        {
            LogInternal(folder, message, BuddyLogLevel.Fatal);
        }

        public void LogCompileError(BuddyFolder folder, CompilationErrorException ex)
        {
            var message = $"Script has compile error: {ex.Message}";
            LogInternal(folder, message, BuddyLogLevel.Fatal);
            LogExceptionInternal(folder, ex);
        }
        
        // TODO: FatalじゃなくてErrorくらいの扱いにするオプションが欲しいかも
        public void LogRuntimeException(BuddyFolder folder, Exception ex)
        {
            if (!_settingsRepository.DeveloperModeActive.CurrentValue)
            {
                LogRuntimeExceptionSimple(folder, ex);
                return;
            }

            // スタックトレースからスクリプト内の行番号を抽出
            // NOTE: Roslynのスクリプトは "Submission#0" という名前で実行される(らしい)
            var scriptStackFrame = ex.StackTrace?
                .Split('\n')
                .FirstOrDefault(line => line.Contains("Submission#0"));

            if (scriptStackFrame == null)
            {
                LogRuntimeExceptionSimple(folder, ex);
                return;
            }

            // 行+列が入ってる場合はそれを拾う
            var parts = scriptStackFrame.Split(' ');
            var lineInfo = parts.FirstOrDefault(p => p.Contains(":line"));
            if (lineInfo != null)
            {
                var message = $"[{lineInfo.Trim()}]: {ex.Message}";
                LogInternal(folder, message, BuddyLogLevel.Fatal);
                LogExceptionInternal(folder, ex);
                return;
            }

            // NOTE: ココを通過するのは開発者モードがオフのとき + サブキャラが(なぜか)親ディレクトリのスクリプトを読み込んでるとき
            LogRuntimeExceptionSimple(folder, ex);
        }
        
        private void LogRuntimeExceptionSimple(BuddyFolder folder, Exception ex)
        {
            LogInternal(folder, ex.Message, BuddyLogLevel.Fatal);
            LogExceptionInternal(folder, ex);
        }

        // NOTE: このメソッドではファイルにのみスタックトレース等の詳細情報を記録し、WPFには送信しない。例外に対してLogInternalの直後に呼ぶのが望ましい
        private void LogExceptionInternal(BuddyFolder folder, Exception ex)
        {
            _fileLogger.Log(folder, ex);
        }
        
        private void LogInternal(BuddyFolder folder, string message, BuddyLogLevel level)
        {
            var currentLogLevel = _settingsRepository.LogLevel.CurrentValue;
            if (level > currentLogLevel)
            {
                // 例えば warningまでのログ出力が要望されているとき、Infoが来てもむしるう
                return;
            }

            var time = _realtimeSinceStartup.Value;
            string content;
            // NOTE: Verboseログはアプリ内部の情報を提示するもの…という扱いにするので、スクリプト側の行数は紐づけない
            if (_settingsRepository.DeveloperModeActive.CurrentValue &&
                (int)level < (int)BuddyLogLevel.Verbose && 
                TryGetScriptExecLocation(folder, out var locationInfo))
            {    
                // NOTE: 時刻もログ種類も字数が整うようにしてある。else側も同様
                content = $"[{time,7:0.000}][{LogLevelString(level)}][{locationInfo}] {message}";
            }
            else
            {
                content = $"[{time,7:0.000}][{LogLevelString(level)}] {message}";
            }
            
            _fileLogger.Log(folder, content);
            NotifyBuddyLogMessage(folder, content, level);
        }

        private void NotifyBuddyLogMessage(BuddyFolder folder, string message, BuddyLogLevel logLevel)
        {
            if (!_counter.TrySendLog(logLevel))
            {
                // ログを短時間でWPF側に投げすぎの場合、ここでガードされる
                return;
            }
            
            var content = JsonUtility.ToJson(new BuddyLogMessage()
            {
                BuddyId = folder.BuddyId.Value,
                Message = message,
                LogLevel = (int)logLevel,
            });
            _sender.SendCommand(MessageFactory.NotifyBuddyLog(content));
        }

        // ログレベルに対して字数を揃えた文字列を返す
        // NOTE: マルチスレッドとか繰り返し実行とか踏まえてナイーヴなswitchで書いてる
        private static string LogLevelString(BuddyLogLevel level) => level switch
        {
            BuddyLogLevel.Fatal => "Fatal  ",
            BuddyLogLevel.Error => "Error  ",
            BuddyLogLevel.Warning => "Warning",
            BuddyLogLevel.Info => "Info   ",
            BuddyLogLevel.Verbose => "Verbose",
            // ログでエラーにされてもヤなのでエラーにはしない
            _ => "Unknown  ",
        };

        // StackTraceを見に行ってユーザースクリプトの実行位置を特定しようとするすごいやつだよ
        private static bool TryGetScriptExecLocation(BuddyFolder folder, out string result)
        {
            result = "";
            var stackTrace = new System.Diagnostics.StackTrace(true).ToString();
            var scriptStackFrame = stackTrace
                .Split('\n')
                .FirstOrDefault(line => line.Contains("Submission#0"));
            if (scriptStackFrame == null)
            {
                return false;
            }
            
            // こういう記述を探す: "C:\Users\Example\Documents\VMagicMirror_Files\Buddy\ExampleBuddy\main.csx:123"
            var folderPath = SpecialFiles.GetBuddyDirectoryPath(folder);
            var folderPathIndex = scriptStackFrame.IndexOf(folderPath, StringComparison.InvariantCultureIgnoreCase);
            if (folderPathIndex >= 0)
            {
                // 上記の例に対して ".\main.csx:123" のような部分だけ取得する。"."は無いと何か気になるので入れているが、深い意味はない
                result = "." + scriptStackFrame[(folderPathIndex + folderPath.Length)..].Trim();
                return true;
            } 
            return false;
        }
        
        class BuddyLogCounter
        {
            // NOTE: どのログもLogSendMinIntervalを基本の送信間隔として使うが、
            // Fatal/Errorのそれぞれは専用のキャパシティを別で持っており、LogSendMinIntervalが上限に達していても追加でログを送れる。
            // エラー時のログはWPFのUIで強めに反映したいので、その反映が漏れてしまうのを防ぐのが狙い。 

            // ログの送信数をデクリメントする間隔(秒)
            private const float LogSendMinInterval = 0.1f;
            private const float ErrorLogSendMinInterval = 1f;
            private const float FatalLogSendMinInterval = 1f;
            // 0秒間に立て続けにWPFに送れるログ数
            private const int LogSendLimitCount = 10;
            private const int ErrorLogSendLimitCount = 1;
            private const int FatalLogSendLimitCount = 1;

            // これらの値が一定以上になるとログは送らない方がいい…という判定になる
            private readonly Atomic<int> _logSendCount = new();
            private float _logSendCountDecreaseTime;

            private readonly Atomic<int> _errorLogSendCount = new();
            private float _errorLogSendCountDecreaseTime;

            private readonly Atomic<int> _fatalLogSendCount = new();
            private float _fatalLogSendCountDecreaseTime;

            /// <summary>
            /// ログの送信可否を判定する。true を返した場合、ログは実際に送信したとみなして内部的なカウントを増やす
            /// </summary>
            /// <param name="logLevel"></param>
            /// <returns></returns>
            public bool TrySendLog(BuddyLogLevel logLevel)
            {
                if (_logSendCount.Value < LogSendLimitCount)
                {
                    _logSendCount.Value += 1;
                    return true;
                }

                switch (logLevel)
                {
                    case BuddyLogLevel.Error when _errorLogSendCount.Value < ErrorLogSendLimitCount:
                        _errorLogSendCount.Value += 1;
                        return true;
                    case BuddyLogLevel.Fatal when _fatalLogSendCount.Value < FatalLogSendLimitCount:
                        _fatalLogSendCount.Value += 1;
                        return true;
                    case BuddyLogLevel.Warning:
                    case BuddyLogLevel.Info:
                    case BuddyLogLevel.Verbose:
                    default:
                        return false;
                }
            }

            public void Update(float deltaTime)
            {
                _logSendCountDecreaseTime += deltaTime;
                while (_logSendCountDecreaseTime > LogSendMinInterval)
                {
                    _logSendCountDecreaseTime -= LogSendMinInterval;
                    if (_logSendCount.Value > 0)
                    {
                        _logSendCount.Value -= 1;
                    }
                }

                // NOTE: ErrorとFatalのカウントはめったに使わないはずなので、ガードされやすい条件でガードしている
                if (_errorLogSendCount.Value > 0)
                {
                    _errorLogSendCountDecreaseTime += deltaTime;
                    if (_errorLogSendCountDecreaseTime > ErrorLogSendMinInterval)
                    {
                        _errorLogSendCountDecreaseTime -= ErrorLogSendMinInterval;
                        _errorLogSendCount.Value -= 1;
                    }
                }

                if (_fatalLogSendCount.Value > 0)
                {
                    _fatalLogSendCountDecreaseTime += deltaTime;
                    if (_fatalLogSendCountDecreaseTime > FatalLogSendMinInterval)
                    {
                        _fatalLogSendCountDecreaseTime -= FatalLogSendMinInterval;
                        _fatalLogSendCount.Value -= 1;
                    }
                }
            }
        }
    }
    
    [Serializable]
    public class BuddyLogMessage
    {
        public string BuddyId;
        public string Message;
        public int LogLevel;
    }
}
