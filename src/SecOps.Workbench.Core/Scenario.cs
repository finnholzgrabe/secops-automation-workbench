using System.Text.Json;

namespace SecOps.Workbench.Core;

/// <summary>An ordered sequence of synthetic alerts representing an emulated intrusion.</summary>
public sealed record Scenario(
    string Name,
    string Description,
    IReadOnlyList<SecurityAlert> Alerts);

/// <summary>Parses a scenario file: a name/description plus an ordered array of alerts.</summary>
public static class ScenarioParser
{
    public static Scenario Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var name = OptionalString(root, "name", "Unnamed scenario");
        var description = OptionalString(root, "description", string.Empty);

        if (!root.TryGetProperty("alerts", out var alertsElement) || alertsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Scenario is missing required array property 'alerts'.");
        }

        var alerts = new List<SecurityAlert>();
        foreach (var element in alertsElement.EnumerateArray())
        {
            alerts.Add(AlertParser.Parse(element.GetRawText()));
        }

        if (alerts.Count == 0)
        {
            throw new InvalidDataException("Scenario 'alerts' must contain at least one alert.");
        }

        return new Scenario(name, description, alerts);
    }

    private static string OptionalString(JsonElement root, string name, string fallback) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;
}
