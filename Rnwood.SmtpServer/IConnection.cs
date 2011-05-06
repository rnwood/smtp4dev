#region

using System;
using System.IO;
using System.Text;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;

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
        IConnectionChannel Channel { get; }
        void CloseConnection();
        void WriteResponse(SmtpResponse response);
        IEditableMessage NewMessage();
        void CommitMessage();
        void AbortMessage();
        string ReadLine();
        void SetReaderEncoding(Encoding encoding);
        void SetReaderEncodingToDefault();
    }
}