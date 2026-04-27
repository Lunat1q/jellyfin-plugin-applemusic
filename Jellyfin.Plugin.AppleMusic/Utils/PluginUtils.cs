using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.AppleMusic.Utils;

/// <summary>
/// Various general plugin utilities.
/// </summary>
public static class PluginUtils
{
    /// <summary>
    /// Gets plugin name.
    /// </summary>
    public static string PluginName => "Apple Music";

    /// <summary>
    /// Gets Apple Music base URL.
    /// </summary>
    public static string AppleMusicBaseUrl => "https://music.apple.com/us";

    /// <summary>
    /// Gets Apple Music API base URL.
    /// </summary>
    public static string AppleMusicApiBaseUrl => "https://api.music.apple.com/v1";

    /// <summary>
    /// Update image resolution (width)x(height)(opts) in image URL.
    /// For example 1440x1440cc is an image with 1440x1440 resolution with ?center crop? from the source image.
    /// </summary>
    /// <param name="url">URL to work with.</param>
    /// <param name="newImageRes">New image resolution.</param>
    /// <returns>Updated URL.</returns>
    public static string UpdateImageSize(string url, string newImageRes)
    {
        var idx = url.LastIndexOf('/');
        if (idx < 0)
        {
            return url;
        }

        return string.Concat(url.AsSpan(0, idx + 1), newImageRes, ".jpg");
    }

    /// <summary>
    /// Get Apple Music ID from Apple Music URL.
    /// The URL format is always "https://music.apple.com/us/[item type]/[ID]".
    /// </summary>
    /// <param name="url">Apple Music URL.</param>
    /// <returns>Item ID.</returns>
    public static string GetIdFromUrl(string url)
    {
        return url.Split('/').LastOrDefault(string.Empty);
    }

    /// <summary>
    /// Select the best matching item from a list by comparing names to a target string.
    /// Uses a prefix-weighted similarity score: matching characters at the start of the
    /// string contribute more than those at the end.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="items">Candidate items.</param>
    /// <param name="target">Target name to match against.</param>
    /// <param name="nameSelector">Function to extract the name from each item.</param>
    /// <returns>The best matching item, or default if the list is empty.</returns>
    public static T? GetBestMatch<T>(IEnumerable<T> items, string target, Func<T, string?> nameSelector)
    {
        var normalizedTarget = NormalizeForComparison(target);
        T? bestItem = default;
        double bestScore = -1;

        foreach (var item in items)
        {
            var name = nameSelector(item);
            if (name is null)
            {
                continue;
            }

            var score = CalculateSimilarity(normalizedTarget, NormalizeForComparison(name));
            if (score > bestScore)
            {
                bestScore = score;
                bestItem = item;
            }
        }

        return bestItem;
    }

    /// <summary>
    /// Calculate a prefix-weighted similarity score between two strings.
    /// Returns a value between 0.0 (no match) and 1.0 (exact match).
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    internal static double CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return 0.0;
        }

        if (a.Equals(b, StringComparison.Ordinal))
        {
            return 1.0;
        }

        int maxLen = Math.Max(a.Length, b.Length);
        int minLen = Math.Min(a.Length, b.Length);

        // Prefix match: how many characters match from the start
        int prefixLen = 0;
        while (prefixLen < minLen && a[prefixLen] == b[prefixLen])
        {
            prefixLen++;
        }

        double prefixScore = (double)prefixLen / maxLen;

        // Length similarity
        double lengthScore = (double)minLen / maxLen;

        // Weighted: prefix contributes 70%, length similarity 30%
        return (0.7 * prefixScore) + (0.3 * lengthScore);
    }

    private static string NormalizeForComparison(string input)
    {
        return input.Trim().ToLowerInvariant();
    }
}
