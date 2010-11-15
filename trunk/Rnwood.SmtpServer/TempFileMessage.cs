using System.CodeDom.Compiler;
using System.IO;

namespace Rnwood.SmtpServer
{
    public class TempFileMessage : AbstractMessage
    {
        public TempFileMessage(ISession session)
            : base(session)
        {
            _tempFileCollection = new TempFileCollection();
            _tempFile = new FileInfo(_tempFileCollection.AddExtension(".eml"));
        }

        private FileInfo _tempFile;
        private TempFileCollection _tempFileCollection;

        public override Stream GetData(bool forWriting)
        {
            if (forWriting)
            {
                return _tempFile.OpenWrite();
            }
            return _tempFile.OpenRead();
        }

        public override void Dispose()
        {
            _tempFileCollection.Delete();
        }
    }
}