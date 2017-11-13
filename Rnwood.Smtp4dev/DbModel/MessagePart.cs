using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.DbModel
{
    public class MessagePart
    {
        public Guid Id { get; set; }

        [JsonIgnore]
        public Message Owner { get; set; }
        public string Content { get; set; }
    }
}
