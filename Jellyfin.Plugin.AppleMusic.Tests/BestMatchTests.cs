using Jellyfin.Plugin.AppleMusic.Utils;
using Xunit;

namespace Jellyfin.Plugin.AppleMusic.Tests;

public class BestMatchTests
{
    [Theory]
    [InlineData("Atomic Heart, Vol.3 (Original Game Soundtrack)", "Atomic Heart, Vol. 3 (From \"Atomic Heart\")", "Descendants 3 (Original TV Movie Soundtrack)")]
    [InlineData("Atomic Heart, Vol. 1 (Original Game Soundtrack)", "Atomic Heart, Vol. 1 (Original Game Soundtrack)", "Covers, Vol. 1")]
    [InlineData("Dark Side of the Moon", "Dark Side of the Moon (Remastered)", "The Moon Is a Harsh Mistress")]
    public void GetBestMatch_PrefersCloserName(string target, string expectedBest, string expectedWorse)
    {
        var items = new[] { expectedWorse, expectedBest };

        var best = PluginUtils.GetBestMatch(items, target, name => name);

        Assert.Equal(expectedBest, best);
    }

    [Theory]
    [InlineData("Atomic Heart, Vol.3 (Original Game Soundtrack)", "Descendants 3 (Original TV Movie Soundtrack)")]
    [InlineData("Atomic Heart, Vol. 1 (Original Game Soundtrack)", "Covers, Vol. 1")]
    public void CalculateSimilarity_TargetScoresHigherThanUnrelated(string target, string unrelated)
    {
        var targetNorm = target.ToLowerInvariant();
        var unrelatedNorm = unrelated.ToLowerInvariant();

        var selfScore = PluginUtils.CalculateSimilarity(targetNorm, targetNorm);
        var unrelatedScore = PluginUtils.CalculateSimilarity(targetNorm, unrelatedNorm);

        Assert.True(selfScore > unrelatedScore, $"Self score {selfScore} should be > unrelated score {unrelatedScore}");
    }

    [Fact]
    public void CalculateSimilarity_ExactMatch_ReturnsOne()
    {
        Assert.Equal(1.0, PluginUtils.CalculateSimilarity("test", "test"));
    }

    [Fact]
    public void CalculateSimilarity_EmptyString_ReturnsZero()
    {
        Assert.Equal(0.0, PluginUtils.CalculateSimilarity("", "test"));
        Assert.Equal(0.0, PluginUtils.CalculateSimilarity("test", ""));
    }

    [Fact]
    public void GetBestMatch_EmptyList_ReturnsNull()
    {
        var result = PluginUtils.GetBestMatch(System.Array.Empty<string>(), "test", s => s);
        Assert.Null(result);
    }

    [Fact]
    public void GetBestMatch_SingleItem_ReturnsThatItem()
    {
        var result = PluginUtils.GetBestMatch(new[] { "only one" }, "test", s => s);
        Assert.Equal("only one", result);
    }

    [Fact]
    public void GetBestMatch_PicksCorrectVolume_FromMultipleVolumes()
    {
        var items = new[]
        {
            "Atomic Heart, Vol. 3 (From \"Atomic Heart\")",
            "Atomic Heart, Vol. 4 (From \"Atomic Heart\")",
            "Atomic Heart, Vol. 5 (From \"Atomic Heart\")",
            "Atomic Heart, Vol. 1 (From \"Atomic Heart\")",
            "Atomic Heart, Vol. 2 (From \"Atomic Heart\")",
        };

        var best = PluginUtils.GetBestMatch(items, "Atomic Heart (Original Game Soundtrack) Vol.1", name => name);

        Assert.Equal("Atomic Heart, Vol. 1 (From \"Atomic Heart\")", best);
    }

    [Fact]
    public void GetBestMatch_PicksCorrectVolume_WithMixedSubtitles()
    {
        var items = new[]
        {
            "Atomic Heart, Vol. 3 (From \"Atomic Heart\")",
            "Atomic Heart, Vol. 4 (From \"Atomic Heart\")",
            "Atomic Heart, Vol. 5 (From \"Atomic Heart\")",
            "Atomic Heart, Vol. 1 (Original Game Soundtrack)",
            "Atomic Heart, Vol. 2 (Original Game Soundtrack from \"Atomic Heart\")",
        };

        var best = PluginUtils.GetBestMatch(items, "Atomic Heart, Vol.2 (Original Game Soundtrack)", name => name);

        Assert.Equal("Atomic Heart, Vol. 2 (Original Game Soundtrack from \"Atomic Heart\")", best);
    }
}
