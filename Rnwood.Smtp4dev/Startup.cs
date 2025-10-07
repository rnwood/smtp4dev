using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Server;
using VueCliMiddleware;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SpaServices;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Rewrite;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Service;
using Serilog;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Rnwood.Smtp4dev.Server.Settings;
using Microsoft.AspNetCore.Authorization;
using AspNetCore.Authentication.Basic;
using System.Reflection;

namespace Rnwood.Smtp4dev
{
    public class Startup
    {
        private const string InMemoryDbConnString = "Data Source=file:cachedb?mode=memory&cache=shared";
        private SqliteConnection keepAliveConnection;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Validates that the database version is compatible with the current application version.
        /// Throws an exception if the database has migrations that are newer than what this application knows about.
        /// </summary>
        /// <param name="context">The database context to check</param>
        private static void ValidateDatabaseVersionCompatibility(Smtp4devDbContext context)
        {
            // Skip check for in-memory databases as they're always created fresh
            if (context.Database.IsInMemory())
            {
                return;
            }

            // Get all migrations that have been applied to the database
            var appliedMigrations = context.Database.GetAppliedMigrations().ToList();

            // Get all migrations available in the current application
            var availableMigrations = context.Database.GetMigrations().ToList();

            // Find any applied migrations that are not available in the current code
            var unknownMigrations = appliedMigrations.Except(availableMigrations).ToList();

            if (unknownMigrations.Any())
            {
                var unknownMigrationsList = string.Join(", ", unknownMigrations);
                throw new InvalidOperationException(
                    $"Database version mismatch detected. The database contains migrations that are not recognized by this version of smtp4dev: {unknownMigrationsList}. " +
                    "This usually happens when you're running an older version of smtp4dev against a database that was upgraded by a newer version. " +
                    "To resolve this issue: " +
                    "1. Upgrade to a newer version of smtp4dev that supports these migrations, or " +
                    "2. If you want to use this version and don't need to preserve existing data, delete the database file and restart smtp4dev to create a new one.");
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "v1";
                config.Title = "smtp4dev";
                config.Version = "v1";
                config.PostProcess = d =>
                {
                    //Remove the JSON content type from the actions where it is not supported.
                    NSwag.OpenApiOperationDescription sendOp = d.Operations.FirstOrDefault(o => o.Path.EndsWith("/send") || o.Path.EndsWith("/reply"));
                    sendOp.Operation.RequestBody.Content.Remove("application/json");

                };
            });

            services.Configure<ServerOptions>(Configuration.GetSection("ServerOptions"));
            services.Configure<RelayOptions>(Configuration.GetSection("RelayOptions"));
            services.Configure<ClientOptions>(Configuration.GetSection("ClientOptions"));
            services.Configure<DesktopOptions>(Configuration.GetSection("DesktopOptions"));

            ServerOptions serverOptions = Configuration.GetSection("ServerOptions").Get<ServerOptions>();

            services.AddDbContext<Smtp4devDbContext>(opt =>
                    {
                        if (string.IsNullOrEmpty(serverOptions.Database))
                        {
                            Log.Logger.Information("Using in-memory database for message storage");

                            //Must be held open to keep the memory DB alive
                            keepAliveConnection = new SqliteConnection(InMemoryDbConnString);
                            keepAliveConnection.Open();
                            opt.UseSqlite(InMemoryDbConnString);
                        }
                        else
                        {
                            var dbLocation = Path.GetFullPath(serverOptions.Database);
                            if (serverOptions.RecreateDb && File.Exists(dbLocation))
                            {
                                Log.Logger.Information("Recreating database. Location: {dbLocation}", dbLocation);
                                File.Delete(dbLocation);
                            }

                            Log.Logger.Information("Using SQLite database. Location: {dbLocation}, FileExists: {fileExists}", 
                                dbLocation, File.Exists(dbLocation));

                            opt.UseSqlite($"Data Source={dbLocation}");
                        }



                        using var context = new Smtp4devDbContext((DbContextOptions<Smtp4devDbContext>)opt.Options);

                        // Validate database version compatibility before attempting any operations
                        ValidateDatabaseVersionCompatibility(context);

                        if (string.IsNullOrEmpty(serverOptions.Database))
                        {
                            context.Database.Migrate();
                            context.SaveChanges();
                        }
                        else
                        {

                            var pendingMigrations = context.Database.GetPendingMigrations();
                            if (pendingMigrations.Any())
                            {
                                Log.Logger.Information("Applying database migrations. MigrationCount: {count}, Migrations: {migrations}", 
                                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                                context.Database.Migrate();
                                context.SaveChanges();
                                Log.Logger.Information("Database migrations completed successfully");
                            }
                        }

                        if (!context.ImapState.Any())
                        {
                            context.Add(new ImapState
                            {
                                Id = Guid.Empty,
                                LastUid = 1
                            });
                            context.SaveChanges();
                        }

                        //For message before delivered to was added, assume all recipients.
                        foreach (var m in context.Messages.Where(m => m.DeliveredTo == null))
                        {
                            m.DeliveredTo = m.To;
                        }
                        context.SaveChanges();

                        // Populate MIME metadata for existing messages synchronously during startup
                        var messagesWithoutMetadata = context.Messages
                            .Where(m => string.IsNullOrEmpty(m.MimeMetadata) || string.IsNullOrEmpty(m.BodyText))
                            .ToList();

                        if (messagesWithoutMetadata.Any())
                        {
                            Log.Logger.Information("Populating MIME metadata for {count} existing messages during startup", messagesWithoutMetadata.Count);
                            var mimeProcessingService = new MimeProcessingService();

                            int processed = 0;
                            int batchSize = 50; // Process in batches to avoid memory issues

                            foreach (var batch in messagesWithoutMetadata.Chunk(batchSize))
                            {
                                foreach (var message in batch)
                                {
                                    try
                                    {
                                        var (mimeMetadata, bodyText) = mimeProcessingService.ExtractMimeDataFromMessage(message);
                                        message.MimeMetadata = JsonSerializer.Serialize(mimeMetadata);
                                        message.BodyText = bodyText;
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Logger.Warning(ex, "Failed to extract MIME metadata. MessageId: {messageId}, ExceptionType: {exceptionType}", 
                                            message.Id, ex.GetType().Name);
                                        // Set fallback values
                                        message.MimeMetadata = JsonSerializer.Serialize(new Server.MimeMetadata());
                                        message.BodyText = Encoding.UTF8.GetString(message.Data);
                                    }
                                }

                                context.SaveChanges();
                                processed += batch.Length;
                                Log.Logger.Information("Processed {processed}/{total} messages", processed, messagesWithoutMetadata.Count);
                            }

                            Log.Logger.Information("Successfully populated MIME metadata for all existing messages during startup");
                        }


                    }, ServiceLifetime.Scoped, ServiceLifetime.Singleton);


            services.AddSingleton<ISmtp4devServer, Smtp4devServer>();
            services.AddSingleton<ImapServer>();
            services.AddSingleton<Rnwood.Smtp4dev.Server.Pop3.Pop3Server>();
            services.AddScoped<IMessagesRepository, MessagesRepository>();
            services.AddScoped<IHostingEnvironmentHelper, HostingEnvironmentHelper>();
            services.AddSingleton<ITaskQueue, TaskQueue>();
            services.AddSingleton<ScriptingHost>();
            services.AddScoped<MimeProcessingService>();
            services.AddSingleton(Program.ServerLogService);

            services.AddSingleton<Func<RelayOptions, SmtpClient>>(relayOptions =>
            {
                if (string.IsNullOrEmpty(relayOptions.SmtpServer))
                {
                    return null;
                }

                SmtpClient result = new SmtpClient();
                result.Connect(relayOptions.SmtpServer, relayOptions.SmtpPort, relayOptions.TlsMode);

                if (!string.IsNullOrEmpty(relayOptions.Login))
                {
                    result.Authenticate(relayOptions.Login, relayOptions.Password);
                }

                return result;
            });


            services.AddSignalR();
            services.AddSingleton<NotificationsHub>();



            services.AddControllers(o =>
            {
                o.InputFormatters.Add(new HtmlBodyInputFormatter());
            });
            services.AddRequestLocalization(options => { options.SupportedCultures = CultureInfo.GetCultures(CultureTypes.AllCultures); });

            services.AddSpaStaticFiles(o => o.RootPath = "ClientApp");

            services.AddAuthentication(BasicDefaults.AuthenticationScheme)
            .AddBasic<UserValidationService>(options => { options.Realm = "smtp4dev"; });


            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });

            services.AddScoped<IAuthorizationHandler, UserValidationService>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ServerOptions serverOptions = Configuration.GetSection("ServerOptions").Get<ServerOptions>();

            // Wire up server log notifications
            var serverLogService = app.ApplicationServices.GetRequiredService<ServerLogService>();
            var notificationsHub = app.ApplicationServices.GetRequiredService<NotificationsHub>();
            serverLogService.LogReceived += async (sender, logEntry) =>
            {
                await notificationsHub.OnServerLogReceived(logEntry);
            };

            app.UseRouting();

            Action<IApplicationBuilder> configure = subdir =>
            {
                subdir.UseOpenApi(c =>
                {
                    c.Path = "/api/{documentName}/swagger.json";
                });
                subdir.UseSwaggerUi(c =>
                {
                    c.Path = "/api";
                    c.DocumentPath = "/api/{documentName}/swagger.json";
                    c.DocumentTitle = "smtp4dev API";
                });
                subdir.UseRouting();
                subdir.UseAuthentication();
                subdir.UseAuthorization();
                subdir.UseDeveloperExceptionPage();
                subdir.UseDefaultFiles();
                subdir.UseStaticFiles();
                subdir.UseSpaStaticFiles();
                subdir.UseRequestLocalization();

                subdir.UseWebSockets();

                subdir.UseEndpoints(e =>
                {
                    e.MapHub<NotificationsHub>("/hubs/notifications");

                    subdir.Use(async (context, next) =>
                     {
                         try
                         {
                             await next(context);
                         }
                         catch (FileNotFoundException ex)
                         {
                             context.Response.StatusCode = 404;
                             await context.Response.WriteAsync(ex.Message);

                         }
                     });
                    e.MapControllers();

#if DEBUG
                    if (env.IsDevelopment())
                    {

                        e.MapToVueCliProxy(
                            "{*path}",
                            new SpaOptions { SourcePath = Path.Join(env.ContentRootPath, "ClientApp"), DevServerPort = 5173 },
                            npmScript: "dev",
                            regex: "VITE.*ready in",
                            forceKill: true,
                            port: 5173
                        );

                    }
#endif
                });
            };

            if (!string.IsNullOrEmpty(serverOptions.BasePath) && serverOptions.BasePath != "/")
            {
                string basePathNoSlash = serverOptions.BasePath.TrimEnd('/');
                string redirectTarget = basePathNoSlash + "/";

                // Global middleware to redirect /smtp4dev to /smtp4dev/
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path.Equals(basePathNoSlash, StringComparison.OrdinalIgnoreCase)
                        && !context.Request.Path.Value.EndsWith("/"))
                    {
                        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
                        context.Response.Redirect(redirectTarget + queryString, true);
                        return;
                    }
                    else if (context.Request.Path.Value.Equals("/")
                        || context.Request.Path.Value == String.Empty)
                    {
                        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
                        context.Response.Redirect(redirectTarget + queryString, true);
                        return;
                    }
                    await next();
                });

                app.Map(serverOptions.BasePath.TrimEnd('/'), configure);
            }
            else
            {
                configure(app);
            }
        }
    }
}