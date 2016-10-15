#region

using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;
using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Rnwood.SmtpServer
{
    public class Connection : IConnection
    {
        public IConnectionChannel ConnectionChannel { get; private set; }

        public Connection(IServer server, IConnectionChannel connectionChannel, IVerbMap verbMap)
        {
            ConnectionChannel = connectionChannel;
            VerbMap = verbMap;
            Session = server.Behaviour.OnCreateNewSession(this, ConnectionChannel.ClientIPAddress, DateTime.Now);

            Server = server;

            ConnectionChannel.ReceiveTimeout = Server.Behaviour.GetReceiveTimeout(this);
            SetReaderEncodingToDefault();

            ExtensionProcessors = Server.Behaviour.GetExtensions(this).Select(e => e.CreateExtensionProcessor(this)).ToArray();
        }

        #region IConnectionProcessor Members

        public IServer Server { get; private set; }

        public void SetReaderEncoding(Encoding encoding)
        {
            ConnectionChannel.SetReaderEncoding(encoding);
        }

        public Encoding ReaderEncoding
        {
            get { return ConnectionChannel.ReaderEncoding; }
        }

        public void SetReaderEncodingToDefault()
        {
            SetReaderEncoding(Server.Behaviour.GetDefaultEncoding(this));
        }

        public IExtensionProcessor[] ExtensionProcessors { get; private set; }

        public void CloseConnection()
        {
            ConnectionChannel.Close();
        }

        public IVerbMap VerbMap { get; private set; }

        public async Task ApplyStreamFilterAsync(Func<Stream, Task<Stream>> filter)
        {
            await ConnectionChannel.ApplyStreamFilterAsync(filter);
        }

        public MailVerb MailVerb
        {
            get { return (MailVerb)VerbMap.GetVerbProcessor("MAIL"); }
        }

        public async Task WriteLineAsync(string text, params object[] arg)
        {
            string formattedText = string.Format(text, arg);
            Session.AppendToLog(formattedText);
            await ConnectionChannel.WriteLineAsync(formattedText);
        }

        public async Task WriteResponseAsync(SmtpResponse response)
        {
            await WriteLineAsync(response.ToString().TrimEnd());
        }

        public async Task<string> ReadLineAsync()
        {
            string text = await ConnectionChannel.ReadLineAsync();
            Session.AppendToLog(text);
            return text;
        }

        public IEditableSession Session { get; private set; }

        public IMessageBuilder CurrentMessage { get; private set; }

        public IMessageBuilder NewMessage()
        {
            CurrentMessage = Server.Behaviour.OnCreateNewMessage(this);
            CurrentMessage.Session = Session;
            return CurrentMessage;
        }

        public void CommitMessage()
        {
            IMessage message = CurrentMessage.ToMessage();
            Session.AddMessage(message);
            CurrentMessage = null;

            Server.Behaviour.OnMessageReceived(this, message);
        }

        public void AbortMessage()
        {
            CurrentMessage = null;
        }

        #endregion

        public async Task ProcessAsync()
        {
            try
            {
                Server.Behaviour.OnSessionStarted(this, Session);
                SetReaderEncoding(Server.Behaviour.GetDefaultEncoding(this));

                if (Server.Behaviour.IsSSLEnabled(this))
                {
                    await ConnectionChannel.ApplyStreamFilterAsync(async s =>
                    {
                        SslStream sslStream = new SslStream(s);
                        await sslStream.AuthenticateAsServerAsync(Server.Behaviour.GetSSLCertificate(this));
                        return sslStream;
                    });

                    Session.SecureConnection = true;
                }

                await WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.ServiceReady,
                                               Server.Behaviour.DomainName + " smtp4dev ready"));

                int numberOfInvalidCommands = 0;
                while (ConnectionChannel.IsConnected)
                {
                    bool badCommand = false;
                    SmtpCommand command = new SmtpCommand(await ReadLineAsync());
                    Server.Behaviour.OnCommandReceived(this, command);

                    if (command.IsValid)
                    {
                        IVerb verbProcessor = VerbMap.GetVerbProcessor(command.Verb);

                        if (verbProcessor != null)
                        {
                            try
                            {
                                await verbProcessor.ProcessAsync(this, command);
                            }
                            catch (SmtpServerException exception)
                            {
                                await WriteResponseAsync(exception.SmtpResponse);
                            }
                        }
                        else
                        {
                            badCommand = true;
                        }
                    }
                    else if (command.IsEmpty)
                    {
                    }
                    else
                    {
                        badCommand = true;
                    }

                    if (badCommand)
                    {
                        numberOfInvalidCommands++;

                        if (Server.Behaviour.MaximumNumberOfSequentialBadCommands > 0 &&
                        numberOfInvalidCommands >= Server.Behaviour.MaximumNumberOfSequentialBadCommands)
                        {
                            await WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.ClosingTransmissionChannel, "Too many bad commands. Bye!"));
                            CloseConnection();
                        }
                        else
                        {
                            await WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised,
                                                           "Command unrecognised"));
                        }
                    }
                }
            }
            catch (IOException ioException)
            {
                Session.SessionError = ioException;
                Session.SessionErrorType = SessionErrorType.NetworkError;
            }
            catch (Exception exception)
            {
                Session.SessionError = exception;
                Session.SessionErrorType = SessionErrorType.UnexpectedException;
            }

            CloseConnection();

            Session.EndDate = DateTime.Now;
            Server.Behaviour.OnSessionCompleted(this, Session);
        }

        public void Terminate()
        {
            CloseConnection();
        }
    }
}