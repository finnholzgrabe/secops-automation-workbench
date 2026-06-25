using System.Text.Json;

namespace SecOps.Workbench.Core;

public static class AlertParser
{
    public static SecurityAlert Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new SecurityAlert(
            RequiredString(root, "id"),
            RequiredString(root, "title"),
            RequiredString(root, "source"),
            ParseSeverity(RequiredString(root, "severity")),
            RequiredString(root, "category"),
            DateTimeOffset.Parse(RequiredString(root, "timestampUtc"), null, System.Globalization.DateTimeStyles.AssumeUniversal),
            RequiredString(root, "principal"),
            RequiredString(root, "asset"),
            RequiredStringArray(root, "observables"));
    }

    private static string RequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"Alert is missing required string property '{propertyName}'.");
        }

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Alert property '{propertyName}' must not be empty.");
        }

        return value;
    }

    private static IReadOnlyList<string> RequiredStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Alert is missing required array property '{propertyName}'.");
        }

        var values = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                throw new InvalidDataException($"Alert property '{propertyName}' must contain strings only.");
            }

            values.Add(item.GetString()!);
        }

        return values;
    }

    private static AlertSeverity ParseSeverity(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "low" => AlertSeverity.Low,
            "medium" => AlertSeverity.Medium,
            "high" => AlertSeverity.High,
            "critical" => AlertSeverity.Critical,
            _ => throw new InvalidDataException($"Unknown alert severity '{value}'.")
        };
    }
}
