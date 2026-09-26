using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LamuFlix.Web.TagHelpers;

public class QueryStringKeyValuePair(string key, string value)
{
    public string Key { get; } = key;
    public string Value { get; } = value;
}

public class QueryStringBuilder
{
    private List<KeyValuePair<string, string>> QueryStringValues { get; set; }

    public QueryString QueryString => new QueryBuilder(QueryStringValues).ToQueryString();

    public void Add(QueryStringKeyValuePair kvPair) //KeyValuePair<string, string> keyValue)
    {
        Add(kvPair.Key, kvPair.Value);
    }

    private void Add(string key, string value)
    {
        var qs = QueryStringValues.SingleOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (null != qs.Value)
            QueryStringValues.RemoveAll(x => string.Equals(x.Key, qs.Key, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Value, qs.Value, StringComparison.Ordinal));

        QueryStringValues.Add(new KeyValuePair<string, string>(key, value));
    }
    public void AddRange(List<QueryStringKeyValuePair> keyvalues)
    {
        foreach (var keyvalue in keyvalues)
            Add(keyvalue.Key, keyvalue.Value);
    }
    public void Remove(string key)
    {
        var qs = QueryStringValues.SingleOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (null != qs.Value)
            QueryStringValues.RemoveAll(x => string.Equals(x.Key, qs.Key, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Value, qs.Value, StringComparison.Ordinal));
    }

    public QueryStringBuilder(string displayUrl)
    {
        var uri = new Uri(displayUrl);

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
        var qb = new QueryStringBuilder(context.HttpContext.Re‌​quest.GetDisplayUrl());
        qb.Add(keyValue);

        //var action = context.ActionDescriptor.RouteValues["action"];

        tag.InnerHtml.Append(innerHtml ?? string.Empty);
        tag.MergeAttribute("href", $"{qb.QueryString}");
    }
}