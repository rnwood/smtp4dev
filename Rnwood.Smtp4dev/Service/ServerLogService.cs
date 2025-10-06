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
    /// Service to capture and store server logs in memory for streaming to web UI
    /// </summary>
    public class ServerLogService : ILogEventSink, IDisposable
    {
        private readonly ConcurrentQueue<string> _logBuffer = new ConcurrentQueue<string>();
        private readonly int _maxLogEntries;
        private readonly ITextFormatter _formatter;
        private readonly object _lock = new object();

        public event EventHandler<string> LogReceived;

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

            _logBuffer.Enqueue(formattedMessage);

            // Trim buffer if it exceeds max size
            while (_logBuffer.Count > _maxLogEntries)
            {
                _logBuffer.TryDequeue(out _);
            }

            // Notify subscribers
            LogReceived?.Invoke(this, formattedMessage);
        }

        public string GetAllLogs()
        {
            return string.Join("", _logBuffer);
        }

        public IEnumerable<string> GetRecentLogs(int count)
        {
            return _logBuffer.TakeLast(count);
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
