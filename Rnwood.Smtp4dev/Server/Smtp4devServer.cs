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
using Microsoft.Extensions.DependencyInjection;
using Rnwood.Smtp4dev.Data;
using Serilog;

namespace Rnwood.Smtp4dev.Server
{
    public class Smtp4devServer : ISmtp4devServer
    {
        private readonly ILogger log = Log.ForContext<Smtp4devServer>();

        public Smtp4devServer(IServiceScopeFactory serviceScopeFactory, IOptionsMonitor<ServerOptions> serverOptions,
            IOptionsMonitor<RelayOptions> relayOptions, NotificationsHub notificationsHub, Func<RelayOptions, SmtpClient> relaySmtpClientFactory,
            ITaskQueue taskQueue)
        {
            this.notificationsHub = notificationsHub;
            this.serverOptions = serverOptions;
            this.relayOptions = relayOptions;
            this.serviceScopeFactory = serviceScopeFactory;
            this.relaySmtpClientFactory = relaySmtpClientFactory;
            this.taskQueue = taskQueue;

            DoCleanup();

            IDisposable eventHandler = null;
            var obs = Observable.FromEvent<ServerOptions>(e => eventHandler = serverOptions.OnChange(e), e => eventHandler.Dispose());
            obs.Throttle(TimeSpan.FromMilliseconds(100)).Subscribe(OnServerOptionsChanged);

            taskQueue.Start();
        }

        private void OnServerOptionsChanged(ServerOptions arg1)
        {
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
            X509Certificate2 cert = GetTlsCertificate();

            ServerOptions serverOptionsValue = serverOptions.CurrentValue;
            this.smtpServer = new DefaultServer(serverOptionsValue.AllowRemoteConnections, serverOptionsValue.HostName, serverOptionsValue.Port,
                serverOptionsValue.TlsMode == TlsMode.ImplicitTls ? cert : null,
                serverOptionsValue.TlsMode == TlsMode.StartTls ? cert : null
            );
            this.smtpServer.MessageReceivedEventHandler += OnMessageReceived;
            this.smtpServer.SessionCompletedEventHandler += OnSessionCompleted;
            this.smtpServer.SessionStartedHandler += OnSessionStarted;
            this.smtpServer.AuthenticationCredentialsValidationRequiredEventHandler += OnAuthenticationCredentialsValidationRequired;
            this.smtpServer.IsRunningChanged += ((_, __) =>
            {
                if (this.smtpServer.IsRunning) return;
                log.Information("SMTP server stopped.");
                this.notificationsHub.OnServerChanged().Wait();
            });
        }

        public void Stop()
        {
            log.Information("SMTP server stopping...");
            this.smtpServer.Stop(true);
        }

        private X509Certificate2 GetTlsCertificate()
        {
            X509Certificate2 cert = null;

            log.Information("TLS mode: {TLSMode}", serverOptions.CurrentValue.TlsMode);

            if (serverOptions.CurrentValue.TlsMode != TlsMode.None)
            {
                if (!string.IsNullOrEmpty(serverOptions.CurrentValue.TlsCertificate))
                {
                    var pfxPassword = serverOptions.CurrentValue.TlsCertificatePassword ?? "";

                    if (string.IsNullOrEmpty(serverOptions.CurrentValue.TlsCertificatePrivateKey))
                    {
                        cert = CertificateHelper.LoadCertificate(serverOptions.CurrentValue.TlsCertificate, pfxPassword);
                    }
                    else
                    {
                        cert = CertificateHelper.LoadCertificateWithKey(serverOptions.CurrentValue.TlsCertificate,
                            serverOptions.CurrentValue.TlsCertificatePrivateKey, pfxPassword);
                    }

                    log.Information("Using provided certificate with Subject {SubjectName}, expiry {ExpiryDate}", cert.SubjectName.Name,
                        cert.GetExpirationDateString());
                }
                else
                {
                    string pfxPath = Path.GetFullPath("selfsigned-certificate.pfx");
                    string cerPath = Path.GetFullPath("selfsigned-certificate.cer");

                    if (File.Exists(pfxPath))
                    {
                        cert = new X509Certificate2(File.ReadAllBytes(pfxPath), "",
                            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                        if (cert.Subject != $"CN={serverOptions.CurrentValue.HostName}" ||
                            DateTime.Parse(cert.GetExpirationDateString()) < DateTime.Now.AddDays(30))
                        {
                            cert = null;
                        }
                        else
                        {
                            log.Information(
                                "Using existing self-signed certificate with subject name {Hostname} and expiry date {ExpirationDate}",
                                serverOptions.CurrentValue.HostName,
                                cert.GetExpirationDateString());
                        }
                    }

                    if (cert == null)
                    {
                        cert = SSCertGenerator.CreateSelfSignedCertificate(serverOptions.CurrentValue.HostName);
                        File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pkcs12));
                        File.WriteAllBytes(cerPath, cert.Export(X509ContentType.Cert));
                        log.Information("Generated new self-signed certificate with subject name '{Hostname} and expiry date {ExpirationDate}",
                            serverOptions.CurrentValue.HostName,
                            cert.GetExpirationDateString());
                    }


                    log.Information(
                        "Ensure that the hostname you enter into clients and '{Hostname}' from ServerOptions:HostName configuration match exactly and trust the issuer certificate at {cerPath} in your client/OS to avoid certificate validation errors.",
                        serverOptions.CurrentValue.HostName, cerPath);
                }
            }

            return cert;
        }

        private void DoCleanup()
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
        }

        private Task OnAuthenticationCredentialsValidationRequired(object sender, AuthenticationCredentialsValidationEventArgs e)
        {
            e.AuthenticationResult = AuthenticationResult.Success;
            return Task.CompletedTask;
        }


        private readonly IOptionsMonitor<ServerOptions> serverOptions;
        private readonly IOptionsMonitor<RelayOptions> relayOptions;
        private readonly IDictionary<ISession, Guid> activeSessionsToDbId = new Dictionary<ISession, Guid>();

        private static async Task UpdateDbSession(ISession session, Session dbSession)
        {
            dbSession.StartDate = session.StartDate;
            dbSession.EndDate = session.EndDate;
            dbSession.ClientAddress = session.ClientAddress.ToString();
            dbSession.ClientName = session.ClientName;
            dbSession.NumberOfMessages = (await session.GetMessages()).Count;
            dbSession.Log = (await session.GetLog()).ReadToEnd();
            dbSession.SessionErrorType = session.SessionErrorType;
            dbSession.SessionError = session.SessionError?.ToString();
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

            await taskQueue.QueueTask(() =>
            {
                log.Information("Processing received message");
                using var scope = serviceScopeFactory.CreateScope();
                Smtp4devDbContext dbContext = scope.ServiceProvider.GetService<Smtp4devDbContext>();

                var relayResult = TryRelayMessage(message, null);
                message.RelayError = string.Join("\n", relayResult.Exceptions.Select(e => e.Key + ": " + e.Value.Message));

                ImapState imapState = dbContext.ImapState.Single();
                imapState.LastUid = Math.Max(0, imapState.LastUid + 1);
                message.ImapUid = imapState.LastUid;
                message.Session = dbContext.Sessions.Find(activeSessionsToDbId[e.Message.Session]);
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
            }, false).ConfigureAwait(false);
        }

        public RelayResult TryRelayMessage(Message message, MailboxAddress[] overrideRecipients)
        {
            var result = new RelayResult(message);

            if (!relayOptions.CurrentValue.IsEnabled)
            {
                return result;
            }

            MailboxAddress[] recipients;

            if (overrideRecipients == null)
            {
                recipients = message.To
                    .Split(",")
                    .Select(r => MailboxAddress.Parse(r))
                    .Where(r => relayOptions.CurrentValue.AutomaticEmails.Contains(r.Address, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }
            else
            {
                recipients = overrideRecipients;
            }

            foreach (MailboxAddress recipient in recipients)
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
        private DefaultServer smtpServer;
        private Func<RelayOptions, SmtpClient> relaySmtpClientFactory;
        private NotificationsHub notificationsHub;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public Exception Exception { get; private set; }

        public bool IsRunning
        {
            get { return this.smtpServer.IsRunning; }
        }

        public int PortNumber
        {
            get { return this.smtpServer.PortNumber; }
        }

        public void TryStart()
        {
            try
            {
                this.Exception = null;

                CreateSmtpServer();
                smtpServer.Start();

                log.Information("SMTP Server is listening on port {smtpPortNumber}.",
                    smtpServer.PortNumber);
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
    }
}