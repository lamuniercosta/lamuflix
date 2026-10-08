using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using LamuFlix.Core.Domain;

namespace LamuFlix.Api;

public static class HttpJsonConfiguration
{
    public static void Apply(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new EnrichmentStatusJsonConverter());
    }

    private sealed class EnrichmentStatusJsonConverter : JsonConverter<EnrichmentStatus>
    {
        public override EnrichmentStatus Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            var name = reader.GetString();
            if (name is null || !EnrichmentStatus.TryFromName(name, false, out var status))
            {
                throw new JsonException();
            }

            return status;
        }

        public override void Write(Utf8JsonWriter writer, EnrichmentStatus value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);
            writer.WriteStringValue(value.Name);
        }
    }
}
