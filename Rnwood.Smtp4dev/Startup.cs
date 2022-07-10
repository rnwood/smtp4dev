using System;
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
using Microsoft.Extensions.Hosting;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Rewrite;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Service;
using Serilog;
using System.Linq;
using System.Reflection;

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
            services.Configure<ServerOptions>(Configuration.GetSection("ServerOptions"));
            services.Configure<RelayOptions>(Configuration.GetSection("RelayOptions"));
            services.Configure<ClientOptions>(Configuration.GetSection("ClientOptions"));

            ServerOptions serverOptions = Configuration.GetSection("ServerOptions").Get<ServerOptions>();

            services.AddDbContext<Smtp4devDbContext>(opt =>
            {
                if (string.IsNullOrEmpty(serverOptions.Database))
                {
                    Log.Logger.Information("Using in memory database.");
                    opt.UseInMemoryDatabase("main");
                }
                else
                {


                    var dbLocation = Path.GetFullPath(serverOptions.Database);
                    if (serverOptions.RecreateDb && File.Exists(dbLocation))
                    {
                        Log.Logger.Information("Deleting Sqlite database.");
                        File.Delete(dbLocation);
                    }

                    Log.Logger.Information("Using Sqlite database at {dbLocation}" , dbLocation);
                    opt.UseSqlite($"Data Source='{dbLocation}'");
                }
            }, ServiceLifetime.Scoped, ServiceLifetime.Singleton);

            services.AddSingleton<ISmtp4devServer, Smtp4devServer>();
            services.AddSingleton<ImapServer>();
            services.AddScoped<IMessagesRepository, MessagesRepository>();
            services.AddScoped<IHostingEnvironmentHelper, HostingEnvironmentHelper>();
            services.AddSingleton<ITaskQueue, TaskQueue>();


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
            services.AddOpenApiDocument(d => d.Title = "smtp4dev");


            services.AddControllers();
            services.AddOpenApiDocument(c =>
            {
                c.Title = "smtp4dev";
            });

        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ServerOptions serverOptions = Configuration.GetSection("ServerOptions").Get<ServerOptions>();

            app.UseRouting();
            app.UseOpenApi();
            app.UseSwaggerUi();


            Action<IApplicationBuilder> configure = subdir =>
            {
                if (env.IsDevelopment())
                {
                    app.UseWebAssemblyDebugging();
                }

                subdir.UseOpenApi();
                subdir.UseSwaggerUi();

                subdir.UseRouting();
                subdir.UseDeveloperExceptionPage();
                subdir.UseDefaultFiles();
                subdir.UseBlazorFrameworkFiles();
                subdir.UseStaticFiles();

                subdir.UseWebSockets();

                subdir.UseEndpoints(e =>
                {
                    e.MapHub<NotificationsHub>("/hubs/notifications");
                    e.MapControllers();
                    e.MapFallbackToFile("index.html");

                });

                using (var scope = subdir.ApplicationServices.CreateScope())
                {
                    using (var context = scope.ServiceProvider.GetService<Smtp4devDbContext>())
                    {
                        if (!context.Database.IsInMemory())
                        {

                            var pendingMigrations = context.Database.GetPendingMigrations();
                            if (pendingMigrations.Any())
                            {
                                Log.Logger.Information("Updating DB schema with migrations: {migrations}", string.Join(", ", pendingMigrations));
                                context.Database.Migrate();
                            }

                        }
                    }
                }

                subdir.ApplicationServices.GetService<ISmtp4devServer>().TryStart();
                subdir.ApplicationServices.GetService<ImapServer>().TryStart();
            };

            if (!string.IsNullOrEmpty(serverOptions.BasePath) && serverOptions.BasePath != "/")
            {
                RewriteOptions rewrites = new RewriteOptions();
                rewrites.AddRedirect("^" + serverOptions.BasePath.TrimEnd('/') + "$", serverOptions.BasePath.TrimEnd('/') + "/"); ;
                rewrites.AddRedirect("^(/)?$", serverOptions.BasePath.TrimEnd('/') + "/"); ;
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
