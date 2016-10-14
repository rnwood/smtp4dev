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

        private Task _task;
        private CancellationTokenSource _taskCancellationTokenSource = new CancellationTokenSource();

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

        public void ApplyStreamFilter(Func<Stream, Stream> filter)
        {
            ConnectionChannel.ApplyStreamFilter(filter);
        }

        public MailVerb MailVerb
        {
            get { return (MailVerb)VerbMap.GetVerbProcessor("MAIL"); }
        }

        public void WriteLine(string text, params object[] arg)
        {
            string formattedText = string.Format(text, arg);
            Session.AppendToLog(formattedText);
            ConnectionChannel.WriteLine(formattedText);
        }

        public void WriteResponse(SmtpResponse response)
        {
            WriteLine(response.ToString().TrimEnd());
        }

        public string ReadLine()
        {
            string text = ConnectionChannel.ReadLine();
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

        public Task StartProcessing()
        {
            _task = Task.Run(() => Process());
            return _task;
        }

        public void Process()
        {
            try
            {
                Server.Behaviour.OnSessionStarted(this, Session);
                SetReaderEncoding(Server.Behaviour.GetDefaultEncoding(this));

                if (Server.Behaviour.IsSSLEnabled(this))
                {
                    ConnectionChannel.ApplyStreamFilter(s =>
                    {
                        SslStream sslStream = new SslStream(s);
                        sslStream.AuthenticateAsServerAsync(Server.Behaviour.GetSSLCertificate(this)).Wait();
                        return sslStream;
                    });

                    Session.SecureConnection = true;
                }

                WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ServiceReady,
                                               Server.Behaviour.DomainName + " smtp4dev ready"));

                int numberOfInvalidCommands = 0;
                while (ConnectionChannel.IsConnected && !_taskCancellationTokenSource.IsCancellationRequested)
                {
                    bool badCommand = false;
                    SmtpCommand command = new SmtpCommand(ReadLine());
                    Server.Behaviour.OnCommandReceived(this, command);

                    if (command.IsValid)
                    {
                        IVerb verbProcessor = VerbMap.GetVerbProcessor(command.Verb);

                        if (verbProcessor != null)
                        {
                            try
                            {
                                verbProcessor.Process(this, command);
                            }
                            catch (SmtpServerException exception)
                            {
                                WriteResponse(exception.SmtpResponse);
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
                            WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ClosingTransmissionChannel, "Too many bad commands. Bye!"));
                            CloseConnection();
                        }
                        else
                        {
                            WriteResponse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised,
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

            if (_taskCancellationTokenSource.IsCancellationRequested)
            {
                Session.SessionErrorType = SessionErrorType.ServerShutdown;
            }

            CloseConnection();

            Session.EndDate = DateTime.Now;
            Server.Behaviour.OnSessionCompleted(this, Session);
        }

        public void Terminate()
        {
            _taskCancellationTokenSource.Cancel();
            _task.Wait();
        }
    }
}