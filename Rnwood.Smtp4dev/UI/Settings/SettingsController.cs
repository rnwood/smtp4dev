using Microsoft.AspNet.Mvc;
using Rnwood.Smtp4dev.Model;
using Rnwood.Smtp4dev.UI.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Rnwood.Smtp4dev.UI
{
    public class SettingsController : Controller
    {
        private ISettingsStore _settingsStore;
        private ISmtp4devEngine _server;

        public SettingsController(ISettingsStore settingsStore, ISmtp4devEngine server)
        {
            _settingsStore = settingsStore;
            _server = server;
        }

        // GET: /<controller>/
        public IActionResult Index(string message)
        {
            Model.Settings settings = _settingsStore.Load();

            return View(new SettingsViewModel() { Port = settings.Port, Message = message });
        }

        [HttpPost]
        public IActionResult Index(SettingsViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                Model.Settings settings = _settingsStore.Load();
                settings.Port = viewModel.Port;

                _server.ApplySettings(settings);

                return RedirectToAction("Index", new { message = "Settings have been saved." });
            }
            else
            {
                return View(viewModel);
            }
        }
    }
}