using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Serilog;

namespace PBE_AssetsManager.Services
{
    public class LogService
    {
        private RichTextBox _outputRichTextBox;
        private readonly Dispatcher _dispatcher;
        private readonly ILogger _logger;

        private readonly Queue<LogEntry> _pendingLogs = new Queue<LogEntry>();

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Success,
            Debug
        }

        public class LogEntry
        {
            public string Message { get; }
            public LogLevel Level { get; }
            public Exception Exception { get; }
            public DateTime Timestamp { get; }

            public LogEntry(string message, LogLevel level, Exception exception = null)
            {
                Message = message;
                Level = level;
                Exception = exception;
                Timestamp = DateTime.Now;
            }
        }

        public class InteractiveLogEntry : LogEntry
        {
            public string LinkText { get; }
            public Action LinkAction { get; }

            public InteractiveLogEntry(string message, LogLevel level, string linkText, Action linkAction, Exception exception = null)
                : base(message, level, exception)
            {
                LinkText = linkText;
                LinkAction = linkAction;
            }
        }

        public LogService(ILogger logger)
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _logger = logger;
        }

        public void SetLogOutput(RichTextBox outputRichTextBox, bool preserveExistingLogs = false)
        {
            _outputRichTextBox = outputRichTextBox;

            _dispatcher.Invoke(() =>
            {
                if (_outputRichTextBox.Document == null || _outputRichTextBox.Document.Blocks.Count == 0 || !preserveExistingLogs)
                {
                    _outputRichTextBox.Document = new FlowDocument();
                }

                while (_pendingLogs.TryDequeue(out var logEntry))
                {
                    WriteLog(logEntry);
                }
            });
        }

        public void ClearLog()
        {
            _dispatcher.Invoke(() =>
            {
                if (_outputRichTextBox != null)
                {
                    _outputRichTextBox.Document = new FlowDocument();
                }
            });
        }

        public void Log(string message)
        {
            _logger.Information(message);
            WriteLog(new LogEntry(message, LogLevel.Info));
        }

        public void LogWarning(string message)
        {
            _logger.Warning(message);
            WriteLog(new LogEntry(message, LogLevel.Warning));
        }

        public void LogError(string message)
        {
            _logger.Error(message);
            WriteLog(new LogEntry(message, LogLevel.Error));
        }

        public void LogSuccess(string message)
        {
            _logger.Information(message);
            WriteLog(new LogEntry(message, LogLevel.Success));
        }

        public void LogDebug(string message)
        {
            _logger.Debug(message);
            WriteLog(new LogEntry(message, LogLevel.Debug));
        }

        public void LogError(Exception ex, string message)
        {
            _logger.Error(ex, message);
            WriteLog(new LogEntry(message, LogLevel.Error, ex));
        }

        public void LogCritical(Exception ex, string message)
        {
            _logger.Fatal(ex, message);
            // This method now only logs to the fatal error file, not to the UI.
        }

        public void LogInteractive(string message, string linkText, Action linkAction, LogLevel level = LogLevel.Info)
        {
            _logger.Information($"{message} (Link: {linkText})");
            WriteLog(new InteractiveLogEntry(message, level, linkText, linkAction));
        }

        private void WriteLog(LogEntry logEntry)
        {
            if (_outputRichTextBox == null)
            {
                _pendingLogs.Enqueue(logEntry);
                System.Diagnostics.Debug.WriteLine($"[PENDING LOG (early)] [{logEntry.Level}] {logEntry.Message}{(logEntry.Exception != null ? $" Exception: {logEntry.Exception.Message}" : "")}");
                return;
            }

            if (logEntry.Level == LogLevel.Debug)
            {
                return;
            }

            _dispatcher.Invoke(() =>
            {
                AppendToLog(logEntry);
            });
        }

        private void AppendToLog(LogEntry logEntry)
        {
            if (_outputRichTextBox == null) return;

            var paragraph = new Paragraph { Margin = new Thickness(0) };

            SolidColorBrush levelColor;
            string levelTag;

            switch (logEntry.Level)
            {
                case LogLevel.Info:
                    levelColor = Brushes.Green;
                    levelTag = "INFO";
                    break;
                case LogLevel.Warning:
                    levelColor = Brushes.Yellow;
                    levelTag = "WARNING";
                    break;
                case LogLevel.Error:
                    levelColor = Brushes.Red;
                    levelTag = "ERROR";
                    break;
                case LogLevel.Success:
                    levelColor = Brushes.LightGreen;
                    levelTag = "SUCCESS";
                    break;
                case LogLevel.Debug:
                    levelColor = Brushes.LightBlue;
                    levelTag = "DEBUG";
                    break;
                default:
                    levelColor = Brushes.White;
                    levelTag = "UNKNOWN";
                    break;
            }

            var timestampRun = new Run($"[{logEntry.Timestamp:HH:mm:ss}] ") { Foreground = Brushes.LightGray };
            var levelRun = new Run($"[{levelTag}] ") { Foreground = levelColor, FontWeight = FontWeights.Bold };
            var messageRun = new Run(logEntry.Message);

            paragraph.Inlines.Add(timestampRun);
            paragraph.Inlines.Add(levelRun);

            if (logEntry is InteractiveLogEntry interactiveLogEntry)
            {
                var hyperlink = new Hyperlink(messageRun)
                {
                    Foreground = Brushes.Cyan,
                    TextDecorations = TextDecorations.Underline
                };
                hyperlink.Click += (s, e) => interactiveLogEntry.LinkAction?.Invoke();
                paragraph.Inlines.Add(hyperlink);
            }
            else
            {
                paragraph.Inlines.Add(messageRun);
            }

            if (logEntry.Exception != null)
            {
                var exceptionDetailRun = new Run(" See application_errors.log for more details.")
                {
                    Foreground = Brushes.OrangeRed,
                    FontStyle = FontStyles.Italic
                };
                paragraph.Inlines.Add(exceptionDetailRun);
            }

            _outputRichTextBox.Document.Blocks.Add(paragraph);
            _outputRichTextBox.ScrollToEnd();
        }
    }
}
