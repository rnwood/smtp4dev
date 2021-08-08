using System;
using System.Threading.Tasks;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Server
{
    public interface ISmtp4devServer
    {
        RelayResult TryRelayMessage(Message message, MailboxAddress[] overrideRecipients);
        Exception Exception { get; }
        bool IsRunning { get; }
        int PortNumber { get; }
        void TryStart();
        void Stop();
        Task DeleteSession(Guid id);
        Task DeleteAllSessions();
    }
}