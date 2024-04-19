using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Rnwood.Smtp4dev.ApiModel;
using Rnwood.Smtp4dev.Server.Settings;

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
        [HttpGet]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(ClientSettings), Description = "")]
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