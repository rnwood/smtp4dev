using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev
{
	public interface IMessagesRepository
	{
		Task MarkMessageRead(Guid id);

		IQueryable<DbModel.Message> GetMessages();

		Task DeleteMessage(Guid id);

		Task DeleteAllMessages();
	}
}
