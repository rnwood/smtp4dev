using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.Controllers
{
    /// <summary>
    /// Returns information about mailboxes
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [UseEtagFilter]
    public class MailboxesController : Controller
    {
        private readonly Smtp4devDbContext dbContext;
        private readonly IOptionsMonitor<ServerOptions> serverOptions;

        public MailboxesController(Smtp4devDbContext dbContext, IOptionsMonitor<ServerOptions> serverOptions)
        {
            this.dbContext = dbContext;
            this.serverOptions = serverOptions;
        }

        /// <summary>
        /// Gets list of mailboxes available to current user.
        /// When WebAuthenticationRequired is enabled, non-admin users only see their own mailbox.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<IList<Mailbox>> GetAll()
        {
            string currentUserName = this.User?.Identity?.Name;

            // If no user logged in or user is admin, return all mailboxes
            if (string.IsNullOrEmpty(currentUserName) ||
                currentUserName.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return this.dbContext.Mailboxes.ToList();
            }

            // Find user's default mailbox from configuration
            var user = serverOptions.CurrentValue.Users
                .FirstOrDefault(u => currentUserName.Equals(u.Username, StringComparison.OrdinalIgnoreCase));

            if (user != null && !string.IsNullOrEmpty(user.DefaultMailbox))
            {
                // Return only user's mailbox
                var userMailbox = this.dbContext.Mailboxes
                    .Where(m => m.Name == user.DefaultMailbox)
                    .ToList();

                if (userMailbox.Any())
                {
                    return userMailbox;
                }
            }

            // Fallback to Default mailbox only
            return this.dbContext.Mailboxes
                .Where(m => m.Name == MailboxOptions.DEFAULTNAME)
                .ToList();
        }
    }
}
