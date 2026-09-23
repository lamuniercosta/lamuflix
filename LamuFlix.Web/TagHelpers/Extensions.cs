using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LamuFlix.Web.TagHelpers
{
    public class QueryStringKeyValuePair
    {
        public string Key { get; }
        public string Value { get; }

        public QueryStringKeyValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    public class QueryStringBuilder
    {
        public List<KeyValuePair<string, string>> QueryStringValues { get; private set; }

        public QueryString QueryString
        {
            get
            {
                return new QueryBuilder(QueryStringValues).ToQueryString();
            }
        }

        public void Add(QueryStringKeyValuePair kvPair) //KeyValuePair<string, string> keyValue)
        {
            Add(kvPair.Key, kvPair.Value);
        }
        public void Add(string Key, string Value)
        {
            var qs = QueryStringValues.SingleOrDefault(x => string.Equals(x.Key, Key, StringComparison.OrdinalIgnoreCase));
            if (null != qs.Value)
                QueryStringValues.RemoveAll(x => string.Equals(x.Key, qs.Key, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Value, qs.Value, StringComparison.Ordinal));

            QueryStringValues.Add(new KeyValuePair<string, string>(Key, Value));
        }
        public void AddRange(List<QueryStringKeyValuePair> keyvalues)
        {
            foreach (var keyvalue in keyvalues)
                Add(keyvalue.Key, keyvalue.Value);
        }
        public void Remove(string Key)
        {
            var qs = QueryStringValues.SingleOrDefault(x => string.Equals(x.Key, Key, StringComparison.OrdinalIgnoreCase));
            if (null != qs.Value)
                QueryStringValues.RemoveAll(x => string.Equals(x.Key, qs.Key, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Value, qs.Value, StringComparison.Ordinal));
        }

        public QueryStringBuilder(string displayUrl)
        {
            Uri uri = new Uri(displayUrl);

            QueryStringValues = [.. QueryHelpers.ParseQuery(uri.Query)
                .SelectMany(x =>
                    x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value ?? string.Empty)
                    )];
        }
    }

    public static class Extensions
    {
        public static void CreateLink(this TagBuilder tag, ViewContext context, string? innerHtml, QueryStringKeyValuePair keyValue)
        {
            QueryStringBuilder qb = new QueryStringBuilder(UriHelper.GetDisplayUrl(context.HttpContext.Re‌​quest));
            qb.Add(keyValue);

            //var action = context.ActionDescriptor.RouteValues["action"];

            tag.InnerHtml.Append(innerHtml ?? string.Empty);
            tag.MergeAttribute("href", $"{qb.QueryString}");
        }
        public static void CreateLink(this TagBuilder tag, ViewContext context, string? innerHtml, List<QueryStringKeyValuePair> keyValues)
        {
            QueryStringBuilder qb = new QueryStringBuilder(UriHelper.GetDisplayUrl(context.HttpContext.Re‌​quest));
            qb.AddRange(keyValues);

            //var action = context.ActionDescriptor.RouteValues["action"];

            tag.InnerHtml.Append(innerHtml ?? string.Empty);
            tag.MergeAttribute("href", $"{qb.QueryString}");
        }
    }
}