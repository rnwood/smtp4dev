using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.Data;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestingController
    {
        private readonly Smtp4devDbContext dbContext;

        public TestingController(Smtp4devDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpPost("summaries")]
        public ActionResult<int> CreateSummaries(CreateSummaryModel model)
        {
            //only active for debug
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                return new NotFoundResult();
            }

            for (var i = 0; i < model.Count; i++)
            {
                dbContext.Sessions.Add(new Session
                {
                    ClientAddress = "127.0.0.1", ClientName = "testharness", EndDate = DateTime.Now.AddMinutes(-20),
                    StartDate = DateTime.Now.AddMinutes(-30),
                    Id = Guid.NewGuid(),
                    SessionErrorType = 0,
                    NumberOfMessages = 1,
                    Log = "test-data"
                });
                if (i % 10 == 0)
                {
                    dbContext.SaveChanges();
                }
            }

            dbContext.SaveChanges();
            return dbContext.Sessions.Count();
        }

        public class CreateSummaryModel
        {
            public int Count { get; set; }
        }
    }
}