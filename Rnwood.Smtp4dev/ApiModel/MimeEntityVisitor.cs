using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
	public class MimeEntityVisitor
	{
		public static void Visit(MimeEntity entity, Action<MimeEntity> action)
		{
			VisitWithResults<DBNull>(entity, (node, parentResult) => { action(node); return DBNull.Value; });
		}

		public static T VisitWithResults<T>(MimeEntity entity, Func<MimeEntity, T, T> action)
		{
			return VisitWithResults<T>(entity, default(T), action);
		}


		private static T VisitWithResults<T>(MimeEntity entity, T parentResult, Func<MimeEntity, T, T> action)
		{
			T result = action(entity, parentResult);

			if (entity is Multipart multipart)
			{
				foreach (MimeEntity childEntity in multipart)
				{
					VisitWithResults(childEntity, result, action);
				}
			}
			else if (entity is MimeKit.MessagePart rfc822)
			{
				VisitWithResults(rfc822.Message.Body, result, action);
			}

			return result;
		}
	}
}
