using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.ApiModel;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [UseEtagFilter]
    public class VersionController : Controller
    {
        [HttpGet]
        public ActionResult<VersionInfo> Get()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var infoVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return new VersionInfo { Version = version, InfoVersion = infoVersion };
        }
    }
}