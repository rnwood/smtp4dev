using AspNetCore.Authentication.Basic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Server.Settings;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev
{
    public class UserValidationService : IBasicUserValidationService, IAuthorizationHandler
    {
        private readonly IOptionsMonitor<ServerOptions> serverOptions;


        public UserValidationService(IOptionsMonitor<ServerOptions> serverOptions)
        {
            this.serverOptions = serverOptions;
        }

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
           foreach(var r in context.Requirements)
            {
                if (!serverOptions.CurrentValue.WebAuthenticationRequired || (context.User?.Identity?.IsAuthenticated ?? false))
                {
                    context.Succeed(r);
                }  else
                {
                    context.Fail(new AuthorizationFailureReason(this, "Login required"));
                }
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsValidAsync(string username, string password)
        {
            return Task.FromResult(this.serverOptions.CurrentValue.Users?
                .Any(u =>
                    username.Equals(u.Username, System.StringComparison.CurrentCultureIgnoreCase) &&
                    password == u.Password
                ) ?? false);
        }
    }
}
