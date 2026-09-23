using Microsoft.AspNetCore.Mvc;

namespace LamuFlix.Web.Models.Helper
{
    public class QueryParams
    {
        [FromQuery]
        public FilterViewModel Filter { get; set; } = null!;

        private int _page = 1;
        [FromQuery]
        public int Page
        {
            get
            {
                return _page;
            }
            set => _page = (0 == value || value <= 0) ? 1 : value;
        }

        [FromQuery]
        public int PageSize { get; set; } = 16;

        [FromQuery]
        public string SortBy { get; set; } = null!;
        [FromQuery]
        public string SortOrder { get; set; } = null!;
    }
}
