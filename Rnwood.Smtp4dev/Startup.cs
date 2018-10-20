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
using Microsoft.EntityFrameworkCore;

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
            services.AddMvc();

            
            services.AddDbContext<Smtp4devDbContext>(opt => {
                //opt.UseSqlite()
                opt.UseInMemoryDatabase("main");
            }, ServiceLifetime.Transient, ServiceLifetime.Singleton);

            services.AddSingleton<Smtp4devServer>();
            services.AddSingleton<Func<Smtp4devDbContext>>(sp => (() => sp.GetService<Smtp4devDbContext>()));

            services.Configure<ServerOptions>(Configuration.GetSection("ServerOptions"));

            services.AddSignalR();

            services.AddSingleton<MessagesHub>();
            services.AddSingleton<SessionsHub>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory log)
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = new JsonExceptionMiddleware().Invoke
            });

            app.UseDefaultFiles();
			
			if (env.IsDevelopment()) {
			    app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
			}
			
            app.UseStaticFiles();

            app.UseWebSockets();
            app.UseSignalR(routes =>
            {
                routes.MapHub<MessagesHub>("/hubs/messages");
                routes.MapHub<SessionsHub>("/hubs/sessions");
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });



            app.ApplicationServices.GetService<Smtp4devServer>().Start();

            if (env.IsDevelopment())
            {
                LoadExampleMessagesAsync(app.ApplicationServices.GetService<Smtp4devDbContext>()).ContinueWith((t) => { }) ;
            }
        }

        private static async Task LoadExampleMessagesAsync(Smtp4devDbContext db)
        {;

            MessageConverter messageConverter = new MessageConverter();


            using (Stream stream = File.OpenRead("example.eml"))
            {
                Message message = await messageConverter.ConvertAsync(stream, "from@from.com", "to@to.com");
                db.Messages.Add(message);
            }

            using (Stream stream = File.OpenRead("example2.eml"))
            {
                Message message = await messageConverter.ConvertAsync(stream, "from2@from.com", "to2@to.com");
                db.Messages.Add(message);
            }

            db.SaveChanges();
        }
    }
}
