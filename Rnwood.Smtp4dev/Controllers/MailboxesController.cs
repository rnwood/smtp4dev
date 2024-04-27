using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.Controllers
{
    /// <summary>
    /// Returns information about the version of smtp4dev
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [UseEtagFilter]
    public class MailboxesController : Controller
    {
        private Smtp4devDbContext dbContext;

        public MailboxesController(Smtp4devDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets list of mailboxes.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<IList<Mailbox>> GetAll()
        {
            return this.dbContext.Mailboxes.ToList();
        }
    }
}