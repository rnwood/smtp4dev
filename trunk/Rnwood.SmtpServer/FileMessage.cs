using System;
using System.CodeDom.Compiler;
using System.IO;

namespace Rnwood.SmtpServer
{
    public class FileMessage : AbstractMessage
    {
        public FileMessage(ISession session, FileInfo file, bool keepOnDispose)
            : base(session)
        {
            _file = file;
            _keepOnDispose = keepOnDispose;
        }

        private FileInfo _file;
        private bool _keepOnDispose;

        public override Stream GetData(DataAccessMode dataAccessMode)
        {
            if (dataAccessMode == DataAccessMode.ForWriting)
            {
                return _file.OpenWrite();
            }else if (_file.Length == 0)
            {
                throw new InvalidOperationException("Cannot read message data before it has been written");
            }

            return new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete|FileShare.Read);
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