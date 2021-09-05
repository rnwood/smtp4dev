using System.Threading.Tasks;
using MimeKit;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Server
{
    public interface IRelayMessageService
    {
        Task<RelayResult> TryRelayMessage(Message message, MailboxAddress[] overrideRecipients = null);
    }
}