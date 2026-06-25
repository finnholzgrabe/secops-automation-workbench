using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SecOps.Workbench.Core;

public sealed record TriageResult(
    SecurityAlert Alert,
    IReadOnlyList<string> TechniqueIds,
    string RecommendedPlaybook,
    IReadOnlyList<string> RecommendedActions,
    string Rationale,
    EnrichmentResult Enrichment)
{
    /// <summary>Renders the report in the requested format.</summary>
    public string Render(ReportFormat format) => format switch
    {
        ReportFormat.Json => ToJson(),
        _ => ToMarkdown()
    };

    /// <summary>
    /// Serializes the result with a stable, hand-ordered top-level field layout so the
    /// JSON contract stays deterministic and diff-friendly across runs.
    /// </summary>
    public string ToJson()
    {
        // The report is written to a file or stdout, never embedded in HTML, so the relaxed
        // encoder is safe here and keeps characters like apostrophes human-readable.
        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, options))
        {
            writer.WriteStartObject();
            writer.WriteString("alertId", Alert.Id);
            writer.WriteString("title", Alert.Title);
            writer.WriteString("source", Alert.Source);
            writer.WriteString("severity", Alert.Severity.ToString());
            writer.WriteString("category", Alert.Category);
            writer.WriteString("principal", Alert.Principal);
            writer.WriteString("asset", Alert.Asset);

            writer.WriteStartArray("observables");
            foreach (var observable in Alert.Observables)
            {
                writer.WriteStringValue(observable);
            }
            writer.WriteEndArray();

            writer.WriteStartArray("techniqueIds");
            foreach (var technique in TechniqueIds)
            {
                writer.WriteStringValue(technique);
            }
            writer.WriteEndArray();

            writer.WriteString("recommendedPlaybook", RecommendedPlaybook);

            writer.WriteStartArray("recommendedActions");
            foreach (var action in RecommendedActions)
            {
                writer.WriteStringValue(action);
            }
            writer.WriteEndArray();

            writer.WriteString("rationale", Rationale);
            writer.WriteBoolean("dryRun", true);

            writer.WriteStartObject("enrichment");
            WriteIdentity(writer, Enrichment.Identity);
            WriteAsset(writer, Enrichment.Asset);
            writer.WriteStartArray("observables");
            foreach (var observable in Enrichment.Observables)
            {
                writer.WriteStartObject();
                writer.WriteString("observable", observable.Observable);
                writer.WriteString("verdict", observable.Verdict);
                writer.WriteString("context", observable.Context);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteIdentity(Utf8JsonWriter writer, IdentityContext identity)
    {
        writer.WriteStartObject("identity");
        writer.WriteString("principal", identity.Principal);
        writer.WriteBoolean("isPrivileged", identity.IsPrivileged);
        writer.WriteString("riskTier", identity.RiskTier);
        writer.WriteStartArray("riskHints");
        foreach (var hint in identity.RiskHints)
        {
            writer.WriteStringValue(hint);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteAsset(Utf8JsonWriter writer, AssetContext asset)
    {
        writer.WriteStartObject("asset");
        writer.WriteString("asset", asset.Asset);
        writer.WriteString("criticality", asset.Criticality);
        writer.WriteStartArray("notes");
        foreach (var note in asset.Notes)
        {
            writer.WriteStringValue(note);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public string ToMarkdown()
    {
        var techniques = TechniqueIds.Count == 0 ? "none" : string.Join(", ", TechniqueIds);
        var actions = string.Join(Environment.NewLine, RecommendedActions.Select(action => $"- {action}"));
        var enrichment = BuildEnrichmentMarkdown();

        return $"""
               # Triage: {Alert.Title}

               - Alert ID: {Alert.Id}
               - Severity: {Alert.Severity}
               - Principal: {Alert.Principal}
               - Asset: {Alert.Asset}
               - Techniques: {techniques}
               - Recommended playbook: {RecommendedPlaybook}

               ## Rationale

               {Rationale}

               ## Enrichment (synthetic)

               {enrichment}

               ## Recommended safe actions

               {actions}
               """;
    }

    private string BuildEnrichmentMarkdown()
    {
        var lines = new List<string>
        {
            $"- Identity: {Enrichment.Identity.Principal} — risk tier {Enrichment.Identity.RiskTier}"
                + (Enrichment.Identity.IsPrivileged ? " (privileged)" : string.Empty),
            $"- Asset: {Enrichment.Asset.Asset} — criticality {Enrichment.Asset.Criticality}",
            "- Observable context:"
        };

        lines.AddRange(Enrichment.Observables.Select(
            observable => $"  - {observable.Observable}: {observable.Verdict} — {observable.Context}"));

        return string.Join(Environment.NewLine, lines);
    }
}
