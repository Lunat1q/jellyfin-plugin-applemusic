using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.AppleMusic.ExternalIds;

/// <summary>
/// Extends <see cref="IExternalId"/> with a URL format string.
/// </summary>
public interface IExternalIdWithUrlFormat : IExternalId
{
    /// <summary>
    /// Gets the URL format string for the external ID.
    /// </summary>
    string UrlFormatString { get; }
}
