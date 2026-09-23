using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace LamuFlix.Web.TagHelpers
{
    // You may need to install the Microsoft.AspNetCore.Razor.Runtime package into your project
    [HtmlTargetElement("alerthelper")]
    public class AlertsTagHelper : TagHelper
    {
        public string Type { get; set; } = null!;
        public string Message { get; set; } = null!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.Add("class", $"alert alert-{Type} alert-dismissible");
            output.Attributes.Add("role", "alert");

            var button = new TagBuilder("button");
            button.AddCssClass("close");
            button.Attributes.Add("data-dismiss", "alert");
            button.Attributes.Add("aria-label", "Close");

            var span = new TagBuilder("span");
            button.Attributes.Add("aria-hidden", "true");
            var sb = new StringBuilder();
            sb.Append("x");
            span.InnerHtml.Append(sb.ToString());
            button.InnerHtml.AppendHtml(span);

            output.Content.AppendHtml(button);

            var sb1 = new StringBuilder();
            sb1.Append(Message);

            output.PreContent.SetHtmlContent(sb1.ToString());
        }
    }
}
