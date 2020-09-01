using System;
using System.IO;
using System.Net;
using System.Net.Mail;

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
            ServerOptions serverOptions = Configuration.GetSection("ServerOptions").Get<ServerOptions>();
            RelayOptions relayOptions = Configuration.GetSection("RelayOptions").Get<RelayOptions>();

            services.AddDbContext<Smtp4devDbContext>(opt =>
            {

                if (string.IsNullOrEmpty(serverOptions.Database))
                {
                    Console.WriteLine("Using in memory database.");
                    opt.UseInMemoryDatabase("main");
                }
                else
                {
                    Console.WriteLine("Using Sqlite database at " + Path.GetFullPath(serverOptions.Database));
                    opt.UseSqlite($"Data Source='{serverOptions.Database}'");
                }
            }, ServiceLifetime.Transient, ServiceLifetime.Singleton);

            services.AddSingleton<Smtp4devServer>();
            services.AddSingleton<IMessagesRepository>(sp => sp.GetService<Smtp4devServer>());
            services.AddSingleton<Func<Smtp4devDbContext>>(sp => (() => sp.GetService<Smtp4devDbContext>()));
            
            services.Configure<ServerOptions>(Configuration.GetSection("ServerOptions"));
            services.Configure<RelayOptions>(Configuration.GetSection("RelayOptions"));

            if (relayOptions.IsEnabled)
            {
                services.AddSingleton(new SmtpClient(relayOptions.SmtpServer, relayOptions.SmtpPort)
                {
                    Credentials = new NetworkCredential(relayOptions.Login, relayOptions.Password),
                });
            }
            else
            {
                services.AddSingleton<SmtpClient>(_ => null);
            }
            
            services.AddSignalR();

            services.AddSingleton<MessagesHub>();
            services.AddSingleton<SessionsHub>();

            services.AddControllers();

            services.AddSpaStaticFiles(o => o.RootPath = "ClientApp");
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory log)
        {
            ServerOptions serverOptions = Configuration.GetSection("ServerOptions").Get<ServerOptions>();

            app.UseRouting();


            Action<IApplicationBuilder> configure = subdir =>
            {
                subdir.UseDeveloperExceptionPage();
                subdir.UseDefaultFiles();
                subdir.UseStaticFiles();
                subdir.UseSpaStaticFiles();

                subdir.UseWebSockets();

                subdir.UseEndpoints(e =>
                {
                    e.MapHub<MessagesHub>("/hubs/messages");
                    e.MapHub<SessionsHub>("/hubs/sessions");

                    e.MapControllers();
                    if (env.IsDevelopment())
                    {
                        e.MapToVueCliProxy(
                        "{*path}",
                        new SpaOptions { SourcePath = Path.Join(env.ContentRootPath, "ClientApp") },
                        npmScript: "serve",
                        regex: "Compiled successfully",
                        forceKill: true
                        );
                    }
                });

                



                Smtp4devDbContext context = subdir.ApplicationServices.GetService<Smtp4devDbContext>();
                if (!context.Database.IsInMemory())
                {
                    context.Database.Migrate();
                }

                subdir.ApplicationServices.GetService<Smtp4devServer>().Start();
            };

            if (!string.IsNullOrEmpty(serverOptions.RootUrl))
            {
                app.Map(serverOptions.RootUrl, configure);
            }
            else
            {
                configure(app);
            }
        }

    }
}
