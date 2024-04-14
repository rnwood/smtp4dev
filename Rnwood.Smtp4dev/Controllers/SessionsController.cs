using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSwag.Annotations;
using Rnwood.Smtp4dev.ApiModel;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.Server;
using Session = Rnwood.Smtp4dev.DbModel.Session;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [UseEtagFilterAttribute]
    public class SessionsController : Controller
    {
        private readonly Smtp4devDbContext dbContext;
        private readonly ISmtp4devServer server;

        public SessionsController(Smtp4devDbContext dbContext, ISmtp4devServer server)
        {
            this.dbContext = dbContext;
            this.server = server;
        }

        /// <summary>
        /// Gets a a list of SMTP session summaries (without the details).
        /// </summary>
        /// <param name="page">The page number to get</param>
        /// <param name="pageSize">Maximum number of items to get</param>
        /// <returns></returns>
        [HttpGet]
        public PagedResult<SessionSummary> GetSummaries(int page = 1, int pageSize = 5)
        {
            return dbContext.Sessions.AsNoTracking().Where(s => s.EndDate.HasValue).OrderByDescending(x => x.StartDate)
                .Select(m => new SessionSummary(m)).GetPaged(page, pageSize);
        }

        /// <summary>
        /// Gets the details for the specified session.
        /// This does not include the session log - <see cref="GetSessionLog"/>. 
        /// </summary>
        /// <param name="id">The ID of the session to get</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(ApiModel.Session), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the session does not exist")]
        public ApiModel.Session GetSession(Guid id)
        {
            Session result = dbContext.Sessions.AsNoTracking().SingleOrDefault(m => m.Id == id);
            return new ApiModel.Session(result);
        }

        /// <summary>
        /// Gets the session log for the specified session.
        /// </summary>
        /// <param name="id">The ID for the session to get.</param>
        /// <returns></returns>
        /// 
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(string), Description = "")]
        [SwaggerResponse(System.Net.HttpStatusCode.NotFound, typeof(void), Description = "If the session does not exist")]
        [HttpGet("{id}/log")]
        public string GetSessionLog(Guid id)
        {
            Session result = dbContext.Sessions.AsNoTracking().SingleOrDefault(m => m.Id == id);
            return result.Log;
        }

        /// <summary>
        /// Deletes the specified session.
        /// </summary>
        /// <param name="id">The ID of the session to delete.</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task Delete(Guid id)
        {
            await server.DeleteSession(id);
        }
        /// <summary>
        /// Deletes all sessions.
        /// </summary>
        /// <returns></returns>
        [HttpDelete("*")]
        public async Task DeleteAll()
        {
            await server.DeleteAllSessions();
        }
    }
}