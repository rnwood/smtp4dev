using System;
using System.IO;

namespace Rnwood.SmtpServer
{
    public interface IMessage : IDisposable
    {
        /// <summary>
        /// Date the messge was received by the server.
        /// </summary>
        DateTime ReceivedDate { get; }

        /// <summary>
        /// Session the message was received on.
        /// </summary>
        ISession Session { get; }

        /// <summary>
        /// Sender of the message as specified by the client when sending MAIL FROM command.
        /// </summary>
        string From { get; }

        /// <summary>
        /// Receipient of the message as specified by the client when sending RCPT TO command.
        /// </summary>
        string[] To { get; }

        /// <summary>
        /// True if the message was received over a secure connection.
        /// </summary>
        bool SecureConnection { get; }

        /// <summary>
        /// True if the message was received over a 8-bit 'clean' connection using the 8BITMIME extension.
        /// </summary>
        bool EightBitTransport { get; }

        /// <summary>
        /// The size of the message as declared by the client using the SIZE extension to the MAIL FROM command, or null if not specified by the client.
        /// </summary>
        long? DeclaredMessageSize { get; }

        Stream GetData();
    }
}