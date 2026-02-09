using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Jint;
using Jint.Native;
using Jint.Native.Json;
using Jint.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rnwood.Smtp4dev.Data;
using Rnwood.SmtpServer.Extensions.Auth;
using Serilog;
using SmtpResponse = Rnwood.SmtpServer.SmtpResponse;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Net;
using Rnwood.Smtp4dev.Server.Settings;
using DeepEqual.Syntax;
using MailboxOptions = Rnwood.Smtp4dev.Server.Settings.MailboxOptions;
using DotNet.Globbing;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;
using static MailKit.Net.Imap.ImapMailboxFilter;
using Org.BouncyCastle.Cms;
using LinqKit;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.Security.Authentication;
using System.Net.Security;
using Rnwood.Smtp4dev.Server.Auth;

namespace Rnwood.Smtp4dev.Server
{
    internal class Smtp4devServer : ISmtp4devServer, IHostedService
    {
        private readonly ILogger log = Log.ForContext<Smtp4devServer>();
        private readonly OAuth2TokenValidator oauth2TokenValidator;
        private readonly MailboxRouter mailboxRouter;

        public Smtp4devServer(IServiceScopeFactory serviceScopeFactory, IOptionsMonitor<Settings.ServerOptions> serverOptions,
            IOptionsMonitor<RelayOptions> relayOptions, NotificationsHub notificationsHub, Func<RelayOptions, SmtpClient> relaySmtpClientFactory,
            ITaskQueue taskQueue, ScriptingHost scriptingHost)
        {
            this.notificationsHub = notificationsHub;
            this.serverOptions = serverOptions;
            this.relayOptions = relayOptions;
            this.serviceScopeFactory = serviceScopeFactory;
            this.relaySmtpClientFactory = relaySmtpClientFactory;
            this.taskQueue = taskQueue;
            this.scriptingHost = scriptingHost;
            this.oauth2TokenValidator = new OAuth2TokenValidator(log);
            this.mailboxRouter = new MailboxRouter();

            taskQueue.Start();
            StartWatchingServerOptionsForChanges();
        }

        private void StartWatchingServerOptionsForChanges()
        {
            IDisposable eventHandler = null;
            var obs = Observable.FromEvent<Settings.ServerOptions>(e => eventHandler = this.serverOptions.OnChange(e), e => eventHandler.Dispose());
            obs.Throttle(TimeSpan.FromMilliseconds(100)).Subscribe(OnServerOptionsChanged);
        }

        private void OnServerOptionsChanged(Settings.ServerOptions arg1)
        {
            if (arg1.IsDeepEqual(this.lastStartOptions))
            {
                return;
            }

            if (this.smtpServer?.IsRunning == true)
            {
                log.Information("ServerOptions changed. Restarting server...");
                Stop();
                TryStart();
            }
            else
            {
                log.Information("ServerOptions changed.");
            }
        }

        private void CreateSmtpServer()
        {
            X509Certificate2 cert = CertificateHelper.GetTlsCertificate(serverOptions.CurrentValue, log);

            Settings.ServerOptions serverOptionsValue = serverOptions.CurrentValue;
            IPAddress bindAddress = null;
            if (!string.IsNullOrWhiteSpace(serverOptionsValue.BindAddress))
            {
                if (!IPAddress.TryParse(serverOptionsValue.BindAddress, out bindAddress))
                {
                    throw new ArgumentException($"Invalid bind address: {serverOptionsValue.BindAddress}");
                }
            }

            var builder = SmtpServer.ServerOptions.Builder()
                .WithAllowRemoteConnections(serverOptionsValue.AllowRemoteConnections)
                .WithEnableIpV6(!serverOptionsValue.DisableIPv6)
                .WithDomainName(serverOptionsValue.HostName)
                .WithPort(serverOptionsValue.Port)
                .WithRequireAuthentication(serverOptionsValue.AuthenticationRequired)
                .WithNonSecureAuthMechanisms(serverOptionsValue.SmtpEnabledAuthTypesWhenNotSecureConnection.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .WithSecureAuthMechanisms(serverOptionsValue.SmtpEnabledAuthTypesWhenSecureConnection.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .WithImplicitTlsCertificate(serverOptionsValue.TlsMode == TlsMode.ImplicitTls ? cert : null)
                .WithStartTlsCertificate(serverOptionsValue.TlsMode == TlsMode.StartTls ? cert : null)
                .WithSslProtocols(!string.IsNullOrWhiteSpace(serverOptionsValue.SslProtocols) 
                    ? serverOptionsValue.SslProtocols.Split(",", StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(s => Enum.Parse<SslProtocols>(s, true)).Aggregate((current, protocol) => current | protocol) 
                    : SslProtocols.None)
                .WithMaxMessageSize(serverOptionsValue.MaxMessageSize);

            if (bindAddress != null)
            {
                builder.WithBindAddress(bindAddress);
            }

            if (!string.IsNullOrWhiteSpace(serverOptionsValue.TlsCipherSuites))
            {
                var cipherSuites = serverOptionsValue.TlsCipherSuites.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => Enum.Parse<TlsCipherSuite>(s, true))
                    .ToArray();
                builder.WithTlsCipherSuites(cipherSuites);
            }

            this.smtpServer = new Rnwood.SmtpServer.SmtpServer(builder.Build());
            this.smtpServer.MessageCompletedEventHandler += OnMessageCompleted;
            this.smtpServer.MessageReceivedEventHandler += OnMessageReceived;
            this.smtpServer.SessionCompletedEventHandler += OnSessionCompleted;
            this.smtpServer.SessionStartedHandler += OnSessionStarted;
            this.smtpServer.AuthenticationCredentialsValidationRequiredEventHandler += OnAuthenticationCredentialsValidationRequired;
            this.smtpServer.IsRunningChanged += OnIsRunningChanged;
            ((SmtpServer.ServerOptions)this.smtpServer.Options).MessageStartEventHandler += OnMessageStart;

            ((SmtpServer.ServerOptions)this.smtpServer.Options).MessageRecipientAddingEventHandler += OnMessageRecipientAddingEventHandler;
            ((SmtpServer.ServerOptions)this.smtpServer.Options).CommandReceivedEventHandler += OnCommandReceived;
        }

        private Task OnCommandReceived(object sender, CommandEventArgs e)
        {
            if (!scriptingHost.HasValidateCommandExpression)
            {
                return Task.CompletedTask;
            }

            using var scope = serviceScopeFactory.CreateScope();
            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            Session dbSession = dbContext.Sessions.Find(activeSessionsToDbId[e.Connection.Session]);

            var apiSession = new ApiModel.Session(dbSession);

            var errorResponse = scriptingHost.ValidateCommand(e.Command, apiSession, e.Connection);

            if (errorResponse != null)
            {
                throw new SmtpServerException(errorResponse);
            }

            return Task.CompletedTask;
        }

        private Task OnMessageRecipientAddingEventHandler(object sender, RecipientAddingEventArgs e)
        {
            var sessionId = activeSessionsToDbId[e.Message.Session];
            using var scope = serviceScopeFactory.CreateScope();
            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            var session = dbContext.Sessions.AsNoTracking().Single(s => s.Id == sessionId);
            var apiSession = new ApiModel.Session(session);

            if (!this.scriptingHost.ValidateRecipient(apiSession, e.Recipient, e.Connection))
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.RecipientRejected, "Recipient rejected"));
            }

            return Task.CompletedTask;
        }

        private Task OnMessageStart(object sender, MessageStartEventArgs e)
        {
            if (this.serverOptions.CurrentValue.SecureConnectionRequired && !e.Session.SecureConnection)
            {
                throw new SmtpServerException(new SmtpResponse(451, "Secure connection required"));
            }

            if (this.serverOptions.CurrentValue.AuthenticationRequired && !e.Session.Authenticated)
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.AuthenticationRequired, "Authentication is required"));
            }

            return Task.CompletedTask;
        }

        private void OnIsRunningChanged(object sender, EventArgs e)
        {
            if (this.smtpServer.IsRunning) return;
            log.Information("SMTP server stopped.");
            this.notificationsHub.onServerChanged().Wait();
        }

        private async Task OnMessageCompleted(object sender, ConnectionEventArgs e)
        {
            if (!scriptingHost.HasValidateMessageExpression)
            {
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var mimeProcessingService = scope.ServiceProvider.GetService<MimeProcessingService>();
            Message message = new MessageConverter(mimeProcessingService).ConvertAsync(await e.Connection.CurrentMessage.ToMessage(), e.Connection.CurrentMessage.Recipients.ToArray()).Result;

            var apiMessage = new ApiModel.Message(message);

            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            Session dbSession = dbContext.Sessions.Find(activeSessionsToDbId[e.Connection.Session]);

            var apiSession = new ApiModel.Session(dbSession);

            var errorResponse = scriptingHost.ValidateMessage(apiMessage, apiSession, e.Connection);

            if (errorResponse != null)
            {
                throw new SmtpServerException(errorResponse);
            }
        }

        public void Stop()
        {
            log.Information("SMTP server stopping...");
            this.smtpServer?.Stop(true);
        }


        private void DoCleanup()
        {
            //Mark sessions as ended.
            using var scope = serviceScopeFactory.CreateScope();
            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            dbContext.Sessions.Where(s => !s.EndDate.HasValue).ExecuteUpdate(u => u.SetProperty(s => s.EndDate, DateTime.Now));

            //Find mailboxes in config not in DB and create
            var serverOptionsCurrentValue = this.serverOptions.CurrentValue;
            var configuredMailboxesAndDefault = serverOptionsCurrentValue.Mailboxes.Concat(new[] { new MailboxOptions { Name = MailboxOptions.DEFAULTNAME } });
            var dbMailboxes = dbContext.Mailboxes.ToList();
            foreach (MailboxOptions mailbox in configuredMailboxesAndDefault)
            {
                var dbMailbox = dbMailboxes.FirstOrDefault(m => m.Name == mailbox.Name);
                if (dbMailbox == null)
                {
                    log.Information("Creating mailbox {name}", mailbox.Name);
                    dbMailbox = new Mailbox
                    {
                        Name = mailbox.Name
                    };
                    dbContext.Add(dbMailbox);
                }
            }
            
            dbContext.SaveChanges();
            
            // Ensure default folders exist for each mailbox
            var allMailboxes = dbContext.Mailboxes.Include(m => m.MailboxFolders).ToList();
            foreach (var mailbox in allMailboxes)
            {
                if (!mailbox.MailboxFolders.Any(f => f.Name == MailboxFolder.INBOX))
                {
                    log.Information("Creating INBOX folder for mailbox {mailboxName}", mailbox.Name);
                    dbContext.MailboxFolders.Add(new MailboxFolder
                    {
                        Name = MailboxFolder.INBOX,
                        MailboxId = mailbox.Id,
                        Mailbox = mailbox
                    });
                }
                
                if (!mailbox.MailboxFolders.Any(f => f.Name == MailboxFolder.SENT))
                {
                    log.Information("Creating Sent folder for mailbox {mailboxName}", mailbox.Name);
                    dbContext.MailboxFolders.Add(new MailboxFolder
                    {
                        Name = MailboxFolder.SENT,
                        MailboxId = mailbox.Id,
                        Mailbox = mailbox
                    });
                }
            }

            //Find configuredMailboxesAndDefault in DB not in config and delete
            foreach (var dbMailbox in dbMailboxes)
            {
                if (!configuredMailboxesAndDefault.Any(m => m.Name == dbMailbox.Name))
                {
                    log.Information("Deleting mailbox {name}", dbMailbox.Name);
                    dbContext.Remove(dbMailbox);
                }
            }
            dbContext.SaveChanges();

            var defaultMailbox = dbContext.Mailboxes.FirstOrDefault(m => m.Name == MailboxOptions.DEFAULTNAME);
            foreach (var messageWithoutMailbox in dbContext.Messages.Where(m => m.Mailbox == null))
            {
                messageWithoutMailbox.Mailbox = defaultMailbox;
            }
            
            // Migrate existing messages without folder assignment to INBOX
            var messagesWithoutFolders = dbContext.Messages
                .Include(m => m.Mailbox)
                .ThenInclude(mb => mb.MailboxFolders)
                .Where(m => m.MailboxFolder == null && m.Mailbox != null)
                .ToList();
                
            foreach (var message in messagesWithoutFolders)
            {
                var inboxFolder = message.Mailbox.MailboxFolders.FirstOrDefault(f => f.Name == MailboxFolder.INBOX);
                if (inboxFolder != null)
                {
                    message.MailboxFolder = inboxFolder;
                    message.MailboxFolderId = inboxFolder.Id;
                }
            }
            dbContext.SaveChanges();

            TrimMessages(dbContext, dbMailboxes);
            dbContext.SaveChanges();

            TrimSessions(dbContext);
            dbContext.SaveChanges();

            this.notificationsHub.OnMessagesChanged("*").Wait();
            this.notificationsHub.OnSessionsChanged().Wait();
            this.notificationsHub.OnMailboxesChanged().Wait();

        }

        private async Task OnAuthenticationCredentialsValidationRequired(object sender, AuthenticationCredentialsValidationEventArgs e)
        {


            var sessionId = activeSessionsToDbId[e.Session];
            using var scope = serviceScopeFactory.CreateScope();
            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            var session = dbContext.Sessions.Single(s => s.Id == sessionId);

            var apiSession = new ApiModel.Session(session);

            AuthenticationResult? result = scriptingHost.ValidateCredentials(apiSession, e.Credentials, e.Connection);

            if (result == null && this.serverOptions.CurrentValue.SmtpAllowAnyCredentials)
            {
                this.log.Information("SMTP authentication successful - any credentials allowed. ClientAddress: {clientAddress}", 
                    e.Session.ClientAddress);
                result = AuthenticationResult.Success;
            }

            if (result == null)
            {
                // Check if credentials support OAuth2 token validation
                if (e.Credentials is IAuthenticationCredentialsCanValidateWithToken tokenCreds)
                {
                    // OAuth2/XOAUTH2 validation
                    var authority = serverOptions.CurrentValue.OAuth2Authority;
                    var audience = serverOptions.CurrentValue.OAuth2Audience;
                    var issuer = serverOptions.CurrentValue.OAuth2Issuer;

                    if (!string.IsNullOrWhiteSpace(authority))
                    {
                        // Validate token with IDP
                        var (isValid, subject, error) = await oauth2TokenValidator.ValidateTokenAsync(
                            tokenCreds.AccessToken, 
                            authority, 
                            audience, 
                            issuer);

                        if (isValid)
                        {
                            // Check if subject matches username (case-insensitive)
                            if (subject.Equals(tokenCreds.Username, StringComparison.OrdinalIgnoreCase))
                            {
                                // Check if username exists in configured users list
                                var user = serverOptions.CurrentValue.Users.FirstOrDefault(u => u.Username.Equals(tokenCreds.Username, StringComparison.OrdinalIgnoreCase));
                                if (user != null)
                                {
                                    result = AuthenticationResult.Success;
                                    this.log.Information("SMTP OAuth2 authentication successful. Username: {username}, Subject: {subject}, ClientAddress: {clientAddress}", 
                                        tokenCreds.Username, subject, e.Session.ClientAddress);
                                }
                                else
                                {
                                    result = AuthenticationResult.Failure;
                                    this.log.Warning("SMTP OAuth2 authentication failed - username not in configured users list. Username: {username}, Subject: {subject}, ClientAddress: {clientAddress}", 
                                        tokenCreds.Username, subject, e.Session.ClientAddress);
                                }
                            }
                            else
                            {
                                result = AuthenticationResult.Failure;
                                this.log.Warning("SMTP OAuth2 authentication failed - subject mismatch. Username: {username}, Subject: {subject}, ClientAddress: {clientAddress}", 
                                    tokenCreds.Username, subject, e.Session.ClientAddress);
                            }
                        }
                        else
                        {
                            result = AuthenticationResult.Failure;
                            this.log.Warning("SMTP OAuth2 authentication failed - token validation failed. Username: {username}, Error: {error}, ClientAddress: {clientAddress}", 
                                tokenCreds.Username, error, e.Session.ClientAddress);
                        }
                    }
                    else
                    {
                        // OAuth2Authority not configured - fail authentication
                        result = AuthenticationResult.Failure;
                        this.log.Warning("SMTP OAuth2 authentication failed - OAuth2Authority not configured. Username: {username}, ClientAddress: {clientAddress}", 
                            tokenCreds.Username, e.Session.ClientAddress);
                    }
                }
                else if (e.Credentials is IAuthenticationCredentialsCanValidateWithPassword val)
                {
                    // Password-based validation (PLAIN, LOGIN, CRAM-MD5)
                    var user = serverOptions.CurrentValue.Users.FirstOrDefault(u => u.Username.Equals(val.Username, StringComparison.CurrentCultureIgnoreCase));
                    if (user != null && val.ValidateResponse(user.Password))
                    {
                        result = AuthenticationResult.Success;
                        this.log.Information("SMTP authentication successful. Username: {username}, ClientAddress: {clientAddress}", 
                            val.Username, e.Session.ClientAddress);

                    }
                    else
                    {
                        result = AuthenticationResult.Failure;
                        this.log.Warning("SMTP authentication failed - invalid credentials. Username: {username}, ClientAddress: {clientAddress}", 
                            val.Username, e.Session.ClientAddress);
                    }
                }
                else
                {
                    result = AuthenticationResult.Failure;
                    this.log.Warning("SMTP authentication failed - unsupported credential type. CredentialType: {credentialType}, ClientAddress: {clientAddress}", 
                        e.Credentials.Type, e.Session.ClientAddress);
                }
            }

            e.AuthenticationResult = result.Value;
        }


        private readonly IOptionsMonitor<Settings.ServerOptions> serverOptions;
        private readonly IOptionsMonitor<RelayOptions> relayOptions;
        private readonly IDictionary<ISession, Guid> activeSessionsToDbId = new Dictionary<ISession, Guid>();
        private readonly ScriptingHost scriptingHost;

        private static async Task UpdateDbSession(ISession session, Session dbSession)
        {
            dbSession.StartDate = session.StartDate;
            dbSession.EndDate = session.EndDate;
            dbSession.ClientAddress = session.ClientAddress.ToString();
            dbSession.ClientName = session.ClientName;
            dbSession.NumberOfMessages = (await session.GetMessages()).Count;
            dbSession.Log = (await session.GetLog()).ReadToEnd();
            dbSession.SessionErrorType = session.SessionErrorType;
            dbSession.SessionError = session.SessionError?.Message;
        }

        private async Task OnSessionStarted(object sender, SessionEventArgs e)
        {
            log.Information("SMTP session started. ClientAddress: {clientAddress}", 
                e.Session.ClientAddress);
            await taskQueue.QueueTask(() =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();

                Session dbSession = new Session();
                UpdateDbSession(e.Session, dbSession).Wait();
                dbContext.Sessions.Add(dbSession);
                dbContext.SaveChanges();

                activeSessionsToDbId[e.Session] = dbSession.Id;
                
                notificationsHub.OnSessionUpdated(dbSession.Id).Wait();
            }, false).ConfigureAwait(false);
        }

        private async Task OnSessionCompleted(object sender, SessionEventArgs e)
        {
            int messageCount = (await e.Session.GetMessages()).Count;
            var duration = e.Session.EndDate.HasValue 
                ? (e.Session.EndDate.Value - e.Session.StartDate).TotalMilliseconds 
                : 0;
            log.Information("SMTP session completed. ClientAddress: {clientAddress}, MessageCount: {messageCount}, Duration: {duration}ms", 
                e.Session.ClientAddress, messageCount, duration);


            await taskQueue.QueueTask(() =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();

                Session dbSession = dbContext.Sessions.Find(activeSessionsToDbId[e.Session]);
                UpdateDbSession(e.Session, dbSession).Wait();
                dbContext.SaveChanges();

                TrimSessions(dbContext);
                dbContext.SaveChanges();

                activeSessionsToDbId.Remove(e.Session);

                notificationsHub.OnSessionUpdated(dbSession.Id).Wait();
                notificationsHub.OnSessionsChanged().Wait();
            }, false).ConfigureAwait(false);
        }


        public Task DeleteSession(Guid id)
        {
            return taskQueue.QueueTask(() =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
                Session session = dbContext.Sessions.SingleOrDefault(s => s.Id == id);
                if (session != null)
                {
                    dbContext.Sessions.Remove(session);
                    dbContext.SaveChanges();
                    notificationsHub.OnSessionsChanged().Wait();
                }
            }, true);
        }

        public Task DeleteAllSessions()
        {
            return taskQueue.QueueTask(() =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
                dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.EndDate.HasValue));
                dbContext.SaveChanges();
                notificationsHub.OnSessionsChanged().Wait();
            }, true);
        }

        private async Task OnMessageReceived(object sender, MessageEventArgs e)
        {
            log.Information("SMTP message received. ClientAddress: {clientAddress}, From: {messageFrom}, To: {messageTo}, SecureConnection: {secure}, DeclaredSize: {size}",
                e.Message.Session.ClientAddress, e.Message.From, 
                string.Join(", ", e.Message.Recipients), e.Message.SecureConnection, e.Message.DeclaredMessageSize);

            var targetMailboxes = await GetTargetMailboxes(e.Message.Recipients, e.Message.Session, e.Message);

            if (!targetMailboxes.Any())
            {
                log.Warning("Message delivery failed - no matching mailboxes. Recipients: {recipients}, ClientAddress: {clientAddress}", 
                    string.Join(", ", e.Message.Recipients), e.Message.Session.ClientAddress);
                return;
            }

            foreach (var targetMailboxWithMatchedRecipients in targetMailboxes)
            {
                using var scope = serviceScopeFactory.CreateScope();
                var mimeProcessingService = scope.ServiceProvider.GetService<MimeProcessingService>();
                Message message = new MessageConverter(mimeProcessingService).ConvertAsync(e.Message, targetMailboxWithMatchedRecipients.ToArray()).Result;
                message.IsUnread = true;

                await taskQueue.QueueTask(() => ProcessMessage(message, e.Message.Session, targetMailboxWithMatchedRecipients), false).ConfigureAwait(false);
            }
        }

        private async Task<ILookup<MailboxOptions, string>> GetTargetMailboxes(IEnumerable<string> recipients, ISession messageSession, IMessage message)
        {
            if (serverOptions.CurrentValue.DeliverMessagesToUsersDefaultMailbox && messageSession.Authenticated && messageSession.AuthenticationCredentials is IAuthenticationCredentialsCanValidateWithPassword credentials)
            {
                UserOptions userOptions = serverOptions.CurrentValue.Users.FirstOrDefault(u => string.Equals(u.Username, credentials.Username, StringComparison.OrdinalIgnoreCase));
                MailboxOptions defaultMailboxOptions = new MailboxOptions { Name = MailboxOptions.DEFAULTNAME, Recipients = "*" };
                MailboxOptions mailboxOption = defaultMailboxOptions;

                if (userOptions != null)
                {

                    String userDefaultMailbox = userOptions.DefaultMailbox;
                    mailboxOption = serverOptions.CurrentValue.Mailboxes.FirstOrDefault(m => string.Equals(m.Name, userDefaultMailbox, StringComparison.OrdinalIgnoreCase));

                    if (mailboxOption == null)
                    {
                        log.Warning("Mailbox '{userDefaultMailbox}' was not found. Falling back to default mailbox", userDefaultMailbox);
                        mailboxOption = defaultMailboxOptions;
                    }
                }

                    return recipients.ToLookup(_ => mailboxOption, recipient => recipient);
            }

            // Parse message headers for header-based filtering
            Dictionary<string, string> messageHeaders = null;
            bool headersNeeded = serverOptions.CurrentValue.Mailboxes.Any(m => m.HeaderFilters != null && m.HeaderFilters.Length > 0);
            
            if (headersNeeded)
            {
                messageHeaders = await ParseMessageHeaders(message);
            }

            // Extract source information from the session
            string clientHostname = messageSession.ClientName;
            string clientAddress = messageSession.ClientAddress.ToString();

            // Use the router to find mailboxes for each recipient
            var mailboxes = serverOptions.CurrentValue.Mailboxes.Concat(new[] { new MailboxOptions { Name = MailboxOptions.DEFAULTNAME, Recipients = "*" } });
            
            List<(MailboxOptions,string)> targetMailboxesWithMatchedRecipient = new List<(MailboxOptions, string)>();
            foreach (var to in recipients)
            {
                var targetMailbox = mailboxRouter.FindMailboxForRecipient(to, mailboxes, clientHostname, clientAddress, messageHeaders);

                if (targetMailbox != null)
                {
                    targetMailboxesWithMatchedRecipient.Add((targetMailbox, to));
                }
                else
                {
                    log.Warning("Message recipient {recipient} did not match any mailbox recipients", to);
                }
            }

            return targetMailboxesWithMatchedRecipient.ToLookup(t => t.Item1, t=> t.Item2);
        }

        private async Task<Dictionary<string, string>> ParseMessageHeaders(IMessage message)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                using (Stream messageData = await message.GetData())
                {
                    // Use MimeKit to parse headers efficiently
                    // The 'true' parameter tells MimeKit to parse only headers, not the entire message body,
                    // which is more efficient for routing decisions
                    var mimeMessage = await MimeMessage.LoadAsync(messageData, true);
                    
                    foreach (var header in mimeMessage.Headers)
                    {
                        // Store only the first occurrence of each header (case-insensitive)
                        if (!headers.ContainsKey(header.Field))
                        {
                            headers[header.Field] = header.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warning(ex, "Failed to parse message headers for filtering; falling back to recipient-based routing only");
            }
            
            return headers;
        }


        private bool ShouldDeliverToStdout(string mailboxName)
        {
            var deliverToStdout = serverOptions.CurrentValue.DeliverToStdout;
            
            if (string.IsNullOrWhiteSpace(deliverToStdout))
            {
                return false;
            }

            // If "*", deliver all mailboxes to stdout
            if (deliverToStdout.Trim() == "*")
            {
                return true;
            }

            // Check if mailbox name is in the comma-separated list
            var mailboxNames = deliverToStdout.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim());
            
            return mailboxNames.Contains(mailboxName, StringComparer.OrdinalIgnoreCase);
        }

        void ProcessMessage(Message message, ISession session, IGrouping<MailboxOptions, string> targetMailboxWithRecipients)
        {
            log.Information("Processing received message for mailbox '{mailbox}' for recipients '{recipients}'", targetMailboxWithRecipients.Key.Name, targetMailboxWithRecipients.ToArray());
            using var scope = serviceScopeFactory.CreateScope();
            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            
            message.Session = dbContext.Sessions.Find(activeSessionsToDbId[session]);
            message.Mailbox = dbContext.Mailboxes.FirstOrDefault(m => m.Name == targetMailboxWithRecipients.Key.Name);
            
            // Assign message to INBOX folder by default for SMTP received messages
            if (message.Mailbox != null)
            {
                var inboxFolder = dbContext.MailboxFolders.FirstOrDefault(f => f.MailboxId == message.Mailbox.Id && f.Name == MailboxFolder.INBOX);
                if (inboxFolder != null)
                {
                    message.MailboxFolder = inboxFolder;
                    message.MailboxFolderId = inboxFolder.Id;
                }
            }
            
            var relayResult = TryRelayMessage(message, null);
            message.RelayError = string.Join("\n", relayResult.Exceptions.Select(e => e.Key + ": " + e.Value.Message));
            
            ImapState imapState = dbContext.ImapState.Single();
            imapState.LastUid = Math.Max(0, imapState.LastUid + 1);
            message.ImapUid = imapState.LastUid;
            if (relayResult.WasRelayed)
            {
                foreach (var relay in relayResult.RelayRecipients)
                {
                    message.AddRelay(new MessageRelay { SendDate = DateTime.UtcNow, To = relay.Email });
                }
            }
            
            dbContext.Messages.Add(message);
            
            // Update session warnings if message has bare line feeds
            if (message.HasBareLineFeed)
            {
                message.Session.HasBareLineFeed = true;
            }
            
            dbContext.SaveChanges();
            
            TrimMessages(dbContext, new List<Mailbox>() {message.Mailbox});
            dbContext.SaveChanges();
            notificationsHub.OnMessagesChanged(targetMailboxWithRecipients.Key.Name).Wait();
            log.Information("Message processing completed. MessageId: {messageId}, Mailbox: {mailbox}, ImapUid: {imapUid}", 
                message.Id, message.Mailbox.Name, message.ImapUid);

            // Deliver to stdout if configured for this mailbox
            if (ShouldDeliverToStdout(targetMailboxWithRecipients.Key.Name))
            {
                lock (stdoutLock)
                {
                    // Output the raw message content to stdout
                    // Use a delimiter that is very unlikely to appear in email messages
                    Console.WriteLine("--- BEGIN SMTP4DEV MESSAGE ---");
                    if (message.Data != null)
                    {
                        // Write raw message bytes to stdout
                        using (var stdout = Console.OpenStandardOutput())
                        {
                            stdout.Write(message.Data, 0, message.Data.Length);
                            stdout.Flush();
                        }
                        Console.WriteLine(); // Ensure delimiter is on new line
                    }
                    Console.WriteLine("--- END SMTP4DEV MESSAGE ---");
                    Console.Out.Flush();

                    messagesDeliveredToStdoutCount++;
                    
                    // Check if we should exit after delivering this message
                    var exitAfter = serverOptions.CurrentValue.ExitAfterMessages;
                    if (exitAfter.HasValue && messagesDeliveredToStdoutCount >= exitAfter.Value)
                    {
                        log.Information("Delivered {count} messages to stdout. Exiting as configured by ExitAfterMessages.", messagesDeliveredToStdoutCount);
                        // Schedule application exit on a background thread to allow this method to complete
                        Task.Run(async () =>
                        {
                            await Task.Delay(100); // Give time for logs to flush
                            Environment.Exit(0);
                        });
                    }
                }
            }
        }

        public RelayResult TryRelayMessage(Message message, MailboxAddress[] overrideRecipients)
        {
            var result = new RelayResult();

            if (!relayOptions.CurrentValue.IsEnabled)
            {
                return result;
            }

            List<MailboxAddress> recipients = new List<MailboxAddress>();

            if (overrideRecipients == null)
            {
                recipients.AddRange(message.To
                    .Split(",")
                    .Select(r => MailboxAddress.Parse(r))
                    .Where(r => relayOptions.CurrentValue.AutomaticEmails.Contains(r.Address, StringComparer.OrdinalIgnoreCase))
                );


                var apiMessage = new ApiModel.Message(message);
                var apiSession = new ApiModel.Session(message.Session);
                foreach (string recipient in message.To.Split(','))
                {
                    recipients.AddRange(scriptingHost.GetAutoRelayRecipients(apiMessage, recipient, apiSession)
                        .Select(r => MailboxAddress.Parse(r)));
                }
            }
            else
            {
                recipients.AddRange(overrideRecipients);
            }

            foreach (MailboxAddress recipient in recipients.DistinctBy(r => r.Address))
            {
                try
                {
                    log.Information("Relaying message. Recipient: {recipient}, MessageId: {messageId}, RelayServer: {relayServer}", 
                        recipient.Address, message.Id, relayOptions.CurrentValue.SmtpServer);

                    using SmtpClient relaySmtpClient = relaySmtpClientFactory(relayOptions.CurrentValue);

                    if (relaySmtpClient == null)
                    {
                        throw new ApplicationException("Relay server options are incomplete.");
                    }

                    var apiMsg = new ApiModel.Message(message);
                    MimeMessage newEmail = apiMsg.MimeMessage;
                    
                    // Determine the sender address - use custom if configured, otherwise use original
                    bool hasCustomSenderAddress = !string.IsNullOrEmpty(relayOptions.CurrentValue.SenderAddress);
                    MailboxAddress sender = MailboxAddress.Parse(
                        hasCustomSenderAddress
                            ? relayOptions.CurrentValue.SenderAddress
                            : apiMsg.From);
                    
                    // Update the From header if a custom sender address is configured
                    // This is required for SMTP servers like AWS SES that validate the From header matches the envelope sender
                    if (hasCustomSenderAddress)
                    {
                        newEmail.From.Clear();
                        newEmail.From.Add(sender);
                    }
                    
                    relaySmtpClient.Send(newEmail, sender, new[] { recipient });
                    result.RelayRecipients.Add(new RelayRecipientResult() { Email = recipient.Address, RelayDate = DateTime.UtcNow });
                    relaySmtpClient.Disconnect(true);
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed to relay message. Recipient: {recipient}, MessageId: {messageId}, Exception: {exceptionType}", 
                        recipient.Address, message.Id, e.GetType().Name);
                    result.Exceptions[recipient] = e;
                }
            }

            return result;
        }

        private void TrimMessages(Smtp4devDbContext dbContext, IEnumerable<Mailbox> mailboxes)
        {
            foreach (var mailbox in mailboxes)
            {
                dbContext.Messages
                    .Where(m => m.Mailbox == mailbox)
                    .OrderByDescending(m => m.ReceivedDate)
                    .Skip(serverOptions.CurrentValue.NumberOfMessagesToKeep)
                    .ExecuteDelete();
            }
        }

        private void TrimSessions(Smtp4devDbContext dbContext)
        {
            dbContext.Sessions.RemoveRange(dbContext.Sessions.Where(s => s.EndDate.HasValue).OrderByDescending(m => m.EndDate)
                .Skip(serverOptions.CurrentValue.NumberOfSessionsToKeep));
        }


        private readonly ITaskQueue taskQueue;
        private Rnwood.SmtpServer.SmtpServer smtpServer;
        private readonly Func<RelayOptions, SmtpClient> relaySmtpClientFactory;
        private readonly NotificationsHub notificationsHub;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private Settings.ServerOptions lastStartOptions;
        private int messagesDeliveredToStdoutCount = 0;
        private readonly object stdoutLock = new object();

        public Exception Exception { get; private set; }

        public bool IsRunning => this.smtpServer?.IsRunning ?? false;

        public IPEndPoint[] ListeningEndpoints => this.smtpServer?.ListeningEndpoints ?? [];

        public void TryStart()
        {
            try
            {
                this.Exception = null;
                this.lastStartOptions = this.serverOptions.CurrentValue with { };

                DoCleanup();
                CreateSmtpServer();
                smtpServer.Start();

                foreach (var l in smtpServer.ListeningEndpoints)
                {
                    log.Information("SMTP Server is listening on port {smtpPortNumber} ({address}) with TLS mode ({tlsMode}).",
                        l.Port, l.Address, this.lastStartOptions.TlsMode);
                }

                log.Information("Keeping last {messagesToKeep} messages per mailbox and {sessionsToKeep} sessions.",
                    serverOptions.CurrentValue.NumberOfMessagesToKeep, serverOptions.CurrentValue.NumberOfSessionsToKeep);
            }
            catch (Exception e)
            {
                log.Fatal(e, "SMTP server failed to start. TlsMode: {tlsMode}, Port: {port}, AllowRemoteConnections: {allowRemote}, Exception: {exceptionType}", 
                    this.lastStartOptions?.TlsMode, this.lastStartOptions?.Port, this.lastStartOptions?.AllowRemoteConnections, e.GetType().Name);
                this.Exception = e;
            }
            finally
            {
                this.notificationsHub.onServerChanged().Wait();
            }
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            this.TryStart();
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            this.Stop();
            return Task.CompletedTask;
        }

        public void Send(IDictionary<string, string> headers, string[] to, string[] cc, string from, string[] envelopeRecipients, string subject, string bodyHtml, IEnumerable<AttachmentInfo> attachments = null)
        {
            MailboxAddress sender = MailboxAddress.Parse(from);
            var relaySmtpClient = this.relaySmtpClientFactory(this.relayOptions.CurrentValue);

            if (relaySmtpClient == null)
            {
                throw new InvalidOperationException("Relay SMTP server must be configued to send messages.");
            }

            MimeMessage message = new MimeMessage();
            message.Subject = subject;
            message.MessageId = $"<{Guid.NewGuid()}@{this.serverOptions.CurrentValue.HostName}>";
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = bodyHtml;
            
            // Add attachments if provided
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                }
            }
            
            message.Body = bodyBuilder.ToMessageBody();
            foreach (var kvp in headers)
            {
                message.Headers.Add(kvp.Key, kvp.Value);
            }

            message.From.Add(InternetAddress.Parse(from));

            foreach (var toItem in to)
            {
                message.To.Add(InternetAddress.Parse(toItem));
            }

            foreach (var toItem in cc)
            {
                message.Cc.Add(InternetAddress.Parse(toItem));
            }

            relaySmtpClient.Send(message, MailboxAddress.Parse(from), envelopeRecipients.Select(t => MailboxAddress.Parse(t)));
        }
    }
}