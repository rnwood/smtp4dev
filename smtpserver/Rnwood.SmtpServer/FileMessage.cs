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

        private readonly FileInfo _file;
        private readonly bool _keepOnDispose;

        public override Stream GetData(DataAccessMode dataAccessMode)
        {
            if (dataAccessMode == DataAccessMode.ForWriting)
            {
                return _file.OpenWrite();
            }
            return new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read);
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