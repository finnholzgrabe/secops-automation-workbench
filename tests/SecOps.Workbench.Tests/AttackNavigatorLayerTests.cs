using System.Text.Json;
using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class AttackNavigatorLayerTests
{
    [Fact]
    public void Tally_counts_and_orders_techniques_deterministically()
    {
        var techniques = TechniqueFrequency.Tally(new IReadOnlyList<string>[]
        {
            new[] { "T1078", "T1110" },
            new[] { "T1078", "T1621" },
            new[] { "T1078" }
        });

        Assert.Equal("T1078", techniques[0].TechniqueId);
        Assert.Equal(3, techniques[0].Count);
        Assert.Equal("Valid Accounts", techniques[0].TechniqueName);
        // Remaining ties (count 1) are ordered by technique id.
        Assert.Equal(new[] { "T1078", "T1110", "T1621" }, techniques.Select(t => t.TechniqueId));
    }

    [Fact]
    public void Build_produces_a_valid_navigator_layer()
    {
        var techniques = TechniqueFrequency.Tally(new IReadOnlyList<string>[]
        {
            new[] { "T1078", "T1110" },
            new[] { "T1078" }
        });

        var json = AttackNavigatorLayer.Build("Test layer", "synthetic", techniques);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("Test layer", root.GetProperty("name").GetString());
        Assert.Equal("enterprise-attack", root.GetProperty("domain").GetString());
        Assert.Equal("4.5", root.GetProperty("versions").GetProperty("layer").GetString());

        var entries = root.GetProperty("techniques");
        Assert.Equal(2, entries.GetArrayLength());
        Assert.Equal("T1078", entries[0].GetProperty("techniqueID").GetString());
        Assert.Equal(2, entries[0].GetProperty("score").GetInt32());

        Assert.Equal(2, root.GetProperty("gradient").GetProperty("maxValue").GetInt32());
    }

    [Fact]
    public void Build_handles_empty_techniques()
    {
        var json = AttackNavigatorLayer.Build("Empty", "none", Array.Empty<TechniqueFrequency>());

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(0, root.GetProperty("techniques").GetArrayLength());
        Assert.Equal(1, root.GetProperty("gradient").GetProperty("maxValue").GetInt32());
    }
}
