using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Core;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Rnwood.Smtp4dev.Service
{
    /// <summary>
    /// Represents a structured log entry
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public string Source { get; set; }
        public string FormattedMessage { get; set; }
    }

    /// <summary>
    /// Service to capture and store server logs in memory for streaming to web UI
    /// </summary>
    public class ServerLogService : ILogEventSink, IDisposable
    {
        private readonly ConcurrentQueue<LogEntry> _logBuffer = new ConcurrentQueue<LogEntry>();
        private readonly int _maxLogEntries;
        private readonly ITextFormatter _formatter;
        private readonly object _lock = new object();

        public event EventHandler<LogEntry> LogReceived;

        public ServerLogService(int maxLogEntries = 500)
        {
            _maxLogEntries = maxLogEntries;
            _formatter = new MessageTemplateTextFormatter("{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null);
        }

        public void Emit(LogEvent logEvent)
        {
            var stringWriter = new System.IO.StringWriter();
            _formatter.Format(logEvent, stringWriter);
            var formattedMessage = stringWriter.ToString();

            // Extract source context from log event properties
            string source = null;
            if (logEvent.Properties.TryGetValue("SourceContext", out var sourceValue))
            {
                source = sourceValue.ToString().Trim('"');
            }

            var logEntry = new LogEntry
            {
                Timestamp = logEvent.Timestamp.DateTime,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception?.ToString() ?? string.Empty,
                Source = source ?? "Unknown",
                FormattedMessage = formattedMessage
            };

            _logBuffer.Enqueue(logEntry);

            // Trim buffer if it exceeds max size
            while (_logBuffer.Count > _maxLogEntries)
            {
                _logBuffer.TryDequeue(out _);
            }

            // Notify subscribers
            LogReceived?.Invoke(this, logEntry);
        }

        public IEnumerable<LogEntry> GetAllLogEntries()
        {
            return _logBuffer.ToArray();
        }

        public string GetAllLogs()
        {
            return string.Join("", _logBuffer.Select(e => e.FormattedMessage));
        }

        public IEnumerable<string> GetRecentLogs(int count)
        {
            return _logBuffer.TakeLast(count).Select(e => e.FormattedMessage);
        }

        public void Clear()
        {
            lock (_lock)
            {
                _logBuffer.Clear();
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
