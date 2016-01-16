using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public interface ISmtp4devEngine
    {
        bool IsRunning { get; }

        Exception ServerError { get; }

        event EventHandler StateChanged;
    }
}