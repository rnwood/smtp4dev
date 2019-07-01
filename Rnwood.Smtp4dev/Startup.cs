using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Rnwood.Smtp4dev.Server;
using Microsoft.Net.Http.Headers;

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

            services.AddMvc();

            services.AddDbContext<Smtp4devDbContext>(opt => {

                if (string.IsNullOrEmpty(serverOptions.Database) )
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

            services.AddSignalR();

            services.AddSingleton<MessagesHub>();
            services.AddSingleton<SessionsHub>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory log)
        {
            ServerOptions serverOptions = Configuration.GetSection("ServerOptions").Get<ServerOptions>();


            Action<IApplicationBuilder> configure = subdir => {

            subdir.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = new JsonExceptionMiddleware().Invoke
            });

            subdir.UseDefaultFiles();

			if (env.IsDevelopment()) {
			    subdir.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
			}

            subdir.UseStaticFiles();

            subdir.UseWebSockets();
            subdir.UseSignalR(routes =>
            {
                routes.MapHub<MessagesHub>("/hubs/messages");
                routes.MapHub<SessionsHub>("/hubs/sessions");
            });

            subdir.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
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
            } else {
                configure(app);
            }
        }
        
    }
}
