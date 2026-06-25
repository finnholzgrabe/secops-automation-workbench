using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class AlertParserTests
{
    [Fact]
    public void Parses_required_identity_alert_fields()
    {
        var alert = AlertParser.Parse(SampleAlerts.IdentityMfaFatigue);

        Assert.Equal("sample-001", alert.Id);
        Assert.Equal(AlertSeverity.High, alert.Severity);
        Assert.Equal("identity", alert.Category);
        Assert.Equal("alice@example.invalid", alert.Principal);
        Assert.Equal(4, alert.Observables.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{}")]
    [InlineData("{ \"id\": \"x\" }")]
    public void Rejects_missing_or_empty_input(string json)
    {
        Assert.ThrowsAny<Exception>(() => AlertParser.Parse(json));
    }

    [Fact]
    public void Rejects_unknown_severity()
    {
        const string json = """
        {
          "id": "x", "title": "t", "source": "s", "severity": "spicy",
          "category": "identity", "timestampUtc": "2026-06-25T10:15:00Z",
          "principal": "p", "asset": "a", "observables": ["x"]
        }
        """;

        Assert.Throws<InvalidDataException>(() => AlertParser.Parse(json));
    }
}
