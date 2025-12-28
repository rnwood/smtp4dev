using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Rnwood.Smtp4dev.Service
{
    /// <summary>
    /// A Serilog sink that writes to either Console.Out or Console.Error
    /// </summary>
    public class ConsoleStreamSink : ILogEventSink
    {
        private readonly ITextFormatter _formatter;
        private readonly TextWriter _output;
        private readonly object _syncRoot = new object();

        public ConsoleStreamSink(ITextFormatter formatter, bool useStdErr)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _output = useStdErr ? Console.Error : Console.Out;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            lock (_syncRoot)
            {
                _formatter.Format(logEvent, _output);
            }
        }
    }
}
