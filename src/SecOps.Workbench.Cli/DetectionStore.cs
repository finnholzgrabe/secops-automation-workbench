using SecOps.Workbench.Core;

namespace SecOps.Workbench.Cli;

/// <summary>
/// Lints detection files from a directory. Filesystem access (reading rules, checking that the
/// referenced test fixture exists) stays in the CLI; the content rules live in Core.
/// </summary>
internal static class DetectionStore
{
    public const string DefaultDirectory = "detections";

    public sealed record LintedDetection(string Path, IReadOnlyList<string> Issues);

    public static IReadOnlyList<LintedDetection> LintDirectory(string directory)
    {
        var files = Directory.EnumerateFiles(directory, "*.yml")
            .Concat(Directory.EnumerateFiles(directory, "*.yaml"))
            .OrderBy(path => path, StringComparer.Ordinal);

        var results = new List<LintedDetection>();

        foreach (var file in files)
        {
            var issues = new List<string>();

            try
            {
                var result = DetectionLinter.Lint(File.ReadAllText(file));
                issues.AddRange(result.Issues);

                if (!string.IsNullOrWhiteSpace(result.TestFixture))
                {
                    var fixturePath = Path.Combine(Path.GetDirectoryName(file) ?? ".", result.TestFixture);
                    if (!File.Exists(fixturePath))
                    {
                        issues.Add($"test fixture '{result.TestFixture}' was not found.");
                    }
                }
            }
            catch (IOException ex)
            {
                issues.Add(ex.Message);
            }

            results.Add(new LintedDetection(file, issues));
        }

        return results;
    }
}
