using MimeKit;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.ApiModel
{
    internal class ServerMessageEntitySummary : MessageEntitySummary
    {

        [JsonIgnore]
        internal MimeEntity MimeEntity { get; set; }
    }
}
