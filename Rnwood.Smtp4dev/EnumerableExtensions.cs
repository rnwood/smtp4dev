using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<TItem> Flatten<TItem>(this IEnumerable<TItem> items, Func<TItem, IEnumerable<TItem>> getChildren)
		{
			return items.SelectMany(c => getChildren(c).Flatten(getChildren)).Concat(items);
		}
	}
}
