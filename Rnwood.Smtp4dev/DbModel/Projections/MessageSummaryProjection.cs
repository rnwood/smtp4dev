using System;

namespace Rnwood.Smtp4dev.DbModel.Projections;

public class MessageSummaryProjection
{
    public Guid Id { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string Subject { get; set; }
    public int AttachmentCount { get; set; }
    public bool IsRelayed { get; set; }
    public string DeliveredTo { get; set; }
    public bool IsUnread { get; set; }
}