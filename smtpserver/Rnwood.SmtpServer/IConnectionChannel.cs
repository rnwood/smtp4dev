using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer
{
    ///<summary>
    ///</summary>
    public interface IConnectionChannel
    {
        bool IsConnected { get; }
        TimeSpan ReceiveTimeout { get; set; }

        TimeSpan SendTimeout { get; set; }

        IPAddress ClientIPAddress { get; }

        Task FlushAsync();

        Task CloseAync();

        Encoding ReaderEncoding { get; }

        void SetReaderEncoding(Encoding encoding);

        Task ApplyStreamFilterAsync(Func<Stream, Task<Stream>> filter);

        Task WriteLineAsync(string text);

        Task<string> ReadLineAsync();

        event EventHandler Closed;
    }
}