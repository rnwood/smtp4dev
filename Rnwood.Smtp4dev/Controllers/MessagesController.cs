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
        public IEnumerable<ApiModel.MessageHeader> GetHeaders()
        {
            return _dbContext.Messages.Select(m => new ApiModel.MessageHeader(m));
        }

        [HttpGet("{id}")]
        public ApiModel.Message GetMessage(Guid id)
        {
            Message result = _dbContext.Messages.FirstOrDefault(m => m.Id == id);
            return new ApiModel.Message(result);
        }

        [HttpDelete("*")]
        public async Task DeleteAll()
        {

            _dbContext.Messages.RemoveRange(_dbContext.Messages);
            _dbContext.SaveChanges();

            await _messagesHub.OnMessagesChanged();

        }


    }
}
