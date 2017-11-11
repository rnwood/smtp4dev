using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using Microsoft.EntityFrameworkCore;
using Rnwood.SmtpServer;
using Rnwood.Smtp4dev.Server;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Rnwood.Smtp4dev
{
    public class Startup
    {

        public Startup(IHostingEnvironment env)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddDbContext<Smtp4devDbContext>(opt => opt.UseInMemoryDatabase("main"));

            services.AddSingleton<Smtp4devServer>();
            services.AddSingleton<Func<Smtp4devDbContext>>(sp => (() => sp.GetService<Smtp4devDbContext>()));

           
            services.Configure<ServerOptions>(Configuration.GetSection("ServerOptions"));
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.ApplicationServices.GetService<Smtp4devServer>().Start();
        }
    }
}
