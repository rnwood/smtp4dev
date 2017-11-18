using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Session
    {
        public Session(DbModel.Session dbSession)
        {
            this.Id = dbSession.Id;
            this.Log = dbSession.Log;
        }


        public Guid Id { get; private set; }
        public string Log { get; private set; }
    }
}
