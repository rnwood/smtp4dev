using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer
{
    internal class TcpClientConnectionChannel : IConnectionChannel
    {
        public TcpClientConnectionChannel(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
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
            get { return _tcpClient.Client.Connected; }
        }

        public void Close()
        {
            _writer.Flush();
            _tcpClient.Dispose();
        }

        public int ReceiveTimeout
        {
            get { return _tcpClient.ReceiveTimeout; }
            set { _tcpClient.ReceiveTimeout = value; }
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
            string text = await _reader.ReadLineAsync();

            if (text == null)
            {
                throw new ConnectionUnexpectedlyClosedException();
            }

            return text;
        }

        public async Task WriteLineAsync(string text)
        {
            await _writer.WriteLineAsync(text);
        }
    }
}