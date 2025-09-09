using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rnwood.SmtpServer;

namespace Rnwood.Smtp4dev.Server
{
    /// <summary>
    /// A message implementation for imported EML files.
    /// </summary>
    public class ImportedMessage : IMessage
    {
        private readonly byte[] data;
        private readonly List<string> recipients = new();

        public ImportedMessage(byte[] data)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            ReceivedDate = DateTime.Now;
        }

        public long? DeclaredMessageSize => data.Length;
        public bool EightBitTransport { get; set; } = true;
        public string From { get; set; } = "";
        public DateTime ReceivedDate { get; set; }
        public bool SecureConnection { get; set; } = false;
        public ISession Session { get; set; }

        public IReadOnlyCollection<string> Recipients => recipients.AsReadOnly();

        public bool HasBareLineFeed => false;

        public void AddRecipient(string recipient)
        {
            recipients.Add(recipient);
        }

        public Task<Stream> GetData()
        {
            return Task.FromResult<Stream>(new MemoryStream(data));
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}