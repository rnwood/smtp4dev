using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.IO;
using MimeKit;
using HtmlAgilityPack;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [UseEtagFilterAttribute]
    public class SessionsController : Controller
    {
        public SessionsController(Smtp4devDbContext dbContext, SessionsHub sessionsHub)
        {
            _dbContext = dbContext;
            this.sessionsHub = sessionsHub;
        }


        private Smtp4devDbContext _dbContext;
        private SessionsHub sessionsHub;

        [HttpGet]
        [ResponseCache( Duration = 3600)]
        public IEnumerable<ApiModel.SessionSummary> GetSummaries()
        {
            return _dbContext.Sessions.Select(m => new ApiModel.SessionSummary(m));
        }

        [HttpGet("{id}")]
        [ResponseCache( Duration = 3600)]
        public ApiModel.Session GetSession(Guid id)
        {
            Session result = _dbContext.Sessions.FirstOrDefault(m => m.Id == id);
            return new ApiModel.Session(result);
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
