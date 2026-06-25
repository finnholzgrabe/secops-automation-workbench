using System.Net;
using System.Text;

namespace SecOps.Workbench.Core;

/// <summary>
/// Renders a self-contained, dependency-free HTML triage report. All dynamic values are
/// HTML-encoded. The output is a complete static page suitable for publishing (e.g. GitHub Pages).
/// </summary>
public static class HtmlReport
{
    public static string Render(TriageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var alert = result.Alert;
        var builder = new StringBuilder();

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine($"<title>Triage: {Encode(alert.Id)}</title>");
        builder.AppendLine("<style>");
        builder.AppendLine(Css);
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<main>");

        builder.AppendLine("<p class=\"banner\">Synthetic report &middot; all response steps are dry-run recommendations</p>");
        builder.AppendLine($"<h1>{Encode(alert.Title)}</h1>");

        builder.AppendLine("<dl class=\"meta\">");
        AppendField(builder, "Alert ID", alert.Id);
        builder.AppendLine($"<dt>Severity</dt><dd><span class=\"sev sev-{Encode(alert.Severity.ToString().ToLowerInvariant())}\">{Encode(alert.Severity.ToString())}</span></dd>");
        AppendField(builder, "Principal", alert.Principal);
        AppendField(builder, "Asset", alert.Asset);
        AppendField(builder, "Recommended playbook", result.RecommendedPlaybook);
        builder.AppendLine("</dl>");

        builder.Append("<p class=\"chips\">");
        if (result.TechniqueIds.Count == 0)
        {
            builder.Append("<span class=\"chip\">no techniques mapped</span>");
        }
        else
        {
            foreach (var technique in result.TechniqueIds)
            {
                builder.Append($"<span class=\"chip\">{Encode(technique)} &middot; {Encode(AttackTechniqueCatalog.NameFor(technique))}</span>");
            }
        }
        builder.AppendLine("</p>");

        builder.AppendLine("<h2>Rationale</h2>");
        builder.AppendLine($"<p>{Encode(result.Rationale)}</p>");

        builder.AppendLine("<h2>Enrichment (synthetic)</h2>");
        builder.AppendLine("<ul>");
        builder.AppendLine($"<li>Identity: {Encode(result.Enrichment.Identity.Principal)} &mdash; risk tier {Encode(result.Enrichment.Identity.RiskTier)}{(result.Enrichment.Identity.IsPrivileged ? " (privileged)" : string.Empty)}</li>");
        builder.AppendLine($"<li>Asset: {Encode(result.Enrichment.Asset.Asset)} &mdash; criticality {Encode(result.Enrichment.Asset.Criticality)}</li>");
        builder.AppendLine("</ul>");
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>observable</th><th>verdict</th><th>context</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var observable in result.Enrichment.Observables)
        {
            builder.AppendLine($"<tr><td>{Encode(observable.Observable)}</td><td>{Encode(observable.Verdict)}</td><td>{Encode(observable.Context)}</td></tr>");
        }
        builder.AppendLine("</tbody></table>");

        builder.AppendLine("<h2>Recommended safe actions</h2>");
        builder.AppendLine("<ul>");
        foreach (var action in result.RecommendedActions)
        {
            builder.AppendLine($"<li>{Encode(action)}</li>");
        }
        builder.AppendLine("</ul>");

        builder.AppendLine("</main>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString().TrimEnd();
    }

    private static void AppendField(StringBuilder builder, string label, string value)
    {
        builder.AppendLine($"<dt>{Encode(label)}</dt><dd>{Encode(value)}</dd>");
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private const string Css = """
        :root { color-scheme: light dark; }
        body { font-family: system-ui, -apple-system, sans-serif; line-height: 1.6; margin: 0; padding: 2rem; max-width: 820px; }
        main { margin: 0 auto; }
        h1 { font-size: 1.6rem; margin: 0.4rem 0 1rem; }
        h2 { font-size: 1.15rem; margin-top: 1.8rem; border-bottom: 1px solid #8884; padding-bottom: 0.2rem; }
        .banner { background: #f3f3f3; color: #444; border: 1px solid #ddd; border-radius: 6px; padding: 0.5rem 0.75rem; font-size: 0.9rem; }
        dl.meta { display: grid; grid-template-columns: auto 1fr; gap: 0.3rem 1rem; }
        dl.meta dt { font-weight: 600; }
        dl.meta dd { margin: 0; }
        .chips { display: flex; flex-wrap: wrap; gap: 0.4rem; padding: 0; }
        .chip { background: #6b8cff22; border: 1px solid #6b8cff66; border-radius: 999px; padding: 0.15rem 0.7rem; font-size: 0.85rem; }
        .sev { border-radius: 4px; padding: 0.1rem 0.5rem; font-size: 0.85rem; font-weight: 600; }
        .sev-low { background: #d7f5dd; color: #1c5b2b; }
        .sev-medium { background: #fdf0c8; color: #7a5a00; }
        .sev-high { background: #ffe0cc; color: #8a3b00; }
        .sev-critical { background: #ffd6d6; color: #8a0000; }
        table { border-collapse: collapse; width: 100%; font-size: 0.92rem; }
        th, td { border: 1px solid #8884; padding: 0.4rem 0.6rem; text-align: left; vertical-align: top; }
        """;
}
