using System;
using System.IO;
using System.Net;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class MemorySession : AbstractSession
    {
        public MemorySession(IPAddress clientAddress, DateTime startDate) : base(clientAddress, startDate)
        {
        }

        private readonly StringBuilder _log = new StringBuilder();
        public override TextReader GetLog()
        {
            return new StringReader(_log.ToString());
        }

        public override void AppendToLog(string text)
        {
            _log.AppendLine(text);
        }

        public override void Dispose()
        {

        }
    }
}