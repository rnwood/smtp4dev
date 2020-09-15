using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Server
    {
        public bool IsRunning { get; set; }

        public string Exception { get; set; }

        public int PortNumber { get; set; }


    }
}
