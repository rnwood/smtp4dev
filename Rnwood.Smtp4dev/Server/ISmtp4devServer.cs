using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Server
{
    /// <summary>
    /// Represents an email attachment with its metadata and content.
    /// The consumer is responsible for disposing the Content stream after use.
    /// </summary>
    public class AttachmentInfo
    {
        /// <summary>
        /// Gets or sets the filename of the attachment.
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// Gets or sets the MIME content type of the attachment.
        /// </summary>
        public string ContentType { get; set; }
        
        /// <summary>
        /// Gets or sets the content stream of the attachment.
        /// The stream will be disposed by the SMTP client after sending.
        /// </summary>
        public Stream Content { get; set; }
    }

    public interface ISmtp4devServer
    {
        RelayResult TryRelayMessage(Message message, MailboxAddress[] overrideRecipients);
        Exception Exception { get; }
        bool IsRunning { get; }
        public IPEndPoint[] ListeningEndpoints { get;  }
        void TryStart();
        void Stop();
        Task DeleteSession(Guid id);
        Task DeleteAllSessions();
        void Send(IDictionary<string, string> headers, string[] to, string[] cc, string from, string[] envelopeRecipients, string subject, string bodyHtml, IEnumerable<AttachmentInfo> attachments = null);
    }
}