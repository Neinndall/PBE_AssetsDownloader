// Services/LogService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents; // Ensure this is present for Paragraph, Run, LineBreak
using System.Windows.Media;
using System.Windows.Threading;
using Serilog; // Añadimos el using para Serilog

namespace PBE_AssetsDownloader.Services
{
    public class LogService
    {
        private RichTextBox _outputRichTextBox;
        private ScrollViewer _logScrollViewer;
        private readonly Dispatcher _dispatcher;

        private readonly Queue<LogMessage> _pendingLogs = new Queue<LogMessage>();

        private Paragraph _logParagraph;

        private struct LogMessage
        {
            public string Message { get; }
            public LogLevel Level { get; }
            public Exception Exception { get; }

            public LogMessage(string message, LogLevel level, Exception exception = null)
            {
                Message = message;
                Level = level;
                Exception = exception;
            }
        }

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Success,
            Debug // ¡NUEVO: Nivel de log para depuración!
        }

        public LogService()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public void SetLogOutput(RichTextBox outputRichTextBox, ScrollViewer scrollViewer, bool preserveExistingLogs = false)
        {
            _outputRichTextBox = outputRichTextBox;
            _logScrollViewer = scrollViewer;
            
            _dispatcher.Invoke(() =>
            {
                if (!preserveExistingLogs)
                {
                    _outputRichTextBox.Document.Blocks.Clear();
                    _logParagraph = new Paragraph();
                    _logParagraph.Margin = new Thickness(0);
                    _outputRichTextBox.Document.Blocks.Add(_logParagraph);
                }
                else
                {
                    // Si preservamos contenido, usar el último párrafo existente o crear uno nuevo
                    _logParagraph = _outputRichTextBox.Document.Blocks.LastBlock as Paragraph;
                    if (_logParagraph == null)
                    {
                        _logParagraph = new Paragraph();
                        _logParagraph.Margin = new Thickness(0);
                        _outputRichTextBox.Document.Blocks.Add(_logParagraph);
                    }
                }
                
                // Procesar logs pendientes
                while (_pendingLogs.TryDequeue(out var logMessage))
                {
                    AppendToLog(logMessage);
                }
            });
        }

        public void ClearLog()
        {
            _dispatcher.Invoke(() =>
            {
                if (_outputRichTextBox != null)
                {
                    _outputRichTextBox.Document.Blocks.Clear();
                    _logParagraph = new Paragraph();
                    _logParagraph.Margin = new Thickness(0);
                    _outputRichTextBox.Document.Blocks.Add(_logParagraph);
                }
            });
        }

        public void Log(string message)
        {
            Serilog.Log.Information(message);
            WriteLog(message, LogLevel.Info);
        }
        public void LogWarning(string message)
        {
            Serilog.Log.Warning(message);
            WriteLog(message, LogLevel.Warning);
        }
        public void LogError(string message)
        {
            Serilog.Log.Error(message);
            WriteLog(message, LogLevel.Error);
        }
        public void LogSuccess(string message)
        {
            Serilog.Log.Information(message); // Serilog no tiene nivel 'Success', mapeamos a Information
            WriteLog(message, LogLevel.Success);
        }
        public void LogDebug(string message)
        {
            Serilog.Log.Debug(message);
        }

        public void LogError(Exception ex, string message)
        {
            Serilog.Log.Error(ex, message); // Loguear la excepción completa con Serilog
            WriteLog(message, LogLevel.Error, ex); // Mostrar en la UI con el mensaje amigable
        }

        private void WriteLog(string message, LogLevel level, Exception exception = null)
        {
            var logMessage = new LogMessage(message, level, exception);

            if (_outputRichTextBox == null || _logParagraph == null)
            {
                _pendingLogs.Enqueue(logMessage);
                System.Diagnostics.Debug.WriteLine($"[PENDING LOG (early)] [{level}] {message}{(exception != null ? $" Exception: {exception.Message}" : "")}");
                return;
            }

            _dispatcher.Invoke(() =>
            {
                AppendToLog(logMessage);
            });
        }

        private void AppendToLog(LogMessage logMessage)
        {
            if (_outputRichTextBox == null) return;

            if (_logParagraph == null || !_outputRichTextBox.Document.Blocks.Contains(_logParagraph))
            {
                _logParagraph = new Paragraph();
                _logParagraph.Margin = new Thickness(0);
                _outputRichTextBox.Document.Blocks.Clear();
                _outputRichTextBox.Document.Blocks.Add(_logParagraph);
            }

            SolidColorBrush levelColor;
            string levelTag;

            switch (logMessage.Level)
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
                    // Eliminamos la escritura manual a application_errors.log aquí
                    break;
                case LogLevel.Success:
                    levelColor = Brushes.LightGreen;
                    levelTag = "SUCCESS";
                    break;
                case LogLevel.Debug: // ¡NUEVO CASE para Debug!
                    levelColor = Brushes.LightBlue; // Puedes elegir otro color si prefieres
                    levelTag = "DEBUG";
                    break;
                default:
                    levelColor = Brushes.White;
                    levelTag = "UNKNOWN";
                    break;
            }

            var timestampRun = new Run($"[{DateTime.Now:HH:mm:ss}] ") { Foreground = Brushes.LightGray };
            var levelRun = new Run($"[{levelTag}] ") { Foreground = levelColor, FontWeight = FontWeights.Bold };
            var messageRun = new Run(logMessage.Message); 

            _logParagraph.Inlines.Add(timestampRun);
            _logParagraph.Inlines.Add(levelRun);
            _logParagraph.Inlines.Add(messageRun); 

            if (logMessage.Exception != null)
            {
                // Mostramos solo el mensaje de la excepción en la UI
                var exceptionDetailRun = new Run($" (Error: {logMessage.Exception.Message})") { Foreground = Brushes.OrangeRed, FontStyle = FontStyles.Italic };
                _logParagraph.Inlines.Add(exceptionDetailRun); 
            }

            _logParagraph.Inlines.Add(new LineBreak()); 

            _logScrollViewer?.ScrollToEnd();
        }
    }
}