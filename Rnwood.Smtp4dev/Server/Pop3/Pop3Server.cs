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
		private readonly TcpListener listener;
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
			this.listener = new TcpListener(IPAddress.Any, optionsMonitor.CurrentValue.Pop3Port ?? 110);
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
					if (listener?.LocalEndpoint is IPEndPoint ep)
					{
						return new[] { ep.Port };
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
			listener.Start();
			isRunning = true;
			_ = Task.Run(() => AcceptLoopAsync(cts.Token));
			logger.LogInformation("POP3 server started");
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				cts?.Cancel();
				listener.Stop();
			}
			catch { }
			finally
			{
				isRunning = false;
			}
			logger.LogInformation("POP3 server stopped");
			return Task.CompletedTask;
		}

		private async Task AcceptLoopAsync(CancellationToken cancellationToken)
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

							switch (verb)
							{
								case "USER": await ExecuteHandlerSafe(handlers["USER"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "PASS": await ExecuteHandlerSafe(handlers["PASS"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "STAT": await ExecuteHandlerSafe(handlers["STAT"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "LIST": await ExecuteHandlerSafe(handlers["LIST"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "RETR": await ExecuteHandlerSafe(handlers["RETR"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "UIDL": await ExecuteHandlerSafe(handlers["UIDL"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "DELE": await ExecuteHandlerSafe(handlers["DELE"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "RSET": await ExecuteHandlerSafe(handlers["RSET"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "QUIT": await ExecuteHandlerSafe(handlers["QUIT"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "STLS": await ExecuteHandlerSafe(handlers["STLS"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								case "CAPA": await ExecuteHandlerSafe(handlers["CAPA"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
								default: await ExecuteHandlerSafe(handlers["UNKNOWN"], ctx, arg, sessionTokenSource.Token).ConfigureAwait(false); break;
							}
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
			if (serviceProvider != null)
			{
				// Resolve handlers from DI when available
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

			// Fallback - create handlers directly
			var handlersDirect = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase)
			{
				{"USER", new UserCommand()},
				{"PASS", new PassCommand()},
				{"STAT", new StatCommand()},
				{"LIST", new ListCommand()},
				{"RETR", new RetrCommand()},
				{"UIDL", new UidlCommand()},
				{"DELE", new DeleCommand()},
				{"RSET", new RsetCommand()},
				{"QUIT", new QuitCommand()},
				{"STLS", new StlsCommand(logger)},
				{"CAPA", new CapaCommand()},
				{"UNKNOWN", new UnknownCommand()}
			};

			return handlersDirect;
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
			listener?.Stop();
			cts?.Dispose();
		}
	}
}
