using Microsoft.AspNet.Mvc;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.UI
{
    public class AppController : Controller
    {
        public AppController()
        {
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}