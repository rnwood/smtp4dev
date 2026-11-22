#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Rnwood.Smtp4dev.FakeGateways
{
    /// <summary>Short-circuits the request with 404 when FakeGateways:Enabled is false.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class FakeGatewaysEnabledAttribute : Attribute, IAsyncResourceFilter
    {
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<FakeGatewaysOptions>>();
            if (!options.CurrentValue.Enabled)
            {
                context.Result = new NotFoundResult();
                return;
            }

            await next();
        }
    }
}
