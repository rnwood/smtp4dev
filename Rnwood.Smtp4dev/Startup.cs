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

namespace Rnwood.Smtp4dev
{
    public class Startup
    {
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
            });

            services.Configure<ServerOptions>(Configuration.GetSection("ServerOptions"));
            services.Configure<RelayOptions>(Configuration.GetSection("RelayOptions"));
            services.Configure<ClientOptions>(Configuration.GetSection("ClientOptions"));

            ServerOptions serverOptions = Configuration.GetSection("ServerOptions").Get<ServerOptions>();

            services.AddDbContext<Smtp4devDbContext>(opt =>
            {
                if (string.IsNullOrEmpty(serverOptions.Database))
                {
                    Log.Logger.Information("Using in memory database.");
                    opt.UseSqlite("Data Source=file:cachedb?mode=memory&cache=shared");
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
            }, ServiceLifetime.Scoped, ServiceLifetime.Singleton);

            services.AddSingleton<ISmtp4devServer, Smtp4devServer>();
            services.AddSingleton<ImapServer>();
            services.AddScoped<IMessagesRepository, MessagesRepository>();
            services.AddScoped<IHostingEnvironmentHelper, HostingEnvironmentHelper>();
            services.AddSingleton<ITaskQueue, TaskQueue>();
            services.AddSingleton<ScriptingHost>();

            services.AddSingleton<Func<RelayOptions, SmtpClient>>((relayOptions) =>
            {
                if (!relayOptions.IsEnabled)
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

            services.AddControllers();
            services.AddRequestLocalization(options => { options.SupportedCultures = CultureInfo.GetCultures(CultureTypes.AllCultures); });

            services.AddSpaStaticFiles(o => o.RootPath = "ClientApp");
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
                            new SpaOptions { SourcePath = Path.Join(env.ContentRootPath, "ClientApp") },
                            npmScript: "serve",
                            regex: "App running at",
                            forceKill: true,
                            port: 8123
                        );

                    }
                });





                using (var scope = subdir.ApplicationServices.CreateScope())
                {
                    using (var context = scope.ServiceProvider.GetService<Smtp4devDbContext>())
                    {
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

                        context.Messages.ToList();
                        context.Sessions.ToList();
                        context.ImapState.ToList();
                        context.MessageRelays.ToList();

                    }
                }

                subdir.ApplicationServices.GetService<ISmtp4devServer>().TryStart();
                subdir.ApplicationServices.GetService<ImapServer>().TryStart();
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