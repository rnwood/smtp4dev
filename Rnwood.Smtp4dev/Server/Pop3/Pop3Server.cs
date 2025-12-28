namespace Rnwood.Smtp4dev.Server.Pop3
{
	using System;
	using System.Buffers;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft.Extensions.Hosting;
	using Microsoft.Extensions.Logging;
	using Microsoft.Extensions.Options;
	using Microsoft.Extensions.DependencyInjection;
	using Rnwood.Smtp4dev.Service;
	using Rnwood.Smtp4dev.Server.Pop3.CommandHandlers;
	using Rnwood.Smtp4dev.Server.Settings;
	using Rnwood.Smtp4dev.Data;
	using System.Security.Cryptography.X509Certificates;
	using Rnwood.Smtp4dev.Server;
	using System.Net.Security;
	using System.Security.Authentication;

	public class Pop3Server : IHostedService, IDisposable
	{
		private TcpListener[] listeners;
		private readonly IOptionsMonitor<ServerOptions> optionsMonitor;
		private readonly ILogger<Pop3Server> logger;
		private readonly IServiceScopeFactory serviceScopeFactory;
		private readonly IServiceProvider serviceProvider; // optional - used to resolve command handlers from DI
		private CancellationTokenSource cts;

		// Optional handler overrides provided as a single dictionary rather than many ctor params
		private readonly IReadOnlyDictionary<string, ICommandHandler> handlerOverrides;

		// Test-friendly constructor (keeps backward compatibility for tests that prefer not to build a service provider)
		public Pop3Server(IOptionsMonitor<ServerOptions> optionsMonitor, ILogger<Pop3Server> logger, IServiceScopeFactory serviceScopeFactory)
		{
			this.optionsMonitor = optionsMonitor;
			this.logger = logger;
			this.serviceScopeFactory = serviceScopeFactory;
			this.serviceProvider = null;
			this.handlerOverrides = null;
		}

		// DI constructor - accepts IServiceProvider and an optional handler overrides dictionary
		internal Pop3Server(IOptionsMonitor<ServerOptions> optionsMonitor, ILogger<Pop3Server> logger, IServiceScopeFactory serviceScopeFactory, IServiceProvider serviceProvider,
					 IDictionary<string, ICommandHandler> handlerOverrides = null)
		: this(optionsMonitor, logger, serviceScopeFactory)
		{
			this.serviceProvider = serviceProvider;

			// capture any handler overrides (may be null)
			this.handlerOverrides = handlerOverrides != null
				? new Dictionary<string, ICommandHandler>(handlerOverrides, StringComparer.OrdinalIgnoreCase)
				: null;
		}

		public int[] ListeningPorts
		{
			get
			{
				try
				{
					if (listeners != null && listeners.Length > 0)
					{
						return listeners.Select(l => ((IPEndPoint)l.LocalEndpoint).Port).ToArray();
					}
				}
				catch { }
				return new[] { (optionsMonitor.CurrentValue.Pop3Port ?? 110) };
			}
		}

		private volatile bool isRunning;
		public bool IsRunning => isRunning;

		public void TryStart()
		{
			// Start in background and don't wait
			_ = StartAsync(CancellationToken.None);
		}

		public void Stop()
		{
			try
			{
				StopAsync(CancellationToken.None).GetAwaiter().GetResult();
			}
			catch { }
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			// Create listeners now based on options
			var opts = optionsMonitor.CurrentValue;
			
			if (!opts.Pop3Port.HasValue)
			{
				logger.LogInformation("POP3 server disabled - no port configured");
				return Task.CompletedTask;
			}
			
			int port = opts.Pop3Port.Value;

			// Determine a specific bind address if configured
			IPAddress bindAddress = null;
			if (!string.IsNullOrWhiteSpace(opts.BindAddress))
			{
				if (!IPAddress.TryParse(opts.BindAddress, out bindAddress))
				{
					throw new ArgumentException($"Invalid bind address: {opts.BindAddress}");
				}
			}

			if (bindAddress != null)
			{
				// Use the specific bind address when configured
				listeners = new[] { new TcpListener(bindAddress, port) };
			}
			else if (opts.AllowRemoteConnections)
			{
				if (!opts.DisableIPv6)
				{
					// Prefer IPv6Any with DualMode enabled; fall back to IPv4Any if IPv6 not supported
					listeners = TryCreateListenersWithFallback(
						() => new[] { CreateTcpListenerWithDualMode(IPAddress.IPv6Any, port) },
						() => new[] { new TcpListener(IPAddress.Any, port) }
					);
				}
				else
				{
					listeners = new[] { new TcpListener(IPAddress.Any, port) };
				}
			}
			else
			{
				// Loopback only
				if (!opts.DisableIPv6)
				{
					listeners = TryCreateListenersWithFallback(
						() => new[] { CreateTcpListenerWithDualMode(IPAddress.IPv6Loopback, port) },
						() => new[] { new TcpListener(IPAddress.Loopback, port) }
					);
				}
				else
				{
					listeners = new[] { new TcpListener(IPAddress.Loopback, port) };
				}
			}

			try
			{
				// Start all listeners
				foreach (var l in listeners)
				{
					l.Start();
				}
				isRunning = true;

				// Start accept loops for each listener
				foreach (var l in listeners)
				{
					_ = Task.Run(() => AcceptLoopAsync(l, cts.Token), cts.Token);
				}

				// Log listening endpoints (if available)
				foreach (var l in listeners)
				{
					try
					{
						var localEp = l.LocalEndpoint as IPEndPoint;
						if (localEp != null)
						{
							logger.LogInformation($"POP3 Server is listening on port {localEp.Port} ({localEp.Address}) with TLS mode {this.optionsMonitor.CurrentValue.Pop3TlsMode}");
						}
					}
					catch { }
				}
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressFamilyNotSupported && listeners != null && listeners.Any(l => l.LocalEndpoint == null))
			{
				// IPv6 not supported, stop and rethrow to let fallback logic in TryCreateListenersWithFallback handle
				foreach (var l in listeners)
				{
					try { l.Stop(); } catch { }
				}
				logger.LogWarning("IPv6 not supported when starting POP3 listeners (AddressFamilyNotSupported)");
				throw;
			}

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				cts?.Cancel();
				if (listeners != null)
				{
					foreach (var l in listeners)
					{
						try { l.Stop(); } catch { }
					}
				}
			}
			catch { }
			finally
			{
				isRunning = false;
			}
			logger.LogInformation("POP3 server stopped");
			return Task.CompletedTask;
		}

		private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				TcpClient client = null;
				try
				{
					client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
					try
					{
						var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
						logger.LogInformation("POP3 accepted TCP connection from {remote}", remote);
					}
					catch { }
					_ = Task.Run(() => HandleClientAsync(client, cancellationToken));
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					// shutting down
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error accepting POP3 client");
					client?.Close();
				}
			}
		}

		private async Task HandleClientAsync(TcpClient client, CancellationToken serverCancellationToken)
		{
			try
			{
				using (client)
				using (var networkStream = client.GetStream())
				{
					Stream activeStream = networkStream;
					StreamWriter writer = new StreamWriter(activeStream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
					StreamReader reader = new StreamReader(activeStream, Encoding.ASCII);

					// Create a scoped repository for the duration of this session
					using var scope = serviceScopeFactory.CreateScope();
					var repo = scope.ServiceProvider.GetRequiredService<IMessagesRepository>();
					var sessionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
					var ctx = new Pop3SessionContext
					{
						Stream = activeStream,
						Writer = writer,
						Reader = reader,
						MessagesRepository = repo,
						Logger = logger,
						Options = optionsMonitor.CurrentValue,
						CancellationToken = sessionTokenSource.Token,
						ScopeFactory = this.serviceScopeFactory
					};

					// Log session start so E2E runs can be correlated with client connections
					try
					{
						var remoteEp = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
						logger.LogInformation("POP3 session starting. Client address {clientAddress}, Pop3TlsMode: {pop3Tls}", remoteEp, ctx.Options.Pop3TlsMode);
					}
					catch { }

					// session started

					// If implicit TLS is configured for POP3, perform TLS handshake immediately and replace the active stream/reader/writer
					if (ctx.Options.Pop3TlsMode == TlsMode.ImplicitTls)
					{
						try
						{
							var cert = Rnwood.Smtp4dev.Server.CertificateHelper.GetTlsCertificate(ctx.Options, Serilog.Log.ForContext<Pop3Server>());
							// Prefer Pop3-specific certificate selection
							// Note: use GetTlsCertificateForPop3 if available to respect Pop3TlsMode
							// (fall back to GetTlsCertificate where needed)
							try
							{
								var pop3Cert = Rnwood.Smtp4dev.Server.CertificateHelper.GetTlsCertificateForPop3(ctx.Options, Serilog.Log.ForContext<Pop3Server>());
								if (pop3Cert != null) cert = pop3Cert;
							}
							catch { }
							if (cert == null)
							{
								logger?.LogWarning("POP3 implicit TLS requested but no TLS certificate available");
								// For implicit TLS we must not write plaintext responses (client expects TLS immediately) — close the connection
								return;
							}

							var ssl = new SslStream(networkStream, leaveInnerStreamOpen: false);
							var sslOptions = new SslServerAuthenticationOptions
							{
								ServerCertificate = cert,
								EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
								CertificateRevocationCheckMode = X509RevocationMode.NoCheck
							};

							try
							{
								// Diagnostic file logging for integration test debugging
								await ssl.AuthenticateAsServerAsync(sslOptions, sessionTokenSource.Token).ConfigureAwait(false);
								logger.LogInformation("POP3 implicit TLS handshake completed for client {clientAddress}", client.Client.RemoteEndPoint);
							}
							catch (Exception ex)
							{
								// Don't attempt to write plaintext to a client that expected TLS; just log and close the connection
								logger?.LogWarning(ex, "POP3 implicit TLS handshake failed");
								return;
							}

							// Swap to SSL stream for the session
							activeStream = ssl;
							writer = new StreamWriter(ssl, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
							reader = new StreamReader(ssl, Encoding.ASCII);

							ctx.Stream = activeStream;
							ctx.Writer = writer;
							ctx.Reader = reader;
							ctx.IsSecure = true;
						}
						catch (Exception ex)
						{
							// Don't attempt to write plaintext to a client that expected TLS; just log and close the connection
							logger?.LogWarning(ex, "POP3 implicit TLS handshake failed");
							return;
						}
					}

					try
					{
						await ctx.WriteLineAsync($"+OK smtp4dev POP3 server ready");

						var handlers = CreateHandlers();

						while (!sessionTokenSource.Token.IsCancellationRequested)
						{
							var line = await ctx.ReadLineAsync().ConfigureAwait(false);
							// received line
							if (line == null)
								break;
							var parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
							var verb = parts[0].ToUpperInvariant();
							var arg = parts.Length > 1 ? parts[1] : null;

							// Dispatch to handler by looking up the verb in the handlers dictionary
							if (!handlers.TryGetValue(verb, out var handler))
							{
								handler = handlers["UNKNOWN"];
							}

							await ExecuteHandlerSafe(handler, ctx, arg, sessionTokenSource.Token).ConfigureAwait(false);
						}
					}
					catch (OperationCanceledException)
					{
						// expected to end session
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "POP3 session error");
					}
					finally
					{
						sessionTokenSource.Cancel();
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "POP3 session setup failed");
			}
		}

		private IDictionary<string, ICommandHandler> CreateHandlers()
		{
			// Build handlers dictionary preferring handlerOverrides, then direct construction
			var handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase)
			{
				{"USER", GetOverrideOrDefault("USER", () => new UserCommand())},
				{"PASS", GetOverrideOrDefault("PASS", () => new PassCommand())},
				{"STAT", GetOverrideOrDefault("STAT", () => new StatCommand())},
				{"LIST", GetOverrideOrDefault("LIST", () => new ListCommand())},
				{"RETR", GetOverrideOrDefault("RETR", () => new RetrCommand())},
				{"UIDL", GetOverrideOrDefault("UIDL", () => new UidlCommand())},
				{"DELE", GetOverrideOrDefault("DELE", () => new DeleCommand())},
				{"RSET", GetOverrideOrDefault("RSET", () => new RsetCommand())},
				{"QUIT", GetOverrideOrDefault("QUIT", () => new QuitCommand())},
			};

			// STLS handler is only registered when StartTls is configured for POP3 (implicit TLS doesn't use the STLS command)
			if (optionsMonitor.CurrentValue.Pop3TlsMode == TlsMode.StartTls)
			{
				handlers["STLS"] = GetOverrideOrDefault("STLS", () => new StlsCommand(logger));
			}

			handlers["CAPA"] = GetOverrideOrDefault("CAPA", () => new CapaCommand());
			handlers["UNKNOWN"] = GetOverrideOrDefault("UNKNOWN", () => new UnknownCommand());

			return handlers;
		}

		private ICommandHandler GetOverrideOrDefault(string verb, Func<ICommandHandler> factory)
		{
			if (handlerOverrides != null && handlerOverrides.TryGetValue(verb, out var h))
				return h;
			return factory();
		}

		private async Task ExecuteHandlerSafe(ICommandHandler handler, Pop3SessionContext ctx, string arg, CancellationToken token)
		{
			try
			{
				await handler.ExecuteAsync(ctx, arg, token).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw; // preserve cancellation signaling
			}
			catch (Exception ex)
			{
				try
				{
					logger.LogError(ex, "POP3 command handler failed");
					await ctx.WriteLineAsync("-ERR Internal server error");
				}
				catch (Exception inner)
				{
					logger.LogError(inner, "POP3 handler error while reporting error");
				}
			}
		}

		public void Dispose()
		{
			if (listeners != null)
			{
				foreach (var l in listeners)
				{
					try { l.Stop(); } catch { }
				}
			}
			cts?.Dispose();
		}

		/// <summary>
		/// Tries to create listeners with IPv6, falls back to IPv4 if IPv6 is not supported
		/// </summary>
		private TcpListener[] TryCreateListenersWithFallback(Func<IEnumerable<TcpListener>> primaryFactory, Func<IEnumerable<TcpListener>> fallbackFactory)
		{
			try
			{
				logger.LogDebug("Attempting to create POP3 listeners");
				var primaryListener = primaryFactory();
				return primaryListener.ToArray();
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressFamilyNotSupported)
			{
				logger.LogWarning("Error during POP3 listener creation (AddressFamilyNotSupported), falling back to IPv4 only");
			}

			logger.LogInformation("Creating POP3 fallback listeners");
			return fallbackFactory().ToArray();
		}

		/// <summary>
		/// Creates a TcpListener with DualMode enabled for IPv6
		/// </summary>
		private TcpListener CreateTcpListenerWithDualMode(IPAddress address, int port)
		{
			var listener = new TcpListener(address, port);
			try
			{
				listener.Server.DualMode = true;
			}
			catch { }
			return listener;
		}
	}
}
