using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    internal class Smtp4devMessage : MemoryMessage, ISmtp4devMessage
    {
        private Smtp4devMessage(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }

        public new class Builder : MemoryMessage.Builder
        {
            public Builder() : base(new Smtp4devMessage(Guid.NewGuid()))
            {
            }
        }
    }
}