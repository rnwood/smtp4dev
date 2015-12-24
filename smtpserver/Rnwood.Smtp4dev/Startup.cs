using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rnwood.Smtp4dev.API;
using Rnwood.Smtp4dev.Model;
using Rnwood.Smtp4dev.UI;
using Swashbuckle.SwaggerGen;
using System;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev
{
    public class Startup
    {
        private IHostingEnvironment _hostingEnvironment;

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;

            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            if (_hostingEnvironment.IsDevelopment())
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddMvc();

            services.Configure<RazorViewEngineOptions>(razor => razor.ViewLocationExpanders.Add(new ViewLocationExpander()));

            services.AddSwaggerGen();
            services.ConfigureSwaggerDocument(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "smtp4dev",
                    Description = "SMTP server for development",
                    TermsOfService = "None"
                });
            });

            services.AddSignalR();

            services.UseSmtp4dev();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseApplicationInsightsRequestTelemetry();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseIISPlatformHandler();

            app.UseApplicationInsightsExceptionTelemetry();

            app.UseStaticFiles();

            app.UseSwaggerGen();
            app.UseSwaggerUi();

            app.UseSignalR();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Messages}/{action=Index}/{id?}");
            });

            IMessageStore messageStore = app.ApplicationServices.GetRequiredService<IMessageStore>();
            dynamic clients = app.ApplicationServices.GetRequiredService<IConnectionManager>().GetHubContext<MessagesHub>().Clients.All;

            messageStore.MessageAdded += (s, ea) => { clients.messageAdded(ea.Message.Id); };
            messageStore.MessageDeleted += (s, ea) => { clients.messageDeleted(ea.Message.Id); };
        }
    }
}