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

	public class Pop3Server : IHostedService, IDisposable
	{
		private TcpListener[] listeners;
		private readonly IOptionsMonitor<ServerOptions> optionsMonitor;
		private readonly ILogger<Pop3Server> logger;
		private readonly IServiceScopeFactory serviceScopeFactory;
		private readonly IServiceProvider serviceProvider; // optional - used to resolve command handlers from DI
		private CancellationTokenSource cts;

		// Test-friendly constructor (keeps backward compatibility for tests that prefer not to build a service provider)
		public Pop3Server(IOptionsMonitor<ServerOptions> optionsMonitor, ILogger<Pop3Server> logger, IServiceScopeFactory serviceScopeFactory)
		{
			this.optionsMonitor = optionsMonitor;
			this.logger = logger;
			this.serviceScopeFactory = serviceScopeFactory;
			this.serviceProvider = null;
			// listener creation moved to StartAsync to support IPv6/dual-stack/fallback
		}

		// DI constructor - accepts IServiceProvider so handlers can be resolved from the container
		public Pop3Server(IOptionsMonitor<ServerOptions> optionsMonitor, ILogger<Pop3Server> logger, IServiceScopeFactory serviceScopeFactory, IServiceProvider serviceProvider)
		: this(optionsMonitor, logger, serviceScopeFactory)
		{
			this.serviceProvider = serviceProvider;
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
			int port = opts.Pop3Port ?? 110;

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
						() => new[] { CreateTcpListenerWithDualMode(IPAddress.IPv6Loopback, port), new TcpListener(IPAddress.Loopback, port) },
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
							logger.LogInformation($"POP3 server is listening on port {localEp.Port} ({localEp.Address})");
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
				using (var writer = new StreamWriter(networkStream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true })
				using (var reader = new StreamReader(networkStream, Encoding.ASCII))
				{
					// Create a scoped repository for the duration of this session
					using var scope = serviceScopeFactory.CreateScope();
					var repo = scope.ServiceProvider.GetRequiredService<IMessagesRepository>();
					var sessionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
					var ctx = new Pop3SessionContext
					{
						Stream = networkStream,
						Writer = writer,
						Reader = reader,
						MessagesRepository = repo,
						Logger = logger,
						Options = optionsMonitor.CurrentValue,
						CancellationToken = sessionTokenSource.Token
					};

					try
					{
						await ctx.WriteLineAsync($"+OK smtp4dev POP3 server ready");

						var handlers = CreateHandlers();

						while (!sessionTokenSource.Token.IsCancellationRequested)
						{
							var line = await ctx.ReadLineAsync().ConfigureAwait(false);
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
			// Require handlers be resolved from the configured IServiceProvider
			if (serviceProvider == null)
			{
				throw new InvalidOperationException("Pop3Server requires an IServiceProvider to resolve command handlers. Construct the server using the DI-enabled constructor.");
			}

			var sp = serviceProvider;
			var handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase)
			{
				{"USER", sp.GetRequiredService<UserCommand>()},
				{"PASS", sp.GetRequiredService<PassCommand>()},
				{"STAT", sp.GetRequiredService<StatCommand>()},
				{"LIST", sp.GetRequiredService<ListCommand>()},
				{"RETR", sp.GetRequiredService<RetrCommand>()},
				{"UIDL", sp.GetRequiredService<UidlCommand>()},
				{"DELE", sp.GetRequiredService<DeleCommand>()},
				{"RSET", sp.GetRequiredService<RsetCommand>()},
				{"QUIT", sp.GetRequiredService<QuitCommand>()},
				{"STLS", new StlsCommand(logger)},
				{"CAPA", sp.GetRequiredService<CapaCommand>()},
				{"UNKNOWN", sp.GetRequiredService<UnknownCommand>()}
			};

			return handlers;
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
