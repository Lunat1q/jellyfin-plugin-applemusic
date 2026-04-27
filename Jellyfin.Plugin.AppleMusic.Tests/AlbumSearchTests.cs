using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.MetadataSources;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Web;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Jellyfin.Plugin.AppleMusic.Tests;

public class AlbumSearchTests
{
    private const string AlbumId = "1674527610";
    private const string AlbumName = "Atomic Heart, Vol. 1 (Original Game Soundtrack)";

    private readonly IMetadataSource _metadataSource;
    private readonly ITestOutputHelper _output;

    public AlbumSearchTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder => builder.AddXunitOutput(output));
        _metadataSource = new WebMetadataSource(loggerFactory);
    }

    [Fact]
    public async Task SearchByAlbumId_ReturnsCorrectAlbum()
    {
        // Act
        var album = await _metadataSource.GetAlbumAsync(AlbumId, CancellationToken.None);

        // Assert
        Assert.NotNull(album);
        Assert.Equal(AlbumId, album.Id);
        Assert.Equal(AlbumName, album.Name);
        Assert.NotNull(album.ImageUrl);
        Assert.NotEmpty(album.Artists);
        Assert.NotNull(album.ReleaseDate);

        _output.WriteLine($"Album: {album.Name}");
        _output.WriteLine($"ID: {album.Id}");
        _output.WriteLine($"Image: {album.ImageUrl}");
        _output.WriteLine($"Release Date: {album.ReleaseDate}");
        _output.WriteLine($"About: {album.About}");
        foreach (var artist in album.Artists)
        {
            _output.WriteLine($"Artist: {artist.Name} (ID: {artist.Id}, URL: {artist.Url})");
        }
    }

    [Fact]
    public async Task SearchByAlbumName_ReturnsMatchingResults()
    {
        // Act
        var results = await _metadataSource.SearchAsync(AlbumName, ItemType.Album, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);

        _output.WriteLine($"Found {results.Count} results for '{AlbumName}':");
        foreach (var result in results)
        {
            _output.WriteLine($"  - {result.Name} (ID: {result.Id})");
        }

        // Verify the target album appears in the search results
        var match = Assert.Single(results, r => r.Id == AlbumId);
        Assert.Equal(AlbumName, match.Name);
    }
}

internal static class LoggingExtensions
{
    public static ILoggingBuilder AddXunitOutput(this ILoggingBuilder builder, ITestOutputHelper output)
    {
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddProvider(new XunitLoggerProvider(output));
        return builder;
    }
}

internal sealed class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output) => _output = output;

    public ILogger CreateLogger(string categoryName) => new XunitLogger(_output, categoryName);

    public void Dispose() { }
}

internal sealed class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _category;

    public XunitLogger(ITestOutputHelper output, string category)
    {
        _output = output;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            _output.WriteLine($"[{logLevel}] {_category}: {formatter(state, exception)}");
        }
        catch
        {
            // Test output may not be available
        }
    }
}
