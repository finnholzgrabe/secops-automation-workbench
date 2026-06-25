using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class DetectionFilesTests
{
    private static string DetectionsDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "detections");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository 'detections' directory.");
    }

    public static IEnumerable<object[]> DetectionFiles() =>
        Directory.EnumerateFiles(DetectionsDirectory(), "*.yml")
            .Select(path => new object[] { path });

    [Fact]
    public void At_least_one_detection_exists()
    {
        Assert.NotEmpty(Directory.EnumerateFiles(DetectionsDirectory(), "*.yml"));
    }

    [Theory]
    [MemberData(nameof(DetectionFiles))]
    public void Each_detection_passes_content_linting(string path)
    {
        var result = DetectionLinter.Lint(File.ReadAllText(path));

        Assert.True(result.IsClean, $"{Path.GetFileName(path)} has issues: {string.Join("; ", result.Issues)}");
    }

    [Theory]
    [MemberData(nameof(DetectionFiles))]
    public void Each_detection_references_an_existing_fixture(string path)
    {
        var result = DetectionLinter.Lint(File.ReadAllText(path));

        Assert.NotNull(result.TestFixture);
        var fixturePath = Path.Combine(Path.GetDirectoryName(path)!, result.TestFixture!);
        Assert.True(File.Exists(fixturePath), $"fixture '{result.TestFixture}' referenced by {Path.GetFileName(path)} is missing.");
    }
}
