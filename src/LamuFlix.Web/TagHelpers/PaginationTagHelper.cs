using LamuFlix.Web.Models.Helper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace LamuFlix.Web.TagHelpers;

[HtmlTargetElement("pagination", Attributes = PaginationListAttributeName)]
public class PaginationTagHelper : TagHelper
{
    private const string PaginationListAttributeName = "asp-paginationlist";

    [HtmlAttributeName(PaginationListAttributeName)]
    // ReSharper disable NullableWarningSuppressionIsUsed
    // Razor attribute binding / [ViewContext] activation
    public IPagedListing PaginationList { get; set; } = null!;

    [HtmlAttributeNotBound]
    [ViewContext]
    private ViewContext ViewContext { get; set; } = null!;
    // ReSharper restore NullableWarningSuppressionIsUsed

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "nav";
        output.Attributes.Add("aria-label", "pagination");

        var ul = new TagBuilder("ul");
        ul.AddCssClass("pagination");

        if (PaginationList.HasPreviousPage)
        {
            ul.InnerHtml.AppendHtml(PagingNavButton("fa fa-fast-backward", 1));
            ul.InnerHtml.AppendHtml(PagingNavButton("fa fa-step-backward", PaginationList.CurrentPage - 1));
        }

        for (var i = PaginationList.PagingStart; i <= PaginationList.PagingEnd; i++)
        {
            ul.InnerHtml.AppendHtml(PagingPageButton(i));
        }

        if (PaginationList.HasNextPage)
        {
            ul.InnerHtml.AppendHtml(PagingNavButton("fa fa-step-forward", PaginationList.CurrentPage + 1));
            ul.InnerHtml.AppendHtml(PagingNavButton("fa fa-fast-forward", PaginationList.TotalPages));
        }

        output.Content.AppendHtml(ul);
    }

    private TagBuilder PagingNavButton(string icon, int targetPage)
    {
        var li = new TagBuilder("li");
        li.AddCssClass("page-item");

        var a = new TagBuilder("a");
        a.CreateLink(ViewContext, null, new QueryStringKeyValuePair("parms.page", Convert.ToString(targetPage)));
        a.AddCssClass("page-link");

        var span = new TagBuilder("span");
        span.AddCssClass(icon);

        a.InnerHtml.AppendHtml(span);
        li.InnerHtml.AppendHtml(a);

        return li;
    }
    private TagBuilder PagingPageButton(int targetPage)
    {
        var li = new TagBuilder("li");
        li.AddCssClass("page-item");

        if (PaginationList.CurrentPage == targetPage)
            li.AddCssClass("active");

        var a = new TagBuilder("a");
        a.CreateLink(ViewContext, targetPage.ToString(), new QueryStringKeyValuePair("parms.page", targetPage.ToString()));
        a.AddCssClass("page-link");

        li.InnerHtml.AppendHtml(a);

        return li;
    }
}