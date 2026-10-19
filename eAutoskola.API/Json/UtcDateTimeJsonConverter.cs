using System.Text.Json;
using System.Text.Json.Serialization;

namespace eAutoskola.API.Json;

/// <summary>
/// Every DateTime the API stores or computes is UTC, but EF Core hands values back with an
/// unspecified kind and System.Text.Json would then write them without a zone suffix
/// ("2026-10-23T08:00:00") - which clients read as LOCAL time. This writes them as UTC
/// ("2026-10-23T08:00:00Z") and reads incoming values as UTC.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.SpecifyKind(reader.GetDateTime().ToUniversalTime(), DateTimeKind.Utc);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
