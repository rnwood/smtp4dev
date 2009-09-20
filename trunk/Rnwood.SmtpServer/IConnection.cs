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
        Server Server { get; }
        IExtensionProcessor[] ExtensionProcessors { get; }
        VerbMap VerbMap { get; }
        MailVerb MailVerb { get; }
        Session Session { get; }
        Message CurrentMessage { get; }
        void SwitchReaderEncoding(Encoding encoding);
        void SwitchReaderEncodingToDefault();
        void CloseConnection();
        void ApplyStreamFilter(Func<Stream, Stream> filter);
        void WriteLine(string text, params object[] arg);
        void WriteResponse(SmtpResponse response);
        string ReadLine();
        Message NewMessage();
        void CommitMessage();
        void AbortMessage();
    }
}