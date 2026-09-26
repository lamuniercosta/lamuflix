using Microsoft.AspNetCore.Mvc;

namespace LamuFlix.Web.Models.Helper;

// ReSharper disable NullableWarningSuppressionIsUsed
// MVC model binder
public class QueryParams
{
    [FromQuery]
    public FilterViewModel Filter { get; set; } = null!;

    [FromQuery]
    public int Page => 1;

    [FromQuery]
    public int PageSize { get; set; } = 16;

    [FromQuery]
    public string SortBy { get; set; } = null!;
    [FromQuery]
    public string SortOrder { get; set; } = null!;
}
// ReSharper restore NullableWarningSuppressionIsUsed