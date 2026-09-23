using System.Linq;

namespace LamuFlix.Web.Models.Helper
{
    // ReSharper disable NullableWarningSuppressionIsUsed
    // In-product assignment FilmesServices.cs:151,350
    public class QueryableResult<T>
    {
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; }

        public IQueryable<T> Query { get; set; } = null!;
    }
    // ReSharper restore NullableWarningSuppressionIsUsed
}
