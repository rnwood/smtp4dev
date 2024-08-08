using System;
using System.Globalization;
using System.IO;
using System.Net;
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
                            Log.Logger.Information("Using in memory database.");

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
                                Log.Logger.Information("Deleting Sqlite database.");
                                File.Delete(dbLocation);
                            }

                            Log.Logger.Information("Using Sqlite database at {dbLocation}", dbLocation);

                            opt.UseSqlite($"Data Source={dbLocation}");
                        }



                        using var context = new Smtp4devDbContext((DbContextOptions<Smtp4devDbContext>)opt.Options);
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
                                Log.Logger.Information("Updating DB schema with migrations: {migrations}", string.Join(", ", pendingMigrations));
                                context.Database.Migrate();
                                context.SaveChanges();
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


                    }, ServiceLifetime.Scoped, ServiceLifetime.Singleton);


            services.AddSingleton<ISmtp4devServer, Smtp4devServer>();
            services.AddSingleton<ImapServer>();
            services.AddScoped<IMessagesRepository, MessagesRepository>();
            services.AddScoped<IHostingEnvironmentHelper, HostingEnvironmentHelper>();
            services.AddSingleton<ITaskQueue, TaskQueue>();
            services.AddSingleton<ScriptingHost>();

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
                });
            };

            if (!string.IsNullOrEmpty(serverOptions.BasePath) && serverOptions.BasePath != "/")
            {
                RewriteOptions rewrites = new RewriteOptions();
                rewrites.AddRedirect("^" + serverOptions.BasePath.TrimEnd('/') + "$", serverOptions.BasePath.TrimEnd('/') + "/");
                ;
                rewrites.AddRedirect("^(/)?$", serverOptions.BasePath.TrimEnd('/') + "/");
                ;
                app.UseRewriter(rewrites);

                app.Map(serverOptions.BasePath, configure);
            }
            else
            {
                configure(app);
            }
        }
    }
}