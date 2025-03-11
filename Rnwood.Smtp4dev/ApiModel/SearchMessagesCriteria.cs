using System;

namespace Rnwood.Smtp4dev.ApiModel
{
    public record SearchMessagesCriteria(string To, string Subject, string Content, DateTime? DateFrom);
}
