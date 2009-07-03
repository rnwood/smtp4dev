using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Rnwood.SmtpServer;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer
{
    public class ConnectionProcessor
    {
        private TcpClient _tcpClient;
        private StreamWriter _writer;
        private StreamReader _reader;
        public Server Server
        {
            get;
            private set;
        }

        public ConnectionProcessor(Server server, TcpClient tcpClient)
        {
            VerbMap = new VerbMap();
            Session = new Session()
                              {
                                  ClientAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address
                              };

            Server = server;
            _tcpClient = tcpClient;

            _currentReaderEncoding = _sevenBitASCIIEncoding = Encoding.GetEncoding("ASCII", new EncoderExceptionFallback(), new Encoder7BitTruncatingFallback());
            _stream = tcpClient.GetStream();

            if (server.Behaviour.RunOverSSL)
            {
                SslStream sslStream = new SslStream(_stream);
                sslStream.AuthenticateAsServer(new X509Certificate(Resources.localhost));
                _stream = sslStream;
                Session.SecureConnection = true;
            }

            SetupReaderAndWriter();
            SetupVerbs();
        }

        private Stream _stream;
        private Encoding _sevenBitASCIIEncoding;
        private Encoding _currentReaderEncoding;

        public void SwitchReaderEncoding(Encoding encoding)
        {
            SetupReaderAndWriter();
        }

        private void SetupReaderAndWriter()
        {
            _writer = new StreamWriter(_stream, _currentReaderEncoding) { AutoFlush = true };
            _reader = new StreamReader(_stream, _currentReaderEncoding);
        }

        public void SwitchReaderEncodingToDefault()
        {
            SwitchReaderEncoding(_sevenBitASCIIEncoding);
        }

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

            ExtensionProcessors = Server.Extensions.Select(e => e.CreateExtensionProcessor(this)).ToArray();
        }

        public ExtensionProcessor[] ExtensionProcessors
        {
            get; private set;
        }

        public void CloseConnection()
        {
            _writer.Flush();
            _tcpClient.Close();
        }

        public VerbMap VerbMap
        {
            get;
            private set;
        }

        public void ApplyStreamFilter(Func<Stream, Stream> filter)
        {
            _stream = filter(_stream);
            SetupReaderAndWriter();
        }

        public void Start()
        {
            try
            {
                WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ServiceReady, "Raring for action"));

                while (_tcpClient.Connected)
                {
                    SmtpRequest request = new SmtpRequest(ReadLine());

                    if (request.IsValid)
                    {
                        Verb verbProcessor = VerbMap.GetVerbProcessor(request.Verb);

                        if (verbProcessor != null)
                        {
                            try
                            {
                                verbProcessor.Process(this, request);
                            }
                            catch (SmtpServerException exception)
                            {
                                WriteResponse(exception.SmtpResponse);
                            }
                        }
                        else
                        {
                            WriteResponse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised,
                                                           "Command unrecognised"));
                        }
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

            }

            CloseConnection();
        }

        public MailVerb MailVerb
        {
            get
            {
                return (MailVerb)VerbMap.GetVerbProcessor("MAIL");
            }
        }

        public void WriteLine(string text, params object[] arg)
        {
            string formattedText = string.Format(text, arg);
            Session.AppendToLog(formattedText);
            _writer.WriteLine(formattedText);
        }

        public void WriteResponse(SmtpResponse response)
        {
            WriteLine(response.ToString().TrimEnd());
        }

        public string ReadLine()
        {
            string text = _reader.ReadLine();
            Session.AppendToLog(text);
            return text;
        }

        public Session Session
        {
            get;
            private set;
        }

        public Message CurrentMessage
        {
            get;
            private set;
        }

        public Message NewMessage()
        {
            CurrentMessage = new Message(Session);
            return CurrentMessage;
        }

        public void CommitMessage()
        {
            Message message = CurrentMessage;
            Session.Messages.Add(message);
            CurrentMessage = null;

            Server.Behaviour.OnMessageReceived(message);
        }

        public void AbortMessage()
        {
            CurrentMessage = null;
        }
    }
}
