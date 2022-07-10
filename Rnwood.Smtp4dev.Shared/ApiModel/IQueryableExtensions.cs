using System;
using System.Linq;

namespace Rnwood.Smtp4dev.ApiModel
{
    public static class QueryableExtensions
    {
        public static TResult GetPaged<T, TResult>(this IQueryable<T> query,
            int page, int pageSize) where T : class where TResult : PagedResult<T>, new()
        {
            var result = new TResult
            {
                CurrentPage = page,
                PageSize = pageSize,
                RowCount = query.Count()
            };


            var pageCount = (double)result.RowCount / pageSize;
            result.PageCount = (int)Math.Ceiling(pageCount);

            var skip = (page - 1) * pageSize;
            result.Results = query.Skip(skip).Take(pageSize).ToList();

            return result;
        }
    }
}