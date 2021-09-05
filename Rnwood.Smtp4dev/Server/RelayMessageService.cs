using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json.Linq;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.RulesEngine;

namespace Rnwood.Smtp4dev.Server
{
    public class RelayMessageService : IRelayMessageService
    {
        private readonly Func<RelayOptions, SmtpClient> relaySmtpClientFactory;
        private readonly IOptionsMonitor<RelayOptions> relayOptionsMonitor;
        private readonly IRelayMessageRulesEngine relayMessageRulesEngine;
        private readonly ILogger<RelayMessageService> logger;

        public RelayMessageService(Func<RelayOptions, SmtpClient> relaySmtpClientFactory, IOptionsMonitor<RelayOptions> relayOptionsMonitor,
            IRelayMessageRulesEngine relayMessageRulesEngine, ILogger<RelayMessageService> logger)
        {
            this.relaySmtpClientFactory = relaySmtpClientFactory;
            this.relayOptionsMonitor = relayOptionsMonitor;
            this.relayMessageRulesEngine = relayMessageRulesEngine;
            this.logger = logger;
        }

        private RelayOptions RelayOptions => relayOptionsMonitor.CurrentValue;

        public async Task<RelayResult> TryRelayMessage(Message message, MailboxAddress[] overrideRecipients = null)
        {
            var result = new RelayResult(message);

            if (!RelayOptions.IsEnabled)
            {
                return result;
            }

            List<MailboxAddress> recipients;

            if (overrideRecipients == null)
            {
                recipients = RelayOptions.UseRulesEngine ? GetRecipientsUsingRulesEngine(message) : GetRecipientsViaAutomaticEmails(message);
            }
            else
            {
                recipients = overrideRecipients.ToList();
            }

            return await DispatchEmail(message, recipients);
        }

        private List<MailboxAddress> GetRecipientsUsingRulesEngine(Message message)
        {
            var resultList = new List<MailboxAddress>();
            var ruleResults = Task.Run(() => relayMessageRulesEngine.Execute(message)).Result;
            foreach (var result in ruleResults.Where(r => r.IsSuccess))
            {
                var jObject = result.Rule.Properties[nameof(RelayMessageProps)] as JObject;
                if (jObject is null)
                {
                    logger.LogWarning("Unable to parse RelayMessageProps for Rule, no relay will occur");
                    continue;
                }

                var relayMessageProps = jObject.ToObject<RelayMessageProps>();
                if (relayMessageProps == null)
                {
                    logger.LogWarning("Unable to deserialize {PropType}, no relay will occur", nameof(RelayMessageProps));
                    continue;
                }

                if (relayMessageProps.RelayToOriginalAddress)
                {
                    resultList.Add(MailboxAddress.Parse(message.To));
                }

                if (relayMessageProps.RelayToAdditionalAddress && relayMessageProps.RelayAdditionalAddress.Count > 0)
                {
                    relayMessageProps.RelayAdditionalAddress.ForEach(address => resultList.Add(MailboxAddress.Parse(address)));
                }
            }

            logger.LogDebug("Relaying Message to {RelayAddress}", string.Join(',', resultList.Select(x => x.Address)));
            return resultList;
        }

        private List<MailboxAddress> GetRecipientsViaAutomaticEmails(Message message)
        {
            return message.To
                .Split(",")
                .Select(MailboxAddress.Parse)
                .Where(r => RelayOptions.AutomaticEmails.Contains(r.Address, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        private async Task<RelayResult> DispatchEmail(Message message, IEnumerable<MailboxAddress> recipients)
        {
            var result = new RelayResult(message);
            
            using var relaySmtpClient = relaySmtpClientFactory(RelayOptions);
            foreach (var recipient in recipients)
            {
                try
                {
                    Console.WriteLine($"Relaying message to {recipient}");

                    
                    var apiMsg = new ApiModel.Message(message);
                    var newEmail = apiMsg.MimeMessage;
                    var sender = MailboxAddress.Parse(
                        !string.IsNullOrEmpty(RelayOptions.SenderAddress)
                            ? RelayOptions.SenderAddress
                            : apiMsg.From);
                    await relaySmtpClient.SendAsync(newEmail, sender, new[] { recipient });
                    result.RelayRecipients.Add(new RelayRecipientResult { Email = recipient.Address, RelayDate = DateTime.UtcNow });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Can not relay message to {recipient}: {e}");
                    result.Exceptions[recipient] = e;
                }
            }
            if (relaySmtpClient.IsConnected)
                await relaySmtpClient.DisconnectAsync(true);

            return result;
        }
    }
}