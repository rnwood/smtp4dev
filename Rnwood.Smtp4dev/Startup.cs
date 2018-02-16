﻿using System;
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
using Microsoft.AspNetCore.SignalR;
using Rnwood.Smtp4dev.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.IO;
using Newtonsoft.Json;

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
            services.AddDbContext<Smtp4devDbContext>(opt => opt.UseInMemoryDatabase("main"), ServiceLifetime.Transient, ServiceLifetime.Singleton);

            services.AddSingleton<Smtp4devServer>();
            services.AddSingleton<Func<Smtp4devDbContext>>(sp => (() => sp.GetService<Smtp4devDbContext>()));

            services.Configure<ServerOptions>(Configuration.GetSection("ServerOptions"));

            services.AddSignalR();

            services.AddSingleton<MessagesHub>();
            services.AddSingleton<SessionsHub>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = new JsonExceptionMiddleware().Invoke
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseMvc();

            app.UseWebSockets();
            app.UseSignalR(routes =>
            {
                routes.MapHub<MessagesHub>("hubs/messages");
                routes.MapHub<SessionsHub>("hubs/sessions");
            });




            app.ApplicationServices.GetService<Smtp4devServer>().Start();

            if (env.IsDevelopment())
            {
                Smtp4devDbContext db = app.ApplicationServices.GetService<Smtp4devDbContext>();

                MessageConverter messageConverter = new MessageConverter();


                using (Stream stream = File.OpenRead("example.eml"))
                {
                    var convert = new MessageConverter().Convert(stream);
                    db.Messages.Add(convert.Item1);
                    db.MessageDatas.Add(convert.Item2);
                }

                using (Stream stream = File.OpenRead("example2.eml"))
                {
                    var convert = new MessageConverter().Convert(stream);
                    db.Messages.Add(convert.Item1);
                    db.MessageDatas.Add(convert.Item2);
                }

                db.SaveChanges();
            }
        }
    }

    public class JsonExceptionMiddleware
    {
        public async Task Invoke(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (ex == null) return;

            var error = new
            {
                message = ex.Message
            };

            context.Response.ContentType = "application/json";

            using (var writer = new StreamWriter(context.Response.Body))
            {
                new JsonSerializer().Serialize(writer, error);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}
