using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public interface ISmtp4devServer
    {
        event EventHandler<EventArgs> MessagesChanged;

        IEnumerable<IMessage> Messages { get; }

        void ApplySettings(Settings settings);

        bool IsRunning { get; }

        Exception ServerError { get; }
    }
}