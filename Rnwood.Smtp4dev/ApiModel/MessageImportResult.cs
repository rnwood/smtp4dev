using System;
using System.Collections.Generic;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class MessageImportResult
    {
        public int TotalFiles { get; set; }
        public int SuccessfulImports { get; set; }
        public int FailedImports { get; set; }
        public List<MessageImportFileResult> FileResults { get; set; } = new List<MessageImportFileResult>();
        public List<Guid> ImportedMessageIds { get; set; } = new List<Guid>();
    }

    public class MessageImportFileResult
    {
        public string FileName { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Guid? MessageId { get; set; }
    }
}