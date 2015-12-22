using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    internal class Smtp4devMessage : FileMessage, ISmtp4devMessage
    {
        internal Smtp4devMessage(ISession session, Guid id, FileInfo file) : base(file, true)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
    }
}