#region

using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;
using System;
using System.IO;
using System.Text;

#endregion

namespace Rnwood.SmtpServer
{
    public interface IConnection
    {
        IServer Server { get; }
        IExtensionProcessor[] ExtensionProcessors { get; }
        IVerbMap VerbMap { get; }
        MailVerb MailVerb { get; }
        IEditableSession Session { get; }
        IEditableMessage CurrentMessage { get; }
        Encoding ReaderEncoding { get; }

        void SetReaderEncoding(Encoding encoding);

        void SetReaderEncodingToDefault();

        void CloseConnection();

        void ApplyStreamFilter(Func<Stream, Stream> filter);

        void WriteLine(string text, params object[] arg);

        void WriteResponse(SmtpResponse response);

        string ReadLine();

        IEditableMessage NewMessage();

        void CommitMessage();

        void AbortMessage();
    }
}