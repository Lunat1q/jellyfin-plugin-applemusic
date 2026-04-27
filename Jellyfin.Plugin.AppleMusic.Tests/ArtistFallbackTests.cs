using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.MetadataSources;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Web;
using Jellyfin.Plugin.AppleMusic.Utils;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Jellyfin.Plugin.AppleMusic.Tests;

/// <summary>
/// Tests that artist name is picked from track info when album-level artist is missing.
/// Reproduces the scenario where Jellyfin has an album like "Vol. 1" with no album artist,
/// but the tracks have artist "Мёртвые Осы".
/// </summary>
public class ArtistFallbackTests
{
    private const string ArtistName = "Мёртвые Осы";
    private const string AlbumName = "Vol. 1";

    private readonly IMetadataSource _metadataSource;
    private readonly ITestOutputHelper _output;

    public ArtistFallbackTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder => builder.AddXunitOutput(output));
        _metadataSource = new WebMetadataSource(loggerFactory);
    }

    [Fact]
    public async Task SearchWithArtistFromTrack_FindsCorrectAlbum()
    {
        // When album has no artist, the provider falls back to the first track's artist
        var searchTerm = $"{ArtistName} {AlbumName}";
        _output.WriteLine($"Searching with term: '{searchTerm}'");

        var results = await _metadataSource.SearchAsync(searchTerm, ItemType.Album, CancellationToken.None);

        _output.WriteLine($"Found {results.Count} results:");
        foreach (var result in results)
        {
            _output.WriteLine($"  - {result.Name} (ID: {result.Id})");
            if (result is ITunesAlbum album)
            {
                foreach (var artist in album.Artists)
                {
                    _output.WriteLine($"    Artist: {artist.Name}");
                }
            }
        }

        Assert.NotEmpty(results);

        var albums = results.Cast<ITunesAlbum>().ToList();
        var bestMatch = PluginUtils.GetBestMatch(albums, AlbumName, a => a.Name);
        Assert.NotNull(bestMatch);
        _output.WriteLine($"Best match: '{bestMatch.Name}' (ID: {bestMatch.Id})");

        var hasCorrectArtist = bestMatch.Artists.Any(a =>
            a.Name.Contains(ArtistName, StringComparison.OrdinalIgnoreCase));
        Assert.True(hasCorrectArtist, $"Expected artist '{ArtistName}' in best match, got: {string.Join(", ", bestMatch.Artists.Select(a => a.Name))}");
    }

    [Fact]
    public async Task SearchWithAlbumNameOnly_ReturnsIrrelevantResults()
    {
        // Searching for just "Vol. 1" without artist returns wrong results
        _output.WriteLine($"Searching with generic term: '{AlbumName}'");

        var results = await _metadataSource.SearchAsync(AlbumName, ItemType.Album, CancellationToken.None);

        _output.WriteLine($"Found {results.Count} results:");
        foreach (var result in results)
        {
            _output.WriteLine($"  - {result.Name} (ID: {result.Id})");
            if (result is ITunesAlbum album)
            {
                foreach (var artist in album.Artists)
                {
                    _output.WriteLine($"    Artist: {artist.Name}");
                }
            }
        }

        // Results exist but are unlikely to be the correct artist
        Assert.NotEmpty(results);
    }
}
