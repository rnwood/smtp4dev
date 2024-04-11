using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientSettingsController : Controller
    {
        private readonly IOptions<ClientOptions> clientOptions;

        public ClientSettingsController(IOptions<ClientOptions> clientOptions)
        {
            this.clientOptions = clientOptions;
        }

        /// <summary>
        /// Gets client settings for the smtp4dev UI.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ApiModel.ClientSettings Get()
        {
            var clientProps = clientOptions.Value;
            return new ApiModel.ClientSettings
            {
                PageSize = clientProps.PageSize
            };
        }
    }
}