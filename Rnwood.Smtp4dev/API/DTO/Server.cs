using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.API.DTO
{
    public class Server
    {
        public int id { get; set; }

        public bool isRunning { get; internal set; }

        public bool isEnabled { get; set; }

        public string error { get; set; }
        public int port { get; set; }
    }
}