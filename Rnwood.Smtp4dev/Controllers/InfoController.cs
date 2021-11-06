using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.ApiModel;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [UseEtagFilter]
    public class InfoController : Controller
    {
        [HttpGet]
        public ActionResult<Info> Get()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var infoVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return new Info { Version = version, InfoVersion = infoVersion };
        }
    }
}