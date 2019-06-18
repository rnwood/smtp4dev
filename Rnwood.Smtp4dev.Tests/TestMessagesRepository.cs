using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.DbModel;

namespace Rnwood.Smtp4dev.Tests
{
	internal class TestMessagesRepository : IMessagesRepository
	{
		public TestMessagesRepository(params Message[] messages)
		{
			Messages.AddRange(messages);
		}

		public List<DbModel.Message> Messages { get; } = new List<Message>();

		public Task DeleteAllMessages()
		{
			Messages.Clear();
			return Task.CompletedTask;
		}

		public Task DeleteMessage(Guid id)
		{
			Messages.RemoveAll(m => m.Id == id);
			return Task.CompletedTask;
		}

		public IQueryable<Message> GetMessages()
		{
			return Messages.AsQueryable();
		}

		public Task MarkMessageRead(Guid id)
		{
			Messages.FirstOrDefault(m => m.Id == id).IsUnread = false;
			return Task.CompletedTask;
		}
	}
}
