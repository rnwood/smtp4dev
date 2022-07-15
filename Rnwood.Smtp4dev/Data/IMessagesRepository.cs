﻿using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Data
{
    public interface IMessagesRepository
    {
        Task MarkAllMessagesRead();
        Task MarkMessageRead(Guid id);

        IQueryable<DbModel.Message> GetMessages(bool unTracked = true);

        Task DeleteMessage(Guid id);

        Task DeleteAllMessages();
        Smtp4devDbContext DbContext { get; }
    }
}