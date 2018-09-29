using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.ApiModel;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Session = Rnwood.Smtp4dev.ApiModel.Session;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    public class SessionsController : Controller
    {
        private readonly Smtp4devDbContext _dbContext;
        private readonly SessionsHub sessionsHub;

        public SessionsController(Smtp4devDbContext dbContext, SessionsHub sessionsHub)
        {
            _dbContext = dbContext;
            this.sessionsHub = sessionsHub;
        }

        [HttpGet]
        public IEnumerable<SessionSummary> GetSummaries()
        {
            return _dbContext.Sessions.Select(m => new SessionSummary(m));
        }

        [HttpGet("{id}")]
        public Session GetSession(Guid id)
        {
            var result = _dbContext.Sessions.FirstOrDefault(m => m.Id == id);
            return new Session(result);
        }


        [HttpDelete("{id}")]
        public async Task Delete(Guid id)
        {
            _dbContext.Sessions.RemoveRange(_dbContext.Sessions.Where(s => s.Id == id));
            _dbContext.SaveChanges();

            await sessionsHub.OnSessionsChanged();
        }

        [HttpDelete("*")]
        public async Task DeleteAll()
        {
            _dbContext.Sessions.RemoveRange(_dbContext.Sessions);
            _dbContext.SaveChanges();

            await sessionsHub.OnSessionsChanged();
        }
    }
}