﻿using LumiSoft.Net;
using LumiSoft.Net.IMAP.Server;
using LumiSoft.Net.MIME;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Net.NetworkInformation;
using Serilog;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Org.BouncyCastle.Utilities.Net;
using Rnwood.Smtp4dev.Server.Settings;
using DeepEqual.Syntax;
using MailKit.Net.Imap;

namespace Rnwood.Smtp4dev.Server
{
    public partial class ImapServer : IHostedService
    {
        public ImapServer(IOptionsMonitor<ServerOptions> serverOptions, ScriptingHost scriptingHost, IServiceScopeFactory serviceScopeFactory)
        {
            this.serverOptions = serverOptions;
            this.serviceScopeFactory = serviceScopeFactory;
            this.scriptingHost = scriptingHost;

            IDisposable eventHandler = null;
            var obs = Observable.FromEvent<ServerOptions>(e => eventHandler = serverOptions.OnChange(e), e => eventHandler.Dispose());
            obs.Throttle(TimeSpan.FromMilliseconds(100)).Subscribe(OnServerOptionsChanged);

        }

        private void OnServerOptionsChanged(ServerOptions serverOptions)
        {
            if (serverOptions.IsDeepEqual(this.lastStartOptions))
            {
                return;
            }

            if (IsRunning)
            {
                Stop();

                TryStart();
            }
        }

        public bool IsRunning
        {
            get
            {
                return imapServer?.IsRunning ?? false;
            }
        }

        public async void TryStart()
        {
            this.lastStartOptions = serverOptions.CurrentValue with { };

            if (!serverOptions.CurrentValue.ImapPort.HasValue)
            {
                log.Information("IMAP server disabled - no port configured");
                return;
            }


            List<IPBindInfo> bindings = new List<IPBindInfo>();

            // Check if a specific bind address is configured
            System.Net.IPAddress bindAddress = null;
            if (!string.IsNullOrWhiteSpace(serverOptions.CurrentValue.BindAddress))
            {
                if (!System.Net.IPAddress.TryParse(serverOptions.CurrentValue.BindAddress, out bindAddress))
                {
                    log.Error("Invalid IMAP bind address configured: {bindAddress}", serverOptions.CurrentValue.BindAddress);
                    throw new ArgumentException($"Invalid bind address: {serverOptions.CurrentValue.BindAddress}");
                }
            }

            if (bindAddress != null)
            {
                // Use the specific bind address when configured
                bindings.Add(new IPBindInfo(serverOptions.CurrentValue.HostName, BindInfoProtocol.TCP, bindAddress, serverOptions.CurrentValue.ImapPort.Value));
            }
            else if (serverOptions.CurrentValue.AllowRemoteConnections)
            {
                if (!serverOptions.CurrentValue.DisableIPv6)
                {
                    // Add IPv6 binding first, IPv4 fallback will be handled by the LumiSoft TCP_Server error handling
                    bindings.Add(new IPBindInfo(serverOptions.CurrentValue.HostName, BindInfoProtocol.TCP, System.Net.IPAddress.IPv6Any, serverOptions.CurrentValue.ImapPort.Value));
                    // Add IPv4 as fallback in case IPv6 fails
                    bindings.Add(new IPBindInfo(serverOptions.CurrentValue.HostName, BindInfoProtocol.TCP, System.Net.IPAddress.Any, serverOptions.CurrentValue.ImapPort.Value));
                }
                else
                {
                    bindings.Add(new IPBindInfo(serverOptions.CurrentValue.HostName, BindInfoProtocol.TCP, System.Net.IPAddress.Any, serverOptions.CurrentValue.ImapPort.Value));

                }
            }
            else
            {
                bindings.Add(new IPBindInfo(serverOptions.CurrentValue.HostName, BindInfoProtocol.TCP, System.Net.IPAddress.Loopback, serverOptions.CurrentValue.ImapPort.Value));

                if (!serverOptions.CurrentValue.DisableIPv6)
                {
                    // Add IPv6 loopback first, IPv4 loopback already added above as fallback
                    bindings.Add(new IPBindInfo(serverOptions.CurrentValue.HostName, BindInfoProtocol.TCP, System.Net.IPAddress.IPv6Loopback, serverOptions.CurrentValue.ImapPort.Value));
                }
            }

            imapServer = new IMAP_Server()
            {

                Bindings = bindings.ToArray(),
                GreetingText = "smtp4dev"
            };
            imapServer.SessionCreated += (o, ea) => new SessionHandler(ea.Session, scriptingHost, serverOptions, this.serviceScopeFactory);


            var errorTcs = new TaskCompletionSource<Error_EventArgs>();
            imapServer.Error += (s, ea) =>
            {
                if (!errorTcs.Task.IsCompleted)
                {
                    errorTcs.TrySetResult(ea);
                }
            };

            var startedTcs = new TaskCompletionSource<EventArgs>();
            imapServer.Started += (s, ea) => startedTcs.SetResult(ea);

            imapServer.Start();

            var errorTask = errorTcs.Task;
            var startedTask = startedTcs.Task;

            int index = Task.WaitAny(startedTask, errorTask, Task.Delay(TimeSpan.FromSeconds(30)));

            if (index == 1)
            {
                log.Error(errorTask.Result.Exception, "IMAP server failed to start. Port: {port}, BindAddress: {bindAddress}, ExceptionType: {exceptionType}",
                    serverOptions.CurrentValue.ImapPort, serverOptions.CurrentValue.BindAddress ?? "Any", 
                    errorTask.Result.Exception.GetType().Name);
            }
            else if (index == 2)
            {
                log.Error("IMAP server failed to start - timeout after 30 seconds. Port: {port}", 
                    serverOptions.CurrentValue.ImapPort);

                try
                {
                    imapServer.Stop();
                }
                catch { }
            }
            else
            {
                //Race condition in IMAP server - it fires the running event before this is all populated (or replaced from prev start).
                while (imapServer.ListeningPoints.Length < imapServer.Bindings.Length && !imapServer.ListeningPoints.All(lp =>
                {
                    try
                    {
                        return lp.Socket.LocalEndPoint != null;
                    }
                    catch (ObjectDisposedException)
                    {
                        return false;
                    }
                }))
                {
                    await Task.Delay(100);
                }

                foreach (var lp in imapServer.ListeningPoints)
                {
                    var ep = ((IPEndPoint)lp.Socket.LocalEndPoint);
                    int port = ep.Port;
                    log.Information("IMAP Server is listening on port {port} ({address})", port, ep.Address);
                }

            }
        }

        public void Stop()
        {
            imapServer?.Stop();
            imapServer = null;
        }

        private IMAP_Server imapServer;
        private IOptionsMonitor<ServerOptions> serverOptions;
        private ServerOptions lastStartOptions;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ScriptingHost scriptingHost;
        private readonly ILogger log = Log.ForContext<ImapServer>();

        private void Logger_WriteLog(object sender, LumiSoft.Net.Log.WriteLogEventArgs e)
        {
            log.Information(e.LogEntry.Text);
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            this.TryStart();
            return Task.CompletedTask;

        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => this.Stop());
            return Task.CompletedTask;
        }
    }
}
