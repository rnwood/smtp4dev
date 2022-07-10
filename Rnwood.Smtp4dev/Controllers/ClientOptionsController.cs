using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientOptionsController : Controller
    {
        private readonly IOptions<ClientOptions> clientOptions;

        public ClientOptionsController(IOptions<ClientOptions> clientOptions)
        {
            this.clientOptions = clientOptions;
        }

        [HttpGet]
        public ApiModel.ClientOptions Get()
        {
            var clientProps = clientOptions.Value;
            return new ApiModel.ClientOptions
            {
                PageSize = clientProps.PageSize
            };
        }
    }
}