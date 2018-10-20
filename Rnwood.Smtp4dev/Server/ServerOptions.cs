using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Server
{
    public class ServerOptions
    {
        public int Port { get; set; }
        public bool AllowRemoteConnections { get; set; }

        public string Database { get; set; }
    }
}
