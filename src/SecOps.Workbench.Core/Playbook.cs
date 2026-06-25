using System.Text.Json;
using System.Text.RegularExpressions;

namespace SecOps.Workbench.Core;

/// <summary>
/// A safe, dry-run-only response playbook. Definitions live as JSON files under
/// <c>playbooks/</c> and are mirrored by <see cref="PlaybookCatalog.Default"/>.
/// </summary>
public sealed record Playbook(
    string Id,
    string Title,
    string Description,
    string Category,
    IReadOnlyList<string> Techniques,
    IReadOnlyList<string> RecommendedActions,
    bool DryRunOnly);

/// <summary>
/// Tolerant JSON reader for playbooks. Missing fields are filled with safe defaults so the
/// <see cref="PlaybookValidator"/> can report every problem at once instead of throwing on the
/// first gap. Malformed JSON still surfaces as a <see cref="JsonException"/>.
/// </summary>
public static class PlaybookParser
{
    public static Playbook Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new Playbook(
            OptionalString(root, "id"),
            OptionalString(root, "title"),
            OptionalString(root, "description"),
            OptionalString(root, "category"),
            OptionalStringArray(root, "techniques"),
            OptionalStringArray(root, "recommendedActions"),
            OptionalBool(root, "dryRunOnly"));
    }

    private static string OptionalString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static IReadOnlyList<string> OptionalStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var values = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                values.Add(item.GetString() ?? string.Empty);
            }
        }

        return values;
    }

    private static bool OptionalBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False
        && value.GetBoolean();
}

/// <summary>
/// Validates playbook content against the workbench rules and returns readable error
/// messages. An empty result means the playbook is valid.
/// </summary>
public static partial class PlaybookValidator
{
    public static IReadOnlyList<string> Validate(Playbook playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(playbook.Id))
        {
            errors.Add("id is required.");
        }
        else if (playbook.Id.Any(char.IsWhiteSpace))
        {
            errors.Add($"id '{playbook.Id}' must not contain whitespace.");
        }

        if (string.IsNullOrWhiteSpace(playbook.Title))
        {
            errors.Add("title is required.");
        }

        if (string.IsNullOrWhiteSpace(playbook.Description))
        {
            errors.Add("description is required.");
        }

        if (string.IsNullOrWhiteSpace(playbook.Category))
        {
            errors.Add("category is required.");
        }

        foreach (var technique in playbook.Techniques)
        {
            if (!TechniqueIdPattern().IsMatch(technique))
            {
                errors.Add($"technique '{technique}' is not a valid ATT&CK-style technique id (expected e.g. T1078 or T1078.004).");
            }
        }

        if (playbook.RecommendedActions.Count == 0)
        {
            errors.Add("at least one recommendedAction is required.");
        }
        else if (playbook.RecommendedActions.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("recommendedActions must not contain empty entries.");
        }

        if (!playbook.DryRunOnly)
        {
            errors.Add("dryRunOnly must be true; the workbench only ships safe, dry-run playbooks.");
        }

        return errors;
    }

    [GeneratedRegex(@"^T\d{4}(\.\d{3})?$")]
    private static partial Regex TechniqueIdPattern();
}
