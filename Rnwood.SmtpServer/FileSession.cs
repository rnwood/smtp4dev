using System;
using System.IO;
using System.Net;

namespace Rnwood.SmtpServer
{
    public class FileSession : AbstractSession
    {
        public FileSession(IPAddress clientAddress, DateTime startDate, FileInfo file, bool keepOnDispose) : base(clientAddress, startDate)
        {
            _file = file;
            _keepOnDispose = keepOnDispose;
        }

        private readonly FileInfo _file;
        private readonly bool _keepOnDispose;

        public override TextReader GetLog()
        {
            return _file.OpenText();
        }

        public override void AppendToLog(string text)
        {
            using (StreamWriter writer = _file.AppendText())
            {
                writer.WriteLine(text);
            }
        }

        public override void Dispose()
        {
            if (!_keepOnDispose)
            {
                if (_file.Exists)
                {
                    _file.Delete();
                }
            }
        }
    }
}