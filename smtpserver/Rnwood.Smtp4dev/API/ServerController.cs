using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Rnwood.Smtp4dev.API.DTO;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Rnwood.Smtp4dev.API
{
    [Route("api/server")]
    public class ServerController : Controller
    {
        public ServerController(ISmtp4devEngine engine, ISettingsStore settingsStore)
        {
            _engine = engine;
            _settingsStore = settingsStore;
        }

        private ISmtp4devEngine _engine;
        private ISettingsStore _settingsStore;

        // GET: api/values
        [HttpGet("{id}")]
        public Server Get(int id)
        {
            Settings settings = _settingsStore.Load();

            return new Server()
            {
                isRunning = _engine.IsRunning,
                error = _engine?.ServerError?.Message,
                port = settings.Port,
                isEnabled = settings.IsEnabled
            };
        }

        [HttpPut("{id}")]
        public Server Update([FromBody] ServerUpdate server)
        {
            Settings settings = _settingsStore.Load();

            settings.Port = server.port;
            settings.IsEnabled = server.isEnabled;
            _settingsStore.Save(settings);

            return Get(server.id);
        }

        [HttpGet("events")]
        public async Task Events()
        {
            HttpContext.Response.ContentType = "text/event-stream";

            AutoResetEvent stateChangedEvent = new AutoResetEvent(false);

            _engine.StateChanged += (s, ea) =>
            {
                stateChangedEvent.Set();
            };

            while (true)
            {
                await stateChangedEvent.WaitOneAsync();
                await HttpContext.Response.WriteAsync("event: statechanged\ndata: stateChange!\n\n");
            }
        }
    }
}