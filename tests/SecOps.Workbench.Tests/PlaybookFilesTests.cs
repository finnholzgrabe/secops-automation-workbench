using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class PlaybookFilesTests
{
    private static string PlaybooksDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "playbooks");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository 'playbooks' directory.");
    }

    private static IReadOnlyList<Playbook> LoadFromDisk() =>
        Directory.EnumerateFiles(PlaybooksDirectory(), "*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => PlaybookParser.Parse(File.ReadAllText(path)))
            .ToList();

    [Fact]
    public void Every_on_disk_playbook_is_valid()
    {
        foreach (var file in Directory.EnumerateFiles(PlaybooksDirectory(), "*.json"))
        {
            var playbook = PlaybookParser.Parse(File.ReadAllText(file));
            var errors = PlaybookValidator.Validate(playbook);

            Assert.True(errors.Count == 0, $"{Path.GetFileName(file)} has errors: {string.Join("; ", errors)}");
        }
    }

    [Fact]
    public void On_disk_playbooks_match_the_built_in_catalog()
    {
        var onDisk = LoadFromDisk().OrderBy(p => p.Id, StringComparer.Ordinal).ToList();
        var builtIn = PlaybookCatalog.Default.OrderBy(p => p.Id, StringComparer.Ordinal).ToList();

        Assert.Equal(builtIn.Count, onDisk.Count);

        for (var i = 0; i < builtIn.Count; i++)
        {
            AssertSamePlaybook(builtIn[i], onDisk[i]);
        }
    }

    private static void AssertSamePlaybook(Playbook expected, Playbook actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Category, actual.Category);
        Assert.Equal(expected.Techniques, actual.Techniques);
        Assert.Equal(expected.RecommendedActions, actual.RecommendedActions);
        Assert.Equal(expected.DryRunOnly, actual.DryRunOnly);
    }
}
