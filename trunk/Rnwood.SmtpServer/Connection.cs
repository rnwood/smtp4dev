#region

using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class Connection : IConnection
    {

        public IConnectionChannel Channel { get; private set; }


        public Connection(IServer server, Func<Connection, IConnectionChannel> channelCreator) : this(server, channelCreator, new VerbMap())
        {
        }

        public Connection(IServer server, Func<Connection, IConnectionChannel> channelCreator, IVerbMap verbMap)
        {
            VerbMap = verbMap;
            Server = server;
            Channel = channelCreator(this);
            Session = server.Behaviour.OnCreateNewSession(this, Channel.ClientIPAddress, DateTime.Now);

            

            Channel.ReceiveTimeout = Server.Behaviour.GetReceiveTimeout(this);

            SetReaderEncodingToDefault();

            SetupVerbs();
        }

        #region IConnectionProcessor Members

        public IServer Server { get; private set; }



        public IExtensionProcessor[] ExtensionProcessors { get; private set; }


        public IVerbMap VerbMap { get; private set; }


        public MailVerb MailVerb
        {
            get { return (MailVerb)VerbMap.GetVerbProcessor("MAIL"); }
        }

        public void WriteLine(string text, params object[] arg)
        {
            string formattedText = string.Format(text, arg);
            Session.AppendToLog(formattedText);
            Channel.WriteLine(formattedText);
        }

        public void WriteResponse(SmtpResponse response)
        {
            WriteLine(response.ToString().TrimEnd());
        }

        public string ReadLine()
        {
            string text = Channel.ReadLine();
            Session.AppendToLog(text);
            return text;
        }

        public IEditableSession Session { get; private set; }

        public IEditableMessage CurrentMessage { get; private set; }

        public IEditableMessage NewMessage()
        {
            CurrentMessage = Server.Behaviour.OnCreateNewMessage(this);
            return CurrentMessage;
        }

        public void CommitMessage()
        {
            IMessage message = CurrentMessage;
            Session.AddMessage(message);
            CurrentMessage = null;

            Server.Behaviour.OnMessageReceived(this, message);
        }

        public void AbortMessage()
        {
            CurrentMessage = null;
        }

        #endregion


        private void SetupVerbs()
        {
            VerbMap.SetVerbProcessor("HELO", new HeloVerb());
            VerbMap.SetVerbProcessor("EHLO", new EhloVerb());
            VerbMap.SetVerbProcessor("QUIT", new QuitVerb());
            VerbMap.SetVerbProcessor("MAIL", new MailVerb());
            VerbMap.SetVerbProcessor("RCPT", new RcptVerb());
            VerbMap.SetVerbProcessor("DATA", new DataVerb());
            VerbMap.SetVerbProcessor("RSET", new RsetVerb());
            VerbMap.SetVerbProcessor("NOOP", new NoopVerb());

            ExtensionProcessors =
                Server.Behaviour.GetExtensions(this).Select(e => e.CreateExtensionProcessor(this)).ToArray();
        }

        private Thread _processThread;

        public void AbortProcessing()
        {
            lock (this)
            {
                if (_processThread != null)
                {
                    _processThread.Interrupt();
                }
            }
        }

        public void Process()
        {
            try
            {
                _processThread = Thread.CurrentThread;

                Server.Behaviour.OnSessionStarted(this, Session);


                WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ServiceReady,
                                               Server.Behaviour.DomainName + " smtp4dev ready"));

                int numberOfInvalidCommands = 0;

                while (Channel.IsConnected)
                {
                    SmtpCommand command = new SmtpCommand(ReadLine());
                    Server.Behaviour.OnCommandReceived(this, command);

                    if (command.IsValid)
                    {
                        IVerb verbProcessor = VerbMap.GetVerbProcessor(command.Verb);

                        if (verbProcessor != null)
                        {
                            numberOfInvalidCommands = 0;

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
                            numberOfInvalidCommands++;

                            if (numberOfInvalidCommands >= Server.Behaviour.MaximumNumberOfSequentialBadCommands)
                            {
                                WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ClosingTransmissionChannel,
                                                               "Command unrecognised - closing connection because of too many bad commands in a row"));
                                CloseConnection();
                            }
                            else
                            {
                                WriteResponse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised,
                                                               "Command unrecognised ({0} tries left)", Server.Behaviour.MaximumNumberOfSequentialBadCommands-numberOfInvalidCommands));
                            }
                        }
                    }
                    else if (command.IsEmpty)
                    {
                    }
                    else
                    {
                        WriteResponse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised,
                                                       "Command unrecognised"));
                    }
                }
            }
            catch (IOException ioException)
            {
                Session.SessionError = ioException.Message;
            }

            Channel.Close();

            Session.EndDate = DateTime.Now;
            Server.Behaviour.OnSessionCompleted(this, Session);

            lock (this)
            {
                _processThread = null;
            }
        }

        public void CloseConnection()
        {
            Channel.Close();
        }

        public void Kill()
        {
            Channel.Close();
        }

        public void SetReaderEncoding(Encoding encoding)
        {
            Channel.SetReaderEncoding(encoding);
        }

        public void SetReaderEncodingToDefault()
        {
            SetReaderEncoding(Server.Behaviour.GetDefaultEncoding(this));
        }
    }
}