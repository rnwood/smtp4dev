using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public PagedResult<SessionSummary> GetSummaries(int page = 1, int pageSize = 5)
        {
            return dbContext.Sessions.Where(s => s.EndDate.HasValue).OrderByDescending(x=>x.StartDate)
                .Select(m => new SessionSummary(m)).GetPaged(page, pageSize);
        }

        [HttpGet("{id}")]
        public ApiModel.Session GetSession(Guid id)
        {
            Session result = dbContext.Sessions.SingleOrDefault(m => m.Id == id);
            return new ApiModel.Session(result);
        }

        [HttpGet("{id}/log")]
        public string GetSessionLog(Guid id)
        {
            Session result = dbContext.Sessions.SingleOrDefault(m => m.Id == id);
            return result.Log;
        }

        [HttpDelete("{id}")]
        public async Task Delete(Guid id)
        {
            await server.DeleteSession(id);
        }

        [HttpDelete("*")]
        public async Task DeleteAll()
        {
            await server.DeleteAllSessions();
        }
    }
}