using System.Linq;

namespace LamuFlix.Web.Models.Helper
{
    public class QueryableResult<T>
    {
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }

        public IQueryable<T> Query { get; set; } = null!;
    }
}
