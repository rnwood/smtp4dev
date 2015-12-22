using Microsoft.AspNet.Mvc;
using Rnwood.Smtp4dev.Model;
using Rnwood.Smtp4dev.UI.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.UI
{
    public class MessagesController : Controller
    {
        public MessagesController(IMessageStore messageStore)
        {
            _messageStore = messageStore;
        }

        private IMessageStore _messageStore;

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult View(Guid id)
        {
            ISmtp4devMessage message = _messageStore.Messages.FirstOrDefault(m => m.Id == id);

            if (message == null)
            {
                return View("NotFound");
            }

            return View(new ViewMessageViewModel(message));
        }
    }
}