using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class MessagePart
    {
        public byte[] Content { get; set; }
        public string Headers { get; set; }
    }
}
