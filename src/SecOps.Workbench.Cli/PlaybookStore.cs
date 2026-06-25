using System.Text.Json;
using SecOps.Workbench.Core;

namespace SecOps.Workbench.Cli;

/// <summary>
/// Loads playbook JSON files from a directory. Keeps all filesystem access in the CLI layer;
/// parsing and validation stay in Core.
/// </summary>
internal static class PlaybookStore
{
    public const string DefaultDirectory = "playbooks";

    public sealed record LoadedPlaybook(string Path, Playbook? Playbook, IReadOnlyList<string> Errors);

    public static IReadOnlyList<LoadedPlaybook> LoadDirectory(string directory)
    {
        var results = new List<LoadedPlaybook>();

        foreach (var file in Directory.EnumerateFiles(directory, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            try
            {
                var playbook = PlaybookParser.Parse(File.ReadAllText(file));
                results.Add(new LoadedPlaybook(file, playbook, PlaybookValidator.Validate(playbook)));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or ArgumentException)
            {
                results.Add(new LoadedPlaybook(file, null, new[] { ex.Message }));
            }
        }

        return results;
    }

    /// <summary>
    /// Returns the valid playbooks for triage, falling back to the built-in catalog when the
    /// directory is missing or contains no usable playbook. This keeps triage working from a
    /// clean checkout while still being driven by the on-disk definitions when present.
    /// </summary>
    public static IReadOnlyList<Playbook> LoadForTriage(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return PlaybookCatalog.Default;
        }

        var valid = LoadDirectory(directory)
            .Where(loaded => loaded.Playbook is not null && loaded.Errors.Count == 0)
            .Select(loaded => loaded.Playbook!)
            .ToList();

        return valid.Count > 0 ? valid : PlaybookCatalog.Default;
    }
}
