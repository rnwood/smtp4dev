#region

using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
        IMessageBuilder CurrentMessage { get; }
        Encoding ReaderEncoding { get; }

        void SetReaderEncoding(Encoding encoding);

        void SetReaderEncodingToDefault();

        void CloseConnection();

        Task ApplyStreamFilterAsync(Func<Stream, Task<Stream>> filter);

        Task WriteLineAsync(string text, params object[] arg);

        Task WriteResponseAsync(SmtpResponse response);

        Task<string> ReadLineAsync();

        IMessageBuilder NewMessage();

        void CommitMessage();

        void AbortMessage();
    }
}