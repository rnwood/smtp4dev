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

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        public MessagesController(Smtp4devDbContext dbContext, MessagesHub messagesHub)
        {
            _dbContext = dbContext;
            _messagesHub = messagesHub;
        }


        private Smtp4devDbContext _dbContext;
        private MessagesHub _messagesHub;

        [HttpGet]
        public IEnumerable<Message> Get()
        {
            var result = _dbContext.Messages.Include(m=> m.Parts).ToList();
            
            return result;
        }

        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            if (id == "*")
            {
                _dbContext.Messages.RemoveRange(_dbContext.Messages);
                _dbContext.SaveChanges();

                await _messagesHub.OnMessageRemoved();
            }
        }


    }
}
