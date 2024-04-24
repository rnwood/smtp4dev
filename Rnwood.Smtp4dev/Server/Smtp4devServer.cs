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

namespace Rnwood.Smtp4dev.Server
{
    internal class Smtp4devServer : ISmtp4devServer, IHostedService
    {
        private readonly ILogger log = Log.ForContext<Smtp4devServer>();

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

            taskQueue.Start();
            QueueCleanup();
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
            if (arg1 .IsDeepEqual( this.lastStartOptions))
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
            this.smtpServer = new Rnwood.SmtpServer.SmtpServer(new SmtpServer.ServerOptions(serverOptionsValue.AllowRemoteConnections, !serverOptionsValue.DisableIPv6, serverOptionsValue.HostName, serverOptionsValue.Port, serverOptionsValue.AuthenticationRequired,
                serverOptionsValue.TlsMode == TlsMode.ImplicitTls ? cert : null,
                serverOptionsValue.TlsMode == TlsMode.StartTls ? cert : null
            ));
            this.smtpServer.MessageCompletedEventHandler += OnMessageCompleted;
            this.smtpServer.MessageReceivedEventHandler += OnMessageReceived;
            this.smtpServer.SessionCompletedEventHandler += OnSessionCompleted;
            this.smtpServer.SessionStartedHandler += OnSessionStarted;
            this.smtpServer.AuthenticationCredentialsValidationRequiredEventHandler += OnAuthenticationCredentialsValidationRequired;
            this.smtpServer.IsRunningChanged += OnIsRunningChanged;
            ((SmtpServer.ServerOptions)this.smtpServer.Options).MessageStartEventHandler += OnMessageStart;

            ((SmtpServer.ServerOptions)this.smtpServer.Options).MessageRecipientAddingEventHandler += OnMessageRecipientAddingEventHandler;
        }

        private Task OnMessageRecipientAddingEventHandler(object sender, RecipientAddingEventArgs e)
        {
            var sessionId = activeSessionsToDbId[e.Message.Session];
            using var scope = serviceScopeFactory.CreateScope();
            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            var session = dbContext.Sessions.AsNoTracking().Single(s => s.Id == sessionId);
            var apiSession = new ApiModel.Session(session);

            if (!this.scriptingHost.ValidateRecipient(apiSession, e.Recipient))
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
            this.notificationsHub.OnServerChanged().Wait();
        }

        private async Task OnMessageCompleted(object sender, ConnectionEventArgs e)
        {
            if (!scriptingHost.HasValidateMessageExpression)
            {
                return;
            }

            Message message = new MessageConverter().ConvertAsync(await e.Connection.CurrentMessage.ToMessage()).Result;

            var apiMessage = new ApiModel.Message(message);

            using var scope = serviceScopeFactory.CreateScope();
            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            Session dbSession = dbContext.Sessions.Find(activeSessionsToDbId[e.Connection.Session]);

            var apiSession = new ApiModel.Session(dbSession);

            var errorResponse = scriptingHost.ValidateMessage(apiMessage, apiSession);

            if (errorResponse != null)
            {
                throw new SmtpServerException(errorResponse);
            }
        }

        public void Stop()
        {
            log.Information("SMTP server stopping...");
            this.smtpServer.Stop(true);
        }


        private void QueueCleanup()
        {
            this.taskQueue.QueueTask(
                () =>
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();

                    foreach (Session unfinishedSession in dbContext.Sessions.Where(s => !s.EndDate.HasValue).ToArray())
                    {
                        unfinishedSession.EndDate = DateTime.Now;
                    }

                    dbContext.SaveChanges();

                    TrimMessages(dbContext);
                    dbContext.SaveChanges();

                    TrimSessions(dbContext);
                    dbContext.SaveChanges();

                    this.notificationsHub.OnMessagesChanged().Wait();
                    this.notificationsHub.OnSessionsChanged().Wait();
                }, true);
        }

        private Task OnAuthenticationCredentialsValidationRequired(object sender, AuthenticationCredentialsValidationEventArgs e)
        {
            

            var sessionId = activeSessionsToDbId[e.Session];
            using var scope = serviceScopeFactory.CreateScope();
            Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();
            var session = dbContext.Sessions.Single(s => s.Id == sessionId);

            var apiSession = new ApiModel.Session(session);

            AuthenticationResult? result = scriptingHost.ValidateCredentials(apiSession, e.Credentials);

            if (result == null && this.serverOptions.CurrentValue.SmtpAllowAnyCredentials)
            {
                this.log.Information("SMTP auth success (allow any credentials is on)");
                result = AuthenticationResult.Success;
            }

            if (result == null)
            {
                if (e.Credentials is IAuthenticationCredentialsCanValidateWithPassword val)
                {
                    var user = serverOptions.CurrentValue.Users.FirstOrDefault(u => u.Username.Equals(val.Username, StringComparison.CurrentCultureIgnoreCase));
                    if (user != null && val.ValidateResponse(user.Password))
                    {
                        result = AuthenticationResult.Success;
                        this.log.Information("SMTP auth success for user {user}", val.Username);

                    }
                    else
                    {
                        result = AuthenticationResult.Failure;
                        this.log.Warning("SMTP auth failure for user {user}", val.Username);
                    }
                }
                else
                {
                    result = AuthenticationResult.Failure;
                    this.log.Warning("SMTP auth failure: Cannot validate credentials of type {type}", e.Credentials.Type);
                }
            }

            e.AuthenticationResult = result.Value;
            return Task.CompletedTask;
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
            log.Information("Session started. Client address {clientAddress}.", e.Session.ClientAddress);
            await taskQueue.QueueTask(() =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();

                Session dbSession = new Session();
                UpdateDbSession(e.Session, dbSession).Wait();
                dbContext.Sessions.Add(dbSession);
                dbContext.SaveChanges();

                activeSessionsToDbId[e.Session] = dbSession.Id;
            }, false).ConfigureAwait(false);
        }

        private async Task OnSessionCompleted(object sender, SessionEventArgs e)
        {
            int messageCount = (await e.Session.GetMessages()).Count;
            log.Information("Session completed. Client address {clientAddress}. Number of messages {messageCount}.", e.Session.ClientAddress,
                messageCount);


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

            Message message = new MessageConverter().ConvertAsync(e.Message).Result;
            log.Information("Message received. Client address {clientAddress}, From {messageFrom}, To {messageTo}, SecureConnection: {secure}.",
                e.Message.Session.ClientAddress, e.Message.From, message.To, e.Message.SecureConnection);
            message.IsUnread = true;

            void ProcessMessage()
            {
                log.Information("Processing received message");
                using var scope = serviceScopeFactory.CreateScope();
                Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();

                message.Session = dbContext.Sessions.Find(activeSessionsToDbId[e.Message.Session]);
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

                dbContext.SaveChanges();

                TrimMessages(dbContext);
                dbContext.SaveChanges();
                notificationsHub.OnMessagesChanged().Wait();
                log.Information("Processing received message DONE");
            }

            await taskQueue.QueueTask(ProcessMessage, false).ConfigureAwait(false);
        }

        public RelayResult TryRelayMessage(Message message, MailboxAddress[] overrideRecipients)
        {
            var result = new RelayResult(message);

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
                    log.Information("Relaying message to {recipient}", recipient);

                    using SmtpClient relaySmtpClient = relaySmtpClientFactory(relayOptions.CurrentValue);
                    var apiMsg = new ApiModel.Message(message);
                    MimeMessage newEmail = apiMsg.MimeMessage;
                    MailboxAddress sender = MailboxAddress.Parse(
                        !string.IsNullOrEmpty(relayOptions.CurrentValue.SenderAddress)
                            ? relayOptions.CurrentValue.SenderAddress
                            : apiMsg.From);
                    relaySmtpClient.Send(newEmail, sender, new[] { recipient });
                    result.RelayRecipients.Add(new RelayRecipientResult() { Email = recipient.Address, RelayDate = DateTime.UtcNow });
                }
                catch (Exception e)
                {
                    log.Error(e, "Can not relay message to {recipient}: {errorMessage}", recipient, e.ToString());
                    result.Exceptions[recipient] = e;
                }
            }

            return result;
        }

        private void TrimMessages(Smtp4devDbContext dbContext)
        {
            dbContext.Messages.RemoveRange(dbContext.Messages.OrderByDescending(m => m.ReceivedDate)
                .Skip(serverOptions.CurrentValue.NumberOfMessagesToKeep));
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

        public Exception Exception { get; private set; }

        public bool IsRunning => this.smtpServer.IsRunning;

        public IPEndPoint[] ListeningEndpoints => this.smtpServer.ListeningEndpoints;

        public void TryStart()
        {
            try
            {
                this.Exception = null;
                this.lastStartOptions = this.serverOptions.CurrentValue with { };

                CreateSmtpServer();
                smtpServer.Start();

                foreach (var l in smtpServer.ListeningEndpoints)
                {
                    log.Information("SMTP Server is listening on port {smtpPortNumber} ({address}).",
                        l.Port, l.Address);
                }

                log.Information("Keeping last {messagesToKeep} messages and {sessionsToKeep} sessions.",
                    serverOptions.CurrentValue.NumberOfMessagesToKeep, serverOptions.CurrentValue.NumberOfSessionsToKeep);
            }
            catch (Exception e)
            {
                log.Fatal(e, "The SMTP server failed to start: {failureReason}", e.ToString());
                this.Exception = e;
            }
            finally
            {
                this.notificationsHub.OnServerChanged().Wait();
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
    }
}