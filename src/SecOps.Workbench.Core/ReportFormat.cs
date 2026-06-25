namespace SecOps.Workbench.Core;

public enum ReportFormat
{
    Markdown,
    Json,
    Html
}

public static class ReportFormats
{
    /// <summary>
    /// Parses a user-supplied format name. Returns false for unknown values so callers
    /// can map the failure onto a non-zero exit code.
    /// </summary>
    public static bool TryParse(string? value, out ReportFormat format)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "markdown":
            case "md":
                format = ReportFormat.Markdown;
                return true;
            case "json":
                format = ReportFormat.Json;
                return true;
            case "html":
                format = ReportFormat.Html;
                return true;
            default:
                format = ReportFormat.Markdown;
                return false;
        }
    }
}
