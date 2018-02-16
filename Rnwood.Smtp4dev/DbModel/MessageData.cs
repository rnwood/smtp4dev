using System;

namespace Rnwood.Smtp4dev.DbModel
{
    public class MessageData
    {
        public MessageData()
        {

        }

        public Guid Id { get; set; }

        public Guid MessageId { get; set; }

        public byte[] Data { get; set; }
    }
}
