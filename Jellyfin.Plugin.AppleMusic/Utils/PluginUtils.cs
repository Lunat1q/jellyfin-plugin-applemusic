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
    /// Calculate a prefix-weighted similarity score between two strings using
    /// the Ratcliff/Obershelp "gestalt pattern matching" algorithm (same approach
    /// as Python's <c>difflib.SequenceMatcher</c>), blended with a prefix bonus
    /// so that matches at the start of the string score higher.
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

        // Build character-to-positions index for b (reused across recursive calls).
        var b2j = new Dictionary<char, List<int>>();
        for (int j = 0; j < b.Length; j++)
        {
            if (!b2j.TryGetValue(b[j], out var positions))
            {
                positions = new List<int>();
                b2j[b[j]] = positions;
            }

            positions.Add(j);
        }

        var matches = new List<(int PosA, int PosB, int Length)>();
        CollectMatchingBlocks(a, 0, a.Length, b, 0, b.Length, b2j, matches);

        int totalMatched = 0;
        foreach (var match in matches)
        {
            totalMatched += match.Length;
        }

        // Coverage: what fraction of the search term (a) was matched in the candidate (b).
        // This avoids penalising longer candidates that fully contain the search.
        double coverage = (double)totalMatched / a.Length;

        // Prefix bonus: matching characters at the start contribute extra.
        int prefixLen = 0;
        int minLen = Math.Min(a.Length, b.Length);
        while (prefixLen < minLen && a[prefixLen] == b[prefixLen])
        {
            prefixLen++;
        }

        double prefixRatio = (double)prefixLen / a.Length;

        // Blend: 50% coverage, 50% prefix.
        // Prefix-matching characters effectively count twice (once in coverage,
        // once in prefixRatio), giving the start of the string more weight.
        return (0.5 * coverage) + (0.5 * prefixRatio);
    }

    /// <summary>
    /// Find the longest contiguous matching block between <paramref name="a"/>[alo..ahi)
    /// and <paramref name="b"/>[blo..bhi) using an indexed scan (SequenceMatcher approach).
    /// </summary>
    private static (int PosA, int PosB, int Length) FindLongestMatch(
        string a,
        int alo,
        int ahi,
        string b,
        int blo,
        int bhi,
        Dictionary<char, List<int>> b2j)
    {
        int besti = alo, bestj = blo, bestSize = 0;
        var j2Len = new Dictionary<int, int>();

        for (int i = alo; i < ahi; i++)
        {
            var newJ2Len = new Dictionary<int, int>();
            if (b2j.TryGetValue(a[i], out var positions))
            {
                foreach (int j in positions)
                {
                    if (j < blo)
                    {
                        continue;
                    }

                    if (j >= bhi)
                    {
                        break;
                    }

                    int k = (j2Len.TryGetValue(j - 1, out int prev) ? prev : 0) + 1;
                    newJ2Len[j] = k;
                    if (k > bestSize)
                    {
                        besti = i - k + 1;
                        bestj = j - k + 1;
                        bestSize = k;
                    }
                }
            }

            j2Len = newJ2Len;
        }

        return (besti, bestj, bestSize);
    }

    /// <summary>
    /// Recursively collect all non-overlapping matching blocks between two strings
    /// (equivalent to <c>SequenceMatcher.get_matching_blocks()</c>).
    /// </summary>
    private static void CollectMatchingBlocks(
        string a,
        int alo,
        int ahi,
        string b,
        int blo,
        int bhi,
        Dictionary<char, List<int>> b2j,
        List<(int PosA, int PosB, int Length)> result)
    {
        var (posA, posB, length) = FindLongestMatch(a, alo, ahi, b, blo, bhi, b2j);
        if (length == 0)
        {
            return;
        }

        if (alo < posA && blo < posB)
        {
            CollectMatchingBlocks(a, alo, posA, b, blo, posB, b2j, result);
        }

        result.Add((posA, posB, length));

        if (posA + length < ahi && posB + length < bhi)
        {
            CollectMatchingBlocks(a, posA + length, ahi, b, posB + length, bhi, b2j, result);
        }
    }

    private static string NormalizeForComparison(string input)
    {
        return input.Trim().ToLowerInvariant();
    }
}
