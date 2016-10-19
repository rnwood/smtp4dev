using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer
{
    public class TcpClientConnectionChannel : IConnectionChannel
    {
        public TcpClientConnectionChannel(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            IsConnected = true;
            SetReaderEncoding(Encoding.ASCII);
        }

        private readonly TcpClient _tcpClient;
        private StreamReader _reader;
        private Stream _stream;
        private StreamWriter _writer;

        public Encoding ReaderEncoding
        {
            get;
            private set;
        }

        public void SetReaderEncoding(Encoding encoding)
        {
            ReaderEncoding = encoding;
            SetupReaderAndWriter();
        }

        public bool IsConnected
        {
            get; private set;
        }

        public event EventHandler Closed;

        public async Task FlushAsync()
        {
            await _writer.FlushAsync();
        }

        public async Task CloseAync()
        {
            if (IsConnected)
            {
                IsConnected = false;
                _tcpClient.Dispose();

                Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        public TimeSpan ReceiveTimeout
        {
            get { return TimeSpan.FromMilliseconds(_tcpClient.ReceiveTimeout); }
            set { _tcpClient.ReceiveTimeout = (int)Math.Min(int.MaxValue, value.TotalMilliseconds); }
        }

        public TimeSpan SendTimeout
        {
            get { return TimeSpan.FromMilliseconds(_tcpClient.SendTimeout); }
            set { _tcpClient.SendTimeout = (int)Math.Min(int.MaxValue, value.TotalMilliseconds); }
        }

        public IPAddress ClientIPAddress
        {
            get { return ((IPEndPoint)_tcpClient.Client.RemoteEndPoint).Address; }
        }

        public async Task ApplyStreamFilterAsync(Func<Stream, Task<Stream>> filter)
        {
            _stream = await filter(_stream);
            SetupReaderAndWriter();
        }

        private void SetupReaderAndWriter()
        {
            _writer = new StreamWriter(_stream, ReaderEncoding) { AutoFlush = true, NewLine = "\r\n" };
            _reader = new StreamReader(_stream, ReaderEncoding);
        }

        public async Task<string> ReadLineAsync()
        {
            try
            {
                string text = await _reader.ReadLineAsync();

                if (text == null)
                {
                    throw new IOException("Reader returned null string"); ;
                }

                return text;
            }
            catch (IOException e)
            {
                await CloseAync();
                throw new ConnectionUnexpectedlyClosedException("Read failed", e);
            }
        }

        public async Task WriteLineAsync(string text)
        {
            try
            {
                await _writer.WriteLineAsync(text);
            }
            catch (IOException e)
            {
                await CloseAync();
                throw new ConnectionUnexpectedlyClosedException("Write failed", e);
            }
        }
    }
}