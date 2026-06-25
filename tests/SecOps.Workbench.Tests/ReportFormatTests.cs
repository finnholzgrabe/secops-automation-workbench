using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class ReportFormatTests
{
    [Theory]
    [InlineData("markdown", ReportFormat.Markdown)]
    [InlineData("md", ReportFormat.Markdown)]
    [InlineData("json", ReportFormat.Json)]
    [InlineData("JSON", ReportFormat.Json)]
    public void Accepts_known_formats(string value, ReportFormat expected)
    {
        Assert.True(ReportFormats.TryParse(value, out var format));
        Assert.Equal(expected, format);
    }

    [Theory]
    [InlineData("yaml")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_unknown_formats(string? value)
    {
        Assert.False(ReportFormats.TryParse(value, out _));
    }
}
