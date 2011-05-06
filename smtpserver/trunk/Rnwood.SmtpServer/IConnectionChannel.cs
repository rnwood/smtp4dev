using System;
using System.IO;
using System.Net;
using System.Text;

namespace Rnwood.SmtpServer
{
    ///<summary>
    ///</summary>
    public interface IConnectionChannel
    {
        bool IsConnected { get; }
        int ReceiveTimeout { get; set; }
        IPAddress ClientIPAddress { get; }
        void Close();

        Encoding ReaderEncoding { get; }
        void SetReaderEncoding(Encoding encoding);

        void ApplyStreamFilter(Func<Stream, Stream> filter);
        void WriteLine(string text);
        string ReadLine();

    }
}