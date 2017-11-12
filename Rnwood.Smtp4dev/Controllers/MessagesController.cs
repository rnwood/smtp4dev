using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        public MessagesController(Smtp4devDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        private Smtp4devDbContext _dbContext;

        [HttpGet]
        public IEnumerable<Message> Get()
        {
            return _dbContext.Messages;
        }

        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            if (id == "*")
            {
                _dbContext.Messages.RemoveRange(_dbContext.Messages);
                _dbContext.SaveChanges();
            }
        }


    }
}
