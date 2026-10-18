using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Serilog.Events;
using Serilog.Parsing;

namespace LamuFlix.ServiceDefaults;

// Serilog forwards events to MEL providers with a formatter that quotes every string value
// ("Handler \"ImportMovieFolderCommand\" failed"). This re-renders the template from the forwarded
// attributes with strings written as-is, the way MEL's own formatter renders them.
internal sealed class UnquotedLogMessageProcessor : BaseProcessor<LogRecord>
{
    private const string OriginalFormatKey = "{OriginalFormat}";

    private static readonly MessageTemplateParser Parser = new();

    public override void OnEnd(LogRecord data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Attributes is not { } attributes || !TryGetTemplate(attributes, out var template))
        {
            return;
        }

        data.FormattedMessage = Render(template, attributes);
    }

    private static bool TryGetTemplate(IReadOnlyList<KeyValuePair<string, object?>> attributes, out string template)
    {
        foreach (var attribute in attributes)
        {
            if (attribute is { Key: OriginalFormatKey, Value: string format })
            {
                template = format;
                return true;
            }
        }

        template = string.Empty;
        return false;
    }

    private static string Render(string template, IReadOnlyList<KeyValuePair<string, object?>> attributes)
    {
        var values = new Dictionary<string, object?>(attributes.Count, StringComparer.Ordinal);
        foreach (var attribute in attributes)
        {
            values.TryAdd(attribute.Key, attribute.Value);
        }

        using var output = new StringWriter(CultureInfo.InvariantCulture);
        foreach (var token in Parser.Parse(template).Tokens)
        {
            output.Write(RenderToken(token, values));
        }

        return output.ToString();
    }

    private static string RenderToken(MessageTemplateToken token, Dictionary<string, object?> values) =>
        token switch
        {
            TextToken text => text.Text,
            PropertyToken property when values.TryGetValue(property.PropertyName, out var value) =>
                Align(RenderValue(value, property.Format), property.Alignment),
            _ => token.ToString() ?? string.Empty,
        };

    private static string RenderValue(object? value, string? format) =>
        value switch
        {
            null => "null",
            string text => text,
            ScalarValue scalar => RenderValue(scalar.Value, format),
            LogEventPropertyValue property => property.ToString(format, CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    private static string Align(string rendered, Alignment? alignment) =>
        alignment switch
        {
            null => rendered,
            { Direction: AlignmentDirection.Left } left => rendered.PadRight(left.Width),
            { } right => rendered.PadLeft(right.Width),
        };
}
