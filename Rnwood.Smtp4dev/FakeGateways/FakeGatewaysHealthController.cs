#nullable enable
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>Tells the UI and e2e tests whether fake gateways are active. Available even when they are disabled.</summary>
    [Route("api/fakegateways/health")]
    [ApiController]
    [AllowAnonymous]
    public class FakeGatewaysHealthController : ControllerBase
    {
        private readonly IOptionsMonitor<FakeGatewaysOptions> options;

        public FakeGatewaysHealthController(IOptionsMonitor<FakeGatewaysOptions> options)
        {
            this.options = options;
        }

        /// <summary>Gets the state of the fake gateways.</summary>
        [HttpGet]
        public ActionResult<FakeGatewaysHealth> Get()
        {
            string? version = typeof(FakeGatewaysHealthController).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            bool enabled = options.CurrentValue.Enabled;

            return new FakeGatewaysHealth
            {
                Status = enabled ? "ok" : "disabled",
                Features = enabled ? new List<string> { "konzulta", "targito" } : new List<string>(),
                Version = version
            };
        }
    }
}
