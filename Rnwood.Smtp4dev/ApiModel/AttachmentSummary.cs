using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class AttachmentSummary
    {
        public string FileName { get; set; }
        public string ContentId { get; set; }

		public string Id { get; set; }

		public string Url { get; set; }
    }
}
