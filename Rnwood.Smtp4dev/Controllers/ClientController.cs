using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : Controller
    {
        private readonly IOptions<ClientOptions> clientOptions;

        public ClientController(IOptions<ClientOptions> clientOptions)
        {
            this.clientOptions = clientOptions;
        }

        [HttpGet]
        public ApiModel.Client Get()
        {
            var clientProps = clientOptions.Value;
            return new ApiModel.Client
            {
                PageSize = clientProps.PageSize
            };
        }
    }
}